namespace iLearn.Updates;

public interface IUpdateService
{
    Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default);
}

public interface IUpdateManifestClient
{
    Task<string> GetManifestJsonAsync(CancellationToken cancellationToken = default);
}
