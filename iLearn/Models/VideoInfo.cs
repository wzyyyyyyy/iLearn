namespace iLearn.Models
{
    //你吉为啥整这么多结构不同的VideoInfo
    public class VideoInfo
    {
        public int DetectKnowledgeStatus { get; set; }
        public string LiveRecordId { get; set; } = string.Empty;
        public int EnableWater { get; set; }
        public List<string> TeacherList { get; set; } = [];
        public string ClassNames { get; set; } = string.Empty;
        public List<Video> VideoList { get; set; } = [];
        public int TransPhaseStatus { get; set; }
        public int Company { get; set; }
        public string CourseId { get; set; } = string.Empty;
        public object? VideoCutStatus { get; set; }
        public string ScheduleId { get; set; } = string.Empty;
        public string ResourceCover { get; set; } = string.Empty;
        public string PhaseUrl { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public string TeacherIds { get; set; } = string.Empty;
        public string ResourceName { get; set; } = string.Empty;
        public int IsPublish { get; set; }
        public object? ParentId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string AudioPath { get; set; } = string.Empty;
        public int CommentStatus { get; set; }
        public string BuildingName { get; set; } = string.Empty;
        public string CreateId { get; set; } = string.Empty;
        public int ClassType { get; set; }
        public List<object> SilenceList { get; set; } = [];
        public int ResourceType { get; set; }

        public static VideoInfo Parse(string json)
        {
            return JsonApiResponse.DeserializeDataObject<VideoInfo>(json, "课程服务未返回视频详情");
        }
    }

    public class Video
    {
        public string Id { get; set; } = string.Empty;
        public string VideoCode { get; set; } = string.Empty;
        public string VideoName { get; set; } = string.Empty;
        public string VideoPath { get; set; } = string.Empty;
        public string VideoSize { get; set; } = string.Empty;
    }
}
