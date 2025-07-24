namespace iLearn.Models
{
    public class LocalCourseData
    {
        public int Id { get; set; }                      // LiteDB �������Զ�����
        public string Term { get; set; } = "";
        public string CourseId { get; set; } = "";
        public string CourseName { get; set; } = "";
        public string SectionId { get; set; } = "";
        public string TeacherName { get; set; } = "";
        public string Schedule { get; set; } = "";
    }
}