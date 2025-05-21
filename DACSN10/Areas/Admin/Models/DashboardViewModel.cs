namespace DACSN10.Areas.Admin.Models
{
    public class DashboardViewModel
    {
        // Thống kê chung
        public int TotalUsers { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalStudents { get; set; }
        public int TotalCourses { get; set; }
        public int TotalCategories { get; set; }

        // Thống kê doanh thu
        public decimal TotalRevenue { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public decimal WeeklyRevenue { get; set; }

        // Khóa học chờ duyệt
        public int PendingCourses { get; set; }

        // Khóa học mới trong tuần
        public int NewCoursesThisWeek { get; set; }

        // Người dùng mới trong tuần
        public int NewUsersThisWeek { get; set; }
    }
}