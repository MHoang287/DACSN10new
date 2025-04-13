using Microsoft.AspNetCore.Identity;

namespace DACSN10.Models
{
    public class User : IdentityUser
    {

        public string HoTen { get; set; }
        public DateTime NgayDangKy { get; set; }
        public string TrangThai { get; set; }
        public string LoaiNguoiDung { get; set; } = RoleNames.User; // Giá trị mặc định admin, teacher, user

        public ICollection<Course> Courses { get; set; }
        public ICollection<Submission> Submissions { get; set; }
        public ICollection<Payment> Payments { get; set; }
        public ICollection<Enrollment> Enrollments { get; set; }

        public ICollection<FavoriteCourse> FavoriteCourses { get; set; }
        public ICollection<Follow> Followers { get; set; }        // Người khác follow tôi
        public ICollection<Follow> Following { get; set; }        // Tôi follow người khác
        public ICollection<QuizResult> QuizResults { get; set; }
    }
}
