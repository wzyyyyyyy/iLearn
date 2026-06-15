using System.IO;

namespace iLearn.Services;

public static class FileNameService
{
    private static readonly HashSet<char> InvalidFileNameCharacters = new(
        Path.GetInvalidFileNameChars().Concat(new[] { '<', '>', ':', '"', '/', '\\', '|', '?', '*' }));

    private static readonly HashSet<string> ReservedDeviceNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON",
        "PRN",
        "AUX",
        "NUL",
        "COM1",
        "COM2",
        "COM3",
        "COM4",
        "COM5",
        "COM6",
        "COM7",
        "COM8",
        "COM9",
        "LPT1",
        "LPT2",
        "LPT3",
        "LPT4",
        "LPT5",
        "LPT6",
        "LPT7",
        "LPT8",
        "LPT9"
    };

    public static string BuildVideoFileName(string sourceName, string perspective)
    {
        return $"{SanitizeFileName(sourceName)}_{SanitizeFileName(perspective)}.mp4";
    }

    public static string BuildSubtitleFileName(string sourceName)
    {
        return $"{SanitizeFileName(sourceName)}.vtt";
    }

    public static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "未命名";
        }

        var sanitized = new string(name.Trim().Select(character =>
            InvalidFileNameCharacters.Contains(character) ? '_' : character).ToArray());
        sanitized = sanitized.TrimEnd('.', ' ');

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            return "未命名";
        }

        var deviceName = sanitized.Split('.')[0];
        return ReservedDeviceNames.Contains(deviceName) ? "_" + sanitized : sanitized;
    }
}
