using System.Text.Json;

namespace iLearn.Models
{
    public partial class LiveAndRecordInfo : ObservableObject
    {
        public string Id { get; set; } = string.Empty;
        public string ResourceId { get; set; } = string.Empty;
        public string LiveRecordName { get; set; } = string.Empty;
        public string BuildingName { get; set; } = string.Empty;
        public string CurrentWeek { get; set; } = string.Empty;
        public string CurrentDay { get; set; } = string.Empty;
        public string CurrentDate { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public string RoomId { get; set; } = string.Empty;
        public string IsAllowDownload { get; set; } = string.Empty;
        public string IsNowPlay { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public string CourseId { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string ClassIds { get; set; } = string.Empty;
        public string ClassNames { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public string TimeRange { get; set; } = string.Empty;
        public string IsOpen { get; set; } = string.Empty;
        public string IsAction { get; set; } = string.Empty;
        public string LiveStatus { get; set; } = string.Empty;
        public string SchImgUrl { get; set; } = string.Empty;
        public string VideoTimes { get; set; } = string.Empty;
        public string? VideoSubTime { get; set; }
        public string ClassType { get; set; } = string.Empty;
        public string VideoPath { get; set; } = string.Empty;
        public List<VideoClass> VideoClassMap { get; set; } = [];
        public string? ResourceFileType { get; set; }
        public string LivePath { get; set; } = string.Empty;
        public string RoomType { get; set; } = string.Empty;
        public string ScheduleTimeStart { get; set; } = string.Empty;
        public string ScheduleTimeEnd { get; set; } = string.Empty;

        [ObservableProperty]
        private bool _isHdmiSelected = false;

        [ObservableProperty]
        private bool _isTeacherSelected = false;

        public static List<LiveAndRecordInfo> Parse(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var dataListElement = doc.RootElement
                .GetProperty("data")
                .GetProperty("dataList");

            return JsonSerializer.Deserialize<List<LiveAndRecordInfo>>(dataListElement.GetRawText(), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<LiveAndRecordInfo>();
        }
    }

    public class VideoClass
    {
        public string Id { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
    }
}
