using iLearn.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iLearn.Helpers.Messages
{
    internal record class CourseMessage
    {
        public ClassInfo classInfo { get; set; }
    }
}
