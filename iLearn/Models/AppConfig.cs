using System.IO;
using System.Text.Json;

namespace iLearn.Models
{
    public class AppConfig
    {
        public string? UserName { get; set; }
        public string? UserPassword { get; set; }
        public bool IsRememberMeEnabled { get; set; } = false;
        public bool IsAutoLoginEnabled { get; set; } = false;

        public int MaxConcurrentDownloads { get; set; } = 3;
        public int ChunkCount { get; set; } = 8;
        public long SpeedLimitBytesPerSecond { get; set; } = 0; // 0 表示不限速
        public string DownloadPath { get; set; } = Path.Combine(Environment.CurrentDirectory, "Downloads");

        private readonly string _filePath;

        public AppConfig()
        {
        }

        public AppConfig(string filePath)
        {
            _filePath = filePath;

            if (!File.Exists(filePath))
            {
                Save();
            }
            else
            {
                var json = File.ReadAllText(filePath);
                var config = JsonSerializer.Deserialize<AppConfig>(json);
                if (config != null)
                {
                    UserName = config.UserName;
                    UserPassword = config.UserPassword;
                    IsRememberMeEnabled = config.IsRememberMeEnabled;
                    IsAutoLoginEnabled = config.IsAutoLoginEnabled;
                    MaxConcurrentDownloads = config.MaxConcurrentDownloads;
                    ChunkCount = config.ChunkCount;
                    SpeedLimitBytesPerSecond = config.SpeedLimitBytesPerSecond;
                    DownloadPath = config.DownloadPath ?? Path.Combine(Environment.CurrentDirectory, "Downloads");
                }
            }
        }

        public void Save()
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
    }
}