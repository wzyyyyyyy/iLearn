using System.IO;

namespace iLearn.Services;

public static class FileNameService
{
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

        var invalidCharacters = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Trim().Select(character =>
            invalidCharacters.Contains(character) ? '_' : character).ToArray());

        return string.IsNullOrWhiteSpace(sanitized) ? "未命名" : sanitized;
    }
}
