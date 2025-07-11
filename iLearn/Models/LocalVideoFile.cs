using CommunityToolkit.Mvvm.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

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
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        public LocalVideoFile GetPartnerVideo()
        {
            switch (Perspective)
            {
                case "HDMI": 
                    {
                        var path = FullPath.Replace("_HDMI.mp4", "_教师.mp4");

                        if (File.Exists(path))
                        {
                            return FromFileName(path);
                        }
                        else
                        {
                            return null;
                        }
                    }
                case "教师视角": 
                    {
                        var path = FullPath.Replace("_教师.mp4", "_HDMI.mp4");

                        if (File.Exists(path))
                        {
                            return FromFileName(path);
                        }
                        else
                        {
                            return null;
                        }
                    }
                default: 
                    { 
                        return null;
                    }
            };
        }

        public static LocalVideoFile FromFileName(string filePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var fileInfo = new FileInfo(filePath);

            var video = new LocalVideoFile
            {
                FileName = fileName,
                FullPath = filePath,
                FileSize = fileInfo.Length
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
                MessageBox.Show($"文件名 '{fileName}' 格式不符合预期，无法解析。");
            }
            return video;
        }
    }
}