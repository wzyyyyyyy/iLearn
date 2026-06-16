using iLearn.Security;
using Xunit;

namespace iLearn.Tests.Security;

public sealed class FileSecretStoreTests
{
    [Fact]
    public async Task SaveAndReadSecret_RoundTripsValue()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ilearn-secret-tests", Guid.NewGuid().ToString("N"));
        var store = new FileSecretStore(Path.Combine(directory, "secrets.json"));

        await store.SaveSecretAsync("login-password", "secret-value", TestContext.Current.CancellationToken);
        var value = await store.ReadSecretAsync("login-password", TestContext.Current.CancellationToken);

        Assert.Equal("secret-value", value);
    }

    [Fact]
    public async Task DeleteSecret_RemovesValue()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ilearn-secret-tests", Guid.NewGuid().ToString("N"));
        var store = new FileSecretStore(Path.Combine(directory, "secrets.json"));

        await store.SaveSecretAsync("login-password", "secret-value", TestContext.Current.CancellationToken);
        await store.DeleteSecretAsync("login-password", TestContext.Current.CancellationToken);

        Assert.Null(await store.ReadSecretAsync("login-password", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CancelledSave_DoesNotDestroyExistingSecret()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ilearn-secret-tests", Guid.NewGuid().ToString("N"));
        var store = new FileSecretStore(Path.Combine(directory, "secrets.json"));
        await store.SaveSecretAsync("login-password", "old-value", TestContext.Current.CancellationToken);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            store.SaveSecretAsync("login-password", "new-value", cts.Token));

        Assert.Equal("old-value", await store.ReadSecretAsync("login-password", TestContext.Current.CancellationToken));
    }
}
