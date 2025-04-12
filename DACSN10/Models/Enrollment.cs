namespace DACSN10.Models
{
    public class Enrollment
    {
        public int EnrollmentID { get; set; } // 👈 Đây là khóa chính

        public DateTime NgayDangKy { get; set; }
        public string TrangThai { get; set; }

        public int CourseID { get; set; }
        public Course Course { get; set; }

        public string UserID { get; set; }
        public User User { get; set; }
    }
}
