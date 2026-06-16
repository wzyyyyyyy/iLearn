using iLearn.Updates;
using Xunit;

namespace iLearn.Tests.Updates;

public sealed class UpdateServiceTests
{
    [Fact]
    public async Task CheckAsync_ReturnsAvailable_WhenRemoteVersionIsNewer()
    {
        var json = """
        {
          "version": "9.0.0",
          "notes": "New release",
          "downloads": {
            "win-x64": "https://example.test/iLearn-win-x64.zip",
            "osx-arm64": "https://example.test/iLearn-osx-arm64.zip",
            "linux-x64": "https://example.test/iLearn-linux-x64.tar.gz"
          }
        }
        """;
        var service = new UpdateService(new StaticJsonClient(json), new Version(1, 3, 0));

        var result = await service.CheckAsync(TestContext.Current.CancellationToken);

        Assert.True(result.IsUpdateAvailable);
        Assert.Equal("9.0.0", result.LatestVersion.ToString());
    }

    private sealed class StaticJsonClient : IUpdateManifestClient
    {
        private readonly string _json;

        public StaticJsonClient(string json)
        {
            _json = json;
        }

        public Task<string> GetManifestJsonAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_json);
        }
    }
}
