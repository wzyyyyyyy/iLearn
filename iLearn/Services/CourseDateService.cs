using iLearn.Models;
using LiteDB;
using System.IO;

namespace iLearn.Services
{
    public class CourseDateService
    {
        private readonly LiteDatabase? _db;

        public CourseDateService()
        {
            var uri = new Uri("pack://application:,,,/Assets/CourseDate.db");
            var localPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "iLearn", "CourseDate.db");

            if (!File.Exists(localPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
                using var resourceStream = Application.GetResourceStream(uri)?.Stream;

                if (resourceStream is null)
                {
                    return; // Resource not found
                }

                using var fileStream = new FileStream(localPath, FileMode.Create, FileAccess.Write);
                resourceStream.CopyTo(fileStream);
            }

            _db = new LiteDatabase(new ConnectionString
            {
                Filename = localPath,
            });
        }

        public List<LocalCourseData> GetLocalCourseDatas()
        {
            if (_db is null)
            {
                return [];
            }

            var col = _db.GetCollection<LocalCourseData>("courses");
            return [.. col.FindAll()];
        }
    }
}
