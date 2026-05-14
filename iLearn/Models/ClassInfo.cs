using System.Text.Json;

namespace iLearn.Models
{
    public class ClassInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string CourseId { get; set; }
        public string CourseName { get; set; }
        public string Cover { get; set; }
        public string TeacherId { get; set; }
        public string TeacherName { get; set; }
        public string Status { get; set; }
        public string StatusName { get; set; }
        public string? TermYear { get; set; }
        public string? Term { get; set; }
        public string? Code { get; set; }
        public string? WeekTime { get; set; }
        public int? StudentCount { get; set; }
        public string? CodePath { get; set; }
        public string? TeaImg { get; set; }
        public string ClassId { get; set; }
        public string ClassroomId { get; set; }
        public string? ClassName { get; set; }
        public string TeacherUsername { get; set; }
        public string SchoolId { get; set; }
        public string StudentId { get; set; }
        public string Type { get; set; }
        public string? TypeName { get; set; }
        public string? TermId { get; set; }
        public string? MirrorTeachClassId { get; set; }
        public string? SchoolName { get; set; }
        public bool? IsAreaCourse { get; set; }
        public string? OpenCourseId { get; set; }
        public string? OpenType { get; set; }
        public string? AreaSchoolNames { get; set; }


        public static List<ClassInfo> Parse(string json)
        {
            using var doc = JsonDocument.Parse(json);

            var dataListElement = doc.RootElement
                .GetProperty("data")
                .GetProperty("dataList");

            var classInfos = JsonSerializer.Deserialize<List<ClassInfo>>(dataListElement.GetRawText(), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return classInfos ?? new List<ClassInfo>();
        }
    }
}
