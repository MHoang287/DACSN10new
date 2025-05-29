namespace DACSN10.Models
{
    public class Lesson
    {
        public int LessonID { get; set; }
        public string TenBaiHoc { get; set; }
        public string NoiDung { get; set; }
        public int ThoiLuong { get; set; }
        public string VideoUrl { get; set; }
        public int CourseID { get; set; }
        public Course Course { get; set; }
        public bool IsVideoRequiredComplete { get; set; } // mới thêm
    }

}
