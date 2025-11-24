namespace DACSN10.Areas.Teacher.ViewModels
{
    public class TopCourseStat
    {
        public int CourseID { get; set; }
        public string TenKhoaHoc { get; set; }
        public int StudentCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class RevenuePoint
    {
        public string Label { get; set; }   // Ví dụ: "T1/25"
        public decimal Revenue { get; set; }
    }

    public class StudentDistribution
    {
        public int DangHoc { get; set; }    // Active & Progress < 100
        public int HoanThanh { get; set; }  // Active & Progress >= 100
        public int TamDung { get; set; }    // Trạng thái khác Active
    }

    public class TeacherStatisticsViewModel
    {
        // Thống kê tổng quan
        public int TotalCourses { get; set; }
        public int ActiveCourses { get; set; }
        public int TotalStudents { get; set; }
        public decimal TotalRevenue { get; set; }
        public int Followers { get; set; } // nếu sau này có bảng followers thì thay bằng số thật

        // Doanh thu theo tháng (6 tháng gần nhất)
        public List<RevenuePoint> RevenueByMonth { get; set; } = new();

        // Top khóa học
        public List<TopCourseStat> TopCourses { get; set; } = new();

        // Phân bố học viên
        public StudentDistribution StudentDistribution { get; set; } = new();

        // Một số insight khác
        public double CompletionRate { get; set; }              // %
        public double AverageRating { get; set; }               // 0–5
        public double AverageStudyHoursPerWeek { get; set; }    // giờ/tuần (tạm, nếu chưa có log thời gian)
    }
}