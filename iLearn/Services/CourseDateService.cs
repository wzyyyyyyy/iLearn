using iLearn.Models;
using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace iLearn.Services
{
    public class CourseDateService
    {
        private readonly LiteDatabase _db;

        public CourseDateService() 
        {
            var uri = new Uri("pack://application:,,,/Assets/CourseDate.db");
            var localPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "iLearn", "CourseDate.db");

            if (!File.Exists(localPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
                using var resourceStream = Application.GetResourceStream(uri)?.Stream
                    ?? throw new FileNotFoundException("嵌入资源 CourseDate.db 未找到");
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
            var col = _db.GetCollection<LocalCourseData>("courses");
            return [.. col.FindAll()];
        }
    }
}
