using System.Text.Json;

namespace iLearn.Models
{
    //你吉为啥整这么多结构不同的VideoInfo
    public class VideoInfo
    {
        public int DetectKnowledgeStatus { get; set; }
        public string LiveRecordId { get; set; }
        public int EnableWater { get; set; }
        public List<string> TeacherList { get; set; }
        public string ClassNames { get; set; }
        public List<Video> VideoList { get; set; }
        public int TransPhaseStatus { get; set; }
        public int Company { get; set; }
        public string CourseId { get; set; }
        public object VideoCutStatus { get; set; }
        public string ScheduleId { get; set; }
        public string ResourceCover { get; set; }
        public string PhaseUrl { get; set; }
        public string TeacherName { get; set; }
        public string TeacherIds { get; set; }
        public string ResourceName { get; set; }
        public int IsPublish { get; set; }
        public object ParentId { get; set; }
        public string RoomName { get; set; }
        public string AudioPath { get; set; }
        public int CommentStatus { get; set; }
        public string BuildingName { get; set; }
        public string CreateId { get; set; }
        public int ClassType { get; set; }
        public List<object> SilenceList { get; set; }
        public int ResourceType { get; set; }

        public static VideoInfo Parse(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var dataListElement = doc.RootElement
                .GetProperty("data");

            return JsonSerializer.Deserialize<VideoInfo>(dataListElement.GetRawText(), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new VideoInfo();
        }
    }

    public class Video
    {
        public string Id { get; set; }
        public string VideoCode { get; set; }
        public string VideoName { get; set; }
        public string VideoPath { get; set; }
        public string VideoSize { get; set; }
    }
}
