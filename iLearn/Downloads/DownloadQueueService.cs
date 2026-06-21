using System.Collections.ObjectModel;
using System.Threading.Channels;

namespace iLearn.Downloads;

public sealed class DownloadQueueService
{
    private readonly IDownloadEngine _engine;
    private readonly Channel<QueuedDownload> _channel = Channel.CreateUnbounded<QueuedDownload>();
    private readonly SemaphoreSlim _concurrency = new(3, 3);
    private readonly ObservableCollection<DownloadTaskSnapshot> _tasks = new();
    private readonly Dictionary<string, DownloadRequest> _requests = new();
    private readonly Dictionary<string, CancellationTokenSource> _cancellations = new();
    private readonly Dictionary<string, Guid> _activeRuns = new();
    private readonly HashSet<string> _removedActiveRuns = [];
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly object _tasksGate = new();
    private readonly SynchronizationContext? _collectionContext;
    private int _processorStarted;

    public DownloadQueueService(IDownloadEngine engine)
    {
        _engine = engine;
        _collectionContext = SynchronizationContext.Current;
        Tasks = new ReadOnlyObservableCollection<DownloadTaskSnapshot>(_tasks);
    }

    public ReadOnlyObservableCollection<DownloadTaskSnapshot> Tasks { get; }

    public int ActiveCount => CountByStatus(DownloadTaskStatus.Downloading);

    public int FailedCount => CountByStatus(DownloadTaskStatus.Failed);

    public int CompletedCount => CountByStatus(DownloadTaskStatus.Completed);

    public double TotalBytesPerSecond
    {
        get
        {
            lock (_tasksGate)
            {
                return _tasks
                    .Where(item => item.Status == DownloadTaskStatus.Downloading)
                    .Sum(item => item.BytesPerSecond);
            }
        }
    }

    public async Task EnqueueAsync(DownloadRequest request, CancellationToken cancellationToken = default)
    {
        CancellationTokenSource downloadCancellation;
        Guid runId;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_activeRuns.ContainsKey(request.Id))
            {
                throw new InvalidOperationException($"Download task '{request.Id}' already exists.");
            }

