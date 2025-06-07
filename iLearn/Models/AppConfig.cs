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
