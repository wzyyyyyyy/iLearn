using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace iLearn.Models
{
    public class AppConfig
    {
        [JsonIgnore]
        public string? UserPassword
        {
            get
            {
                if (string.IsNullOrEmpty(EncryptedUserPassword))
                    return null;
                try
                {
                    var encryptedBytes = Convert.FromBase64String(EncryptedUserPassword);
                    var decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
                    return Encoding.UTF8.GetString(decryptedBytes);
                }
                catch
                {
                    return null;
                }
            }
            set
            {
                if (value == null)
                {
                    EncryptedUserPassword = null;
                }
                else
                {
                    var plainBytes = Encoding.UTF8.GetBytes(value);
                    var encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
                    EncryptedUserPassword = Convert.ToBase64String(encryptedBytes);
                }
            }
        }

        public string? UserName { get; set; }
        public string? EncryptedUserPassword { get; set; }

        public bool IsRememberMeEnabled { get; set; } = false;
        public bool IsAutoLoginEnabled { get; set; } = false;

        public int MaxConcurrentDownloads { get; set; } = 3;
        public int ChunkCount { get; set; } = 8;
        public long SpeedLimitBytesPerSecond { get; set; } = 0;

        public string DownloadPath { get; set; }

        private readonly string _filePath;

        public AppConfig()
        {
        }

        public AppConfig(string filePath)
        {
            _filePath = filePath;

            if (!File.Exists(filePath))
            {
                string directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                DownloadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "iLearnVideo");
                Save();
            }
            else
            {
                var json = File.ReadAllText(filePath);
                var config = JsonSerializer.Deserialize<AppConfig>(json);
                if (config != null)
                {
                    UserName = config.UserName;
                    EncryptedUserPassword = config.EncryptedUserPassword; // 用于解密
                    IsRememberMeEnabled = config.IsRememberMeEnabled;
                    IsAutoLoginEnabled = config.IsAutoLoginEnabled;
                    MaxConcurrentDownloads = config.MaxConcurrentDownloads;
                    ChunkCount = config.ChunkCount;
                    SpeedLimitBytesPerSecond = config.SpeedLimitBytesPerSecond;
                    DownloadPath = config.DownloadPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                        "iLearnVideo");

                    if (!Directory.Exists(DownloadPath))
                    {
                        Directory.CreateDirectory(DownloadPath);
                    }
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
