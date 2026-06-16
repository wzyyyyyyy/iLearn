using System.Runtime.InteropServices;
using System.Text.Json;

namespace iLearn.Updates;

public sealed class UpdateService : IUpdateService
{
    private readonly IUpdateManifestClient _client;
    private readonly Version _currentVersion;

    public UpdateService(IUpdateManifestClient client, Version currentVersion)
    {
        _client = client;
        _currentVersion = currentVersion;
    }

    public async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var json = await _client.GetManifestJsonAsync(cancellationToken);
        var manifest = JsonSerializer.Deserialize<UpdateManifest>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new UpdateManifest();

        var latest = Version.Parse(manifest.Version);
        manifest.Downloads.TryGetValue(GetRuntimeId(), out var downloadUrl);

        return new UpdateCheckResult(latest > _currentVersion, latest, manifest.Notes, downloadUrl);
    }

    private static string GetRuntimeId()
    {
        var arch = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "arm64" : "x64";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return $"win-{arch}";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return $"osx-{arch}";
        return $"linux-{arch}";
    }
}

public sealed class HttpUpdateManifestClient : IUpdateManifestClient
{
    private readonly HttpClient _httpClient;
    private readonly string _manifestUrl;

    public HttpUpdateManifestClient(HttpClient httpClient, string manifestUrl)
    {
        _httpClient = httpClient;
        _manifestUrl = manifestUrl;
    }

    public async Task<string> GetManifestJsonAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetStringAsync(_manifestUrl, cancellationToken);
    }
}
