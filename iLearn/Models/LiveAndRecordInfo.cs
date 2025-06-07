using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace iLearn.Models
{
    public class VideoClass
    {
        public string VideoClassId { get; set; }
        public string VideoName { get; set; }
    }

    public class LiveAndRecordInfo
    {
        public string Id { get; set; }
        public string ResourceId { get; set; }
        public string LiveRecordName { get; set; }
        public string BuildingName { get; set; }
        public string CurrentWeek { get; set; }
        public string CurrentDay { get; set; }
        public string CurrentDate { get; set; }
        public string RoomName { get; set; }
        public string RoomId { get; set; }
        public string IsAllowDownload { get; set; }
        public string IsNowPlay { get; set; }
        public string TeacherName { get; set; }
        public string CourseId { get; set; }
        public string CourseName { get; set; }
        public string ClassIds { get; set; }
        public string ClassNames { get; set; }
        public string Section { get; set; }
        public string TimeRange { get; set; }
        public string IsOpen { get; set; }
        public string IsAction { get; set; }
        public string LiveStatus { get; set; }
        public string SchImgUrl { get; set; }
        public string VideoTimes { get; set; }
        public string? VideoSubTime { get; set; }
        public string ClassType { get; set; }
        public string VideoPath { get; set; }
        public List<VideoClass> VideoClassMap { get; set; }
        public string? ResourceFileType { get; set; }
        public string LivePath { get; set; }
        public string RoomType { get; set; }
        public string ScheduleTimeStart { get; set; }
        public string ScheduleTimeEnd { get; set; }

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
}
