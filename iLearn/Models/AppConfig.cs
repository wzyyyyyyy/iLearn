using System.Text.Json;

namespace iLearn.Models
{
    public class AppConfig
    {
        public string? UserName { get; set; }

        public bool IsRememberMeEnabled { get; set; } = false;
        public bool IsAutoLoginEnabled { get; set; } = false;

        public int MaxConcurrentDownloads { get; set; } = 3;
        public int ChunkCount { get; set; } = 8;
        public long SpeedLimitBytesPerSecond { get; set; } = 0;

        public string DownloadPath { get; set; } = GetDefaultDownloadPath();

        private readonly string? _filePath;

        public AppConfig()
        {
        }

        public AppConfig(string filePath)
        {
            _filePath = filePath;
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            if (!File.Exists(filePath))
            {
                Directory.CreateDirectory(DownloadPath);
                Save();
            }
            else
            {
                var json = File.ReadAllText(filePath);
                var config = JsonSerializer.Deserialize<AppConfig>(json);
                if (config != null)
                {
                    UserName = config.UserName;
                    IsRememberMeEnabled = config.IsRememberMeEnabled;
                    IsAutoLoginEnabled = config.IsAutoLoginEnabled;
                    MaxConcurrentDownloads = config.MaxConcurrentDownloads;
                    ChunkCount = config.ChunkCount;
                    SpeedLimitBytesPerSecond = config.SpeedLimitBytesPerSecond;
                    DownloadPath = string.IsNullOrWhiteSpace(config.DownloadPath)
                        ? GetDefaultDownloadPath()
                        : config.DownloadPath;
                }

                Directory.CreateDirectory(DownloadPath);
            }
        }

        public void Save()
        {
            if (_filePath is null)
                return;

            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }

        private static string GetDefaultDownloadPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "iLearnVideo");
        }
    }
}
