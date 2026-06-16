using System.Text;
using System.Text.Json;

namespace iLearn.Security;

public sealed class FileSecretStore : ISecretStore
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public FileSecretStore(string filePath)
    {
        _filePath = filePath;
    }

    public async Task<string?> ReadSecretAsync(string key, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var data = await ReadAllAsync(cancellationToken);
            return data.TryGetValue(key, out var encoded)
                ? Encoding.UTF8.GetString(Convert.FromBase64String(encoded))
                : null;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveSecretAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var data = await ReadAllAsync(cancellationToken);
            data[key] = Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
            await WriteAllAsync(data, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task DeleteSecretAsync(string key, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var data = await ReadAllAsync(cancellationToken);
            data.Remove(key);
            await WriteAllAsync(data, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<Dictionary<string, string>> ReadAllAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
            return new Dictionary<string, string>();

        await using var stream = File.OpenRead(_filePath);
        return await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream, cancellationToken: cancellationToken)
            ?? new Dictionary<string, string>();
    }

    private async Task WriteAllAsync(Dictionary<string, string> data, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var tempPath = _filePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var stream = File.Create(tempPath))
            {
                await JsonSerializer.SerializeAsync(stream, data, new JsonSerializerOptions { WriteIndented = true }, cancellationToken);
            }

            File.Move(tempPath, _filePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }
}