            Directory.CreateDirectory(request.OutputDirectory);
            _requests[request.Id] = request;
            downloadCancellation = new CancellationTokenSource();
            _cancellations[request.Id] = downloadCancellation;
            runId = Guid.NewGuid();
            _activeRuns[request.Id] = runId;
            Upsert(DownloadTaskSnapshot.FromRequest(request, DownloadTaskStatus.Queued));
        }
        finally
        {
            _gate.Release();
        }

        await _channel.Writer.WriteAsync(new QueuedDownload(request, downloadCancellation, runId), cancellationToken);
        EnsureProcessorStarted();
    }

    public async Task CancelAsync(string id, CancellationToken cancellationToken = default)
    {
        CancellationTokenSource? cancellation = null;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!_activeRuns.ContainsKey(id))
                return;

            _cancellations.TryGetValue(id, out cancellation);
            _requests.Remove(id);
            _removedActiveRuns.Add(id);
            Remove(id);
            cancellation?.Cancel();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task CancelAllAsync(CancellationToken cancellationToken = default)
    {
        foreach (var id in _activeRuns.Keys)
            await CancelAsync(id, cancellationToken);
    }

    public Task PauseAsync(string id, CancellationToken cancellationToken = default)
    {
        return CancelAsync(id, cancellationToken);
    }

    public void ClearCompleted()
    {
        if (_collectionContext is not null && SynchronizationContext.Current != _collectionContext)
        {
            _collectionContext.Post(_ => ClearCompletedCore(), null);
            return;
        }

        ClearCompletedCore();
    }

    public async Task RetryFailedAsync(CancellationToken cancellationToken = default)
    {
        List<string> failedIds;
        lock (_tasksGate)
        {
            failedIds = _tasks
                .Where(item => item.Status == DownloadTaskStatus.Failed)
                .Select(item => item.Id)
                .ToList();
        }

        foreach (var id in failedIds)
            await RetryAsync(id, cancellationToken);
    }

    public async Task RetryAsync(string id, CancellationToken cancellationToken = default)
    {
        DownloadRequest request;
        CancellationTokenSource downloadCancellation;
        Guid runId;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!_requests.TryGetValue(id, out var storedRequest))
                throw new InvalidOperationException($"Download task '{id}' does not exist.");

            request = storedRequest;

            if (_activeRuns.ContainsKey(id))
                throw new InvalidOperationException($"Download task '{id}' is still active.");

            if (GetStatus(id) is not DownloadTaskStatus.Failed and not DownloadTaskStatus.Cancelled)
                throw new InvalidOperationException($"Download task '{id}' cannot be retried.");

            downloadCancellation = new CancellationTokenSource();
            _cancellations[id] = downloadCancellation;
            runId = Guid.NewGuid();
            _activeRuns[id] = runId;
            Upsert(DownloadTaskSnapshot.FromRequest(request, DownloadTaskStatus.Queued));
        }
        finally
        {
            _gate.Release();
        }

        await _channel.Writer.WriteAsync(new QueuedDownload(request, downloadCancellation, runId), cancellationToken);
        EnsureProcessorStarted();
    }

    private void EnsureProcessorStarted()
    {
        if (Interlocked.Exchange(ref _processorStarted, 1) == 0)
            _ = Task.Run(ProcessQueueAsync);
    }

    private async Task ProcessQueueAsync()
    {
        await foreach (var queuedDownload in _channel.Reader.ReadAllAsync())
        {
            await _concurrency.WaitAsync();
            _ = Task.Run(() => ProcessRequestAsync(queuedDownload));
        }
    }

    private async Task ProcessRequestAsync(QueuedDownload queuedDownload)
    {
        var request = queuedDownload.Request;
        var cancellation = queuedDownload.Cancellation;
        var runId = queuedDownload.RunId;
        var finalSnapshot = DownloadTaskSnapshot.FromRequest(request, DownloadTaskStatus.Cancelled);
        try
        {
            if (cancellation.IsCancellationRequested)
                throw new OperationCanceledException(cancellation.Token);

            TryUpsertForRun(request.Id, runId, DownloadTaskSnapshot.FromRequest(request, DownloadTaskStatus.Downloading));
            await _engine.DownloadAsync(
                request,
                Path.Combine(request.OutputDirectory, request.FileName),
                new InlineProgress(snapshot => TryUpsertForRun(request.Id, runId, snapshot)),
                cancellation.Token);
            finalSnapshot = DownloadTaskSnapshot.FromRequest(request, DownloadTaskStatus.Completed);
        }
        catch (OperationCanceledException)
        {
            finalSnapshot = DownloadTaskSnapshot.FromRequest(request, DownloadTaskStatus.Cancelled);
        }
        catch (Exception ex)
        {
            finalSnapshot = DownloadTaskSnapshot.FromRequest(
                request,
                DownloadTaskStatus.Failed,
                $"{request.FileName}: {ex.Message}");
        }
        finally
        {
            await CompleteActiveRunAsync(request.Id, runId, cancellation, finalSnapshot);
            _concurrency.Release();
        }
    }

    private async Task CompleteActiveRunAsync(
        string id,
        Guid runId,
        CancellationTokenSource cancellation,
        DownloadTaskSnapshot finalSnapshot)
    {
        await _gate.WaitAsync();
        try
        {
            if (_activeRuns.TryGetValue(id, out var currentRun)
                && currentRun == runId)
            {
                if (!_removedActiveRuns.Remove(id))
                    Upsert(finalSnapshot);
                _activeRuns.Remove(id);
            }

            if (_cancellations.TryGetValue(id, out var current)
                && ReferenceEquals(current, cancellation))
            {
                _cancellations.Remove(id);
                current.Dispose();
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private void TryUpsertForRun(string id, Guid runId, DownloadTaskSnapshot snapshot)
    {
        _gate.Wait();
        try
        {
            if (!_activeRuns.TryGetValue(id, out var currentRun) || currentRun != runId)
                return;

            Upsert(snapshot);
        }
        finally
        {
            _gate.Release();
        }
    }

    private void Upsert(DownloadTaskSnapshot snapshot)
    {
        if (_collectionContext is not null && SynchronizationContext.Current != _collectionContext)
        {
            _collectionContext.Post(_ => UpsertCore(snapshot), null);
            return;
        }

        UpsertCore(snapshot);
    }

    private void Remove(string id)
    {
        if (_collectionContext is not null && SynchronizationContext.Current != _collectionContext)
        {
            _collectionContext.Post(_ => RemoveCore(id), null);
            return;
        }

        RemoveCore(id);
    }

    private void UpsertCore(DownloadTaskSnapshot snapshot)
    {
        lock (_tasksGate)
        {
            for (var index = 0; index < _tasks.Count; index++)
            {
                if (_tasks[index].Id == snapshot.Id)
                {
                    _tasks[index] = snapshot;
                    return;
                }
            }

            _tasks.Add(snapshot);
        }
    }

    private DownloadTaskStatus? GetStatus(string id)
    {
        lock (_tasksGate)
        {
            return _tasks.FirstOrDefault(task => task.Id == id)?.Status;
        }
    }

    private void RemoveCore(string id)
    {
        lock (_tasksGate)
        {
            var item = _tasks.FirstOrDefault(task => task.Id == id);
            if (item is not null)
                _tasks.Remove(item);
        }
    }

    private int CountByStatus(DownloadTaskStatus status)
    {
        lock (_tasksGate)
        {
            return _tasks.Count(item => item.Status == status);
        }
    }

    private void ClearCompletedCore()
    {
        lock (_tasksGate)
        {
            foreach (var item in _tasks.Where(item => item.Status == DownloadTaskStatus.Completed).ToList())
                _tasks.Remove(item);
        }
    }

    private sealed class InlineProgress : IProgress<DownloadTaskSnapshot>
    {
        private readonly Action<DownloadTaskSnapshot> _report;

        public InlineProgress(Action<DownloadTaskSnapshot> report)
        {
            _report = report;
        }

        public void Report(DownloadTaskSnapshot value)
        {
            _report(value);
        }
    }

    private sealed record QueuedDownload(DownloadRequest Request, CancellationTokenSource Cancellation, Guid RunId);
}
