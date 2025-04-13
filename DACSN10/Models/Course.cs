namespace DACSN10.Models
{
    public class Course
    {
        public int CourseID { get; set; }
        public string TenKhoaHoc { get; set; }
        public string MoTa { get; set; }
        public decimal Gia { get; set; }
        public string TrangThai { get; set; }
        public DateTime NgayTao { get; set; }

        public string UserID { get; set; }
        public User User { get; set; }

        public ICollection<Lesson> Lessons { get; set; }
        public ICollection<Assignment> Assignments { get; set; }
        public ICollection<Payment> Payments { get; set; }
        public ICollection<Enrollment> Enrollments { get; set; }

        public ICollection<Quiz> Quizzes { get; set; }
        public ICollection<FavoriteCourse> FavoriteCourses { get; set; }
        public ICollection<CourseCategory> CourseCategories { get; set; }
    }

}
