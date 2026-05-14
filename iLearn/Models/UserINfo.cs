using System.Text.Json;

namespace iLearn.Models
{
    public class UserInfo
    {
        public string StudentId { get; set; }
        public string Msg { get; set; }
        public string StudyNo { get; set; }
        public string StudentName { get; set; }
        public string SchoolName { get; set; }
        public string UserName { get; set; }
        public string HeadPic { get; set; }
        public string MemberId { get; set; }

        public static UserInfo Parse(string json)
        {
            using var doc = JsonDocument.Parse(json);

            var dataElement = doc.RootElement
                .GetProperty("data");

            var userInfo = JsonSerializer.Deserialize<UserInfo>(dataElement.GetRawText(), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return userInfo ?? new UserInfo();
        }
    }
}
