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
        DownloadRequest? request = null;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!_activeRuns.ContainsKey(id))
                return;

            _cancellations.TryGetValue(id, out cancellation);
            _requests.TryGetValue(id, out request);
            if (request is not null)
                Upsert(DownloadTaskSnapshot.FromRequest(request, DownloadTaskStatus.Cancelling));
            cancellation?.Cancel();
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task PauseAsync(string id, CancellationToken cancellationToken = default)
    {
        return CancelAsync(id, cancellationToken);
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
            finalSnapshot = DownloadTaskSnapshot.FromRequest(request, DownloadTaskStatus.Failed, ex.Message);
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
