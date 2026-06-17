namespace iLearn.Models
{
    public class UserInfo
    {
        public string StudentId { get; set; } = string.Empty;
        public string Msg { get; set; } = string.Empty;
        public string StudyNo { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string SchoolName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string HeadPic { get; set; } = string.Empty;
        public string MemberId { get; set; } = string.Empty;

        public static UserInfo Parse(string json)
        {
            return JsonApiResponse.DeserializeDataObject<UserInfo>(json, "课程服务未返回用户信息");
        }
    }
}
