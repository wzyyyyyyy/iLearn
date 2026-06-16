namespace iLearn.Security;

public interface ISecretStore
{
    Task<string?> ReadSecretAsync(string key, CancellationToken cancellationToken = default);
    Task SaveSecretAsync(string key, string value, CancellationToken cancellationToken = default);
    Task DeleteSecretAsync(string key, CancellationToken cancellationToken = default);
}
