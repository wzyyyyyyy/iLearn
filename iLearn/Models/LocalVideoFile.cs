using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using iLearn.Services;

namespace iLearn.Models
{
    public partial class LocalVideoFile : ObservableObject
    {
        [ObservableProperty]
        private string _fileName = string.Empty;

        [ObservableProperty]
        private string _fullPath = string.Empty;

        [ObservableProperty]
        private string _courseName = string.Empty;

        [ObservableProperty]
        private string _type = string.Empty;

        [ObservableProperty]
        private DateTime _recordDate;

        [ObservableProperty]
        private string _timeRange = string.Empty;

        [ObservableProperty]
        private string _perspective = string.Empty;

        [ObservableProperty]
        private long _fileSize;

        [ObservableProperty]
        private string _thumbnailPath = string.Empty;

        public string FileSizeFormatted => FormatFileSize(FileSize);

        public string DateFormatted => RecordDate.ToString("yyyy年MM月dd日");

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        public LocalVideoFile? GetPartnerVideo()
        {
            var directory = Path.GetDirectoryName(FullPath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                return null;
            }

            var baseName = Path.GetFileNameWithoutExtension(FullPath);
            foreach (var partnerBaseName in GetPartnerBaseNames(baseName))
            {
                var partnerPath = Path.Combine(directory, partnerBaseName + ".mp4");
                if (File.Exists(partnerPath))
                {
                    return FromFileName(partnerPath);
                }
            }

            return null;
        }

        public string? FindSubtitlePath(string downloadRoot)
        {
            var videoDirectory = Path.GetDirectoryName(FullPath);
            var subtitleBaseName = StripPerspectiveSuffix(Path.GetFileNameWithoutExtension(FullPath));
            var extensions = new[] { ".vtt", ".srt" };
            var directories = new[]
            {
                videoDirectory,
                string.IsNullOrWhiteSpace(downloadRoot) ? null : Path.Combine(downloadRoot, "Subtitles")
            };

            var exactMatches = directories
                .Where(directory => !string.IsNullOrWhiteSpace(directory))
                .SelectMany(directory => extensions.Select(extension => Path.Combine(directory!, subtitleBaseName + extension)))
                .Where(File.Exists)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (exactMatches.Count > 0)
            {
                return exactMatches.Count == 1 ? exactMatches[0] : null;
            }

            var fuzzyMatches = GetSubtitleCandidates(directories)
                .Where(path => IsSubtitleMatch(subtitleBaseName, Path.GetFileNameWithoutExtension(path)))
                .ToList();

            return fuzzyMatches.Count == 1 ? fuzzyMatches[0] : null;
        }

        public static LocalVideoFile FromFileName(string filePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var fileInfo = new FileInfo(filePath);

            var video = new LocalVideoFile
            {
                FileName = fileName,
                FullPath = filePath,
                FileSize = fileInfo.Exists ? fileInfo.Length : 0
            };

            var regex = new Regex(@"^(.+?)_(.+?)_(\d{4}-\d{2}-\d{2})\s(\d{2})_(\d{2})-(\d{2})_(\d{2})_(.+)$");
            var match = regex.Match(fileName);

            if (match.Success)
            {
                video.CourseName = match.Groups[1].Value;
                video.Type = match.Groups[2].Value;

                string datePart = match.Groups[3].Value;
                string startHourPart = match.Groups[4].Value;
                string startMinutePart = match.Groups[5].Value;
                string endHourPart = match.Groups[6].Value;
                string endMinutePart = match.Groups[7].Value;

                video.Perspective = match.Groups[8].Value;

                string fullDateTimeString = $"{datePart} {startHourPart}:{startMinutePart}";
                if (DateTime.TryParseExact(fullDateTimeString, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime recordedDate))
                {
                    video.RecordDate = recordedDate;
                }

                video.TimeRange = $"{startHourPart}:{startMinutePart}-{endHourPart}:{endMinutePart}";
            }
            else
            {
                video.CourseName = StripPerspectiveSuffix(fileName);
                video.Perspective = GetPerspectiveFromSuffix(fileName);
            }
            return video;
        }

        private static string StripPerspectiveSuffix(string fileName)
        {
            foreach (var suffix in new[] { "_HDMI", "_教师", "_teacher" })
            {
                if (fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    return fileName[..^suffix.Length];
                }
            }

            return fileName;
        }

        private static IEnumerable<string> GetPartnerBaseNames(string baseName)
        {
            if (baseName.EndsWith("_HDMI", StringComparison.OrdinalIgnoreCase))
            {
                var prefix = baseName[..^"_HDMI".Length];
                yield return prefix + "_教师";
                yield return prefix + "_teacher";
            }
            else if (baseName.EndsWith("_教师", StringComparison.OrdinalIgnoreCase))
            {
                yield return baseName[..^"_教师".Length] + "_HDMI";
            }
            else if (baseName.EndsWith("_teacher", StringComparison.OrdinalIgnoreCase))
            {
                yield return baseName[..^"_teacher".Length] + "_HDMI";
            }
        }

        private static IEnumerable<string> GetSubtitleCandidates(IEnumerable<string?> directories)
        {
            var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var directory in directories.Where(directory => !string.IsNullOrWhiteSpace(directory)))
            {
                if (!Directory.Exists(directory))
                {
                    continue;
                }

                foreach (var extension in new[] { "*.vtt", "*.srt" })
                {
                    foreach (var subtitlePath in Directory.EnumerateFiles(directory!, extension, SearchOption.TopDirectoryOnly))
                    {
                        if (seenPaths.Add(subtitlePath))
                        {
                            yield return subtitlePath;
                        }
                    }
                }
            }
        }

        private static bool IsSubtitleMatch(string videoBaseName, string subtitleBaseName)
        {
            var normalizedVideo = NormalizeForSubtitleMatch(videoBaseName);
            var normalizedSubtitle = NormalizeForSubtitleMatch(subtitleBaseName);
            return !string.IsNullOrWhiteSpace(normalizedVideo)
                && string.Equals(normalizedVideo, normalizedSubtitle, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeForSubtitleMatch(string value)
        {
            return new string(FileNameService.SanitizeFileName(value)
                .Where(character => char.IsLetterOrDigit(character))
                .ToArray());
        }

        private static string GetPerspectiveFromSuffix(string fileName)
        {
            foreach (var perspective in new[] { "HDMI", "教师", "teacher" })
            {
                if (fileName.EndsWith("_" + perspective, StringComparison.OrdinalIgnoreCase))
                {
                    return perspective;
                }
            }

            return string.Empty;
        }
    }
}
