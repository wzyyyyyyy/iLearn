namespace iLearn.Models
{
    public class ClassInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CourseId { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string Cover { get; set; } = string.Empty;
        public string TeacherId { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusName { get; set; } = string.Empty;
        public string? TermYear { get; set; }
        public string? Term { get; set; }
        public string? Code { get; set; }
        public string? WeekTime { get; set; }
        public int? StudentCount { get; set; }
        public string? CodePath { get; set; }
        public string? TeaImg { get; set; }
        public string ClassId { get; set; } = string.Empty;
        public string ClassroomId { get; set; } = string.Empty;
        public string? ClassName { get; set; }
        public string TeacherUsername { get; set; } = string.Empty;
        public string SchoolId { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
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
            return JsonApiResponse.DeserializeDataList<ClassInfo>(json, "课程服务未返回课程数据");
        }
    }
}
