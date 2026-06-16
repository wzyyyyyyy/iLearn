using System.Collections.ObjectModel;

namespace iLearn.Downloads;

public sealed class DownloadQueueService
{
    private readonly IDownloadEngine _engine;
    private readonly ObservableCollection<DownloadTaskSnapshot> _tasks = new();
    private readonly Dictionary<string, DownloadRequest> _requests = new();
    private readonly Dictionary<string, CancellationTokenSource> _cancellations = new();
    private readonly SemaphoreSlim _gate = new(1, 1);

    public DownloadQueueService(IDownloadEngine engine)
    {
        _engine = engine;
        Tasks = new ReadOnlyObservableCollection<DownloadTaskSnapshot>(_tasks);
    }

    public ReadOnlyObservableCollection<DownloadTaskSnapshot> Tasks { get; }

    public async Task EnqueueAsync(DownloadRequest request, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_requests.ContainsKey(request.Id))
            {
                throw new InvalidOperationException($"Download task '{request.Id}' already exists.");
            }

            Directory.CreateDirectory(request.OutputDirectory);
            _requests[request.Id] = request;
            Upsert(DownloadTaskSnapshot.FromRequest(request, DownloadTaskStatus.Queued));
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task CancelAsync(string id, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_cancellations.Remove(id, out var cancellation))
                await cancellation.CancelAsync();

            if (_requests.TryGetValue(id, out var request))
                Upsert(DownloadTaskSnapshot.FromRequest(request, DownloadTaskStatus.Cancelled));
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
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!_requests.TryGetValue(id, out var request))
                throw new InvalidOperationException($"Download task '{id}' does not exist.");

            Upsert(DownloadTaskSnapshot.FromRequest(request, DownloadTaskStatus.Queued));
        }
        finally
        {
            _gate.Release();
        }
    }

    private void Upsert(DownloadTaskSnapshot snapshot)
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
