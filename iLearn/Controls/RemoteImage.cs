using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using System.Collections.Concurrent;

namespace iLearn.Controls;

public sealed class RemoteImage : Image
{
    public static readonly StyledProperty<string?> SourceUrlProperty =
        AvaloniaProperty.Register<RemoteImage, string?>(nameof(SourceUrl));

    public static readonly StyledProperty<string?> FallbackSourceUrlProperty =
        AvaloniaProperty.Register<RemoteImage, string?>(
            nameof(FallbackSourceUrl),
            "avares://iLearn/Assets/iLearn.png");

    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(10) };
    private static readonly ConcurrentDictionary<string, Task<Bitmap?>> Cache = new(StringComparer.Ordinal);
    private int _loadVersion;

    public string? SourceUrl
    {
        get => GetValue(SourceUrlProperty);
        set => SetValue(SourceUrlProperty, value);
    }

    public string? FallbackSourceUrl
    {
        get => GetValue(FallbackSourceUrlProperty);
        set => SetValue(FallbackSourceUrlProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SourceUrlProperty || change.Property == FallbackSourceUrlProperty)
            _ = LoadImageAsync();
    }

    private async Task LoadImageAsync()
    {
        var version = Interlocked.Increment(ref _loadVersion);
        var bitmap = await LoadBitmapAsync(SourceUrl).ConfigureAwait(false)
            ?? await LoadBitmapAsync(FallbackSourceUrl).ConfigureAwait(false);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (version == _loadVersion)
                Source = bitmap;
        });
    }

    private static Task<Bitmap?> LoadBitmapAsync(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return Task.FromResult<Bitmap?>(null);

        return Cache.GetOrAdd(imageUrl.Trim(), LoadBitmapCoreAsync);
    }

    private static async Task<Bitmap?> LoadBitmapCoreAsync(string imageUrl)
    {
        try
        {
            var uri = new Uri(imageUrl, UriKind.Absolute);
            if (uri.Scheme.Equals("avares", StringComparison.OrdinalIgnoreCase))
            {
                await using var stream = AssetLoader.Open(uri);
                return new Bitmap(stream);
            }

            if (uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase)
                || uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = await HttpClient.GetByteArrayAsync(uri).ConfigureAwait(false);
                await using var stream = new MemoryStream(bytes);
                return new Bitmap(stream);
            }
        }
        catch
        {
            Cache.TryRemove(imageUrl, out _);
        }

        return null;
    }
}
