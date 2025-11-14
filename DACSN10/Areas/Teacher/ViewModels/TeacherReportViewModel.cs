using System;
using System.Collections.Generic;

namespace DACSN10.Areas.Teacher.ViewModels
{
    public class CoursePerformanceDto
    {
        public int CourseID { get; set; }
        public string TenKhoaHoc { get; set; } = string.Empty;
        public string TrangThai { get; set; } = string.Empty;

        // Lifetime
        public int TotalStudents { get; set; }
        public decimal TotalRevenue { get; set; }
        public double AverageProgress { get; set; }
        public double CompletionRateCourse { get; set; } // % enroll progress >=100 trên khóa

        // Theo kỳ đang chọn (period)
        public int PeriodNewStudents { get; set; }
        public decimal PeriodRevenue { get; set; }
    }

    public class MonthlyRevenueDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Revenue { get; set; }
        public int Count { get; set; }
    }

    public class WeeklyStudentAnalyticsDto
    {
        public string Label { get; set; } = string.Empty;
        public int NewStudents { get; set; }
        public int CompletedStudents { get; set; }
    }

    public class CategoryDistributionDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public int CourseCount { get; set; }
        public int StudentCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class TeacherReportSummaryDto
    {
        public string PeriodKey { get; set; } = "30d";      // 30d | month | prev-month | range
        public string PeriodLabel { get; set; } = "30 ngày qua";
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }             // exclusive

        public decimal PeriodRevenue { get; set; }
        public int PeriodNewStudents { get; set; }

        public double AverageRating { get; set; }           // 0-5 (quy đổi từ quiz score)
        public double AverageQuizScore { get; set; }        // 0-100
        public double GlobalCompletionRate { get; set; }    // %
        public int TotalActiveStudents { get; set; }
        public int TotalCourses { get; set; }
    }

    public class TeacherReportViewModel
    {
        public TeacherReportSummaryDto Summary { get; set; } = new();
        public List<CoursePerformanceDto> CoursePerformance { get; set; } = new();
        public List<MonthlyRevenueDto> MonthlyRevenue { get; set; } = new();
        public List<WeeklyStudentAnalyticsDto> WeeklyAnalytics { get; set; } = new();
        public List<CategoryDistributionDto> CategoryDistribution { get; set; } = new();
    }
}