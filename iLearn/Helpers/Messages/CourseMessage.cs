using iLearn.Models;

namespace iLearn.Helpers.Messages
{
    internal record class CourseMessage
    {
        public ClassInfo classInfo { get; set; }
    }
}
