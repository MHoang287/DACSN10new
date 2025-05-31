using System.ComponentModel.DataAnnotations;

namespace DACSN10.Models
{
    public class PaymentViewModel
    {
        public int CourseID { get; set; }

        [Display(Name = "Tên khóa học")]
        public string CourseName { get; set; }

        [Display(Name = "Giá khóa học")]
        public decimal CoursePrice { get; set; }

        [Display(Name = "Giảng viên")]
        public string TeacherName { get; set; }

        [Display(Name = "Email")]
        public string UserEmail { get; set; }

        [Display(Name = "Họ tên")]
        public string UserName { get; set; }

        [Display(Name = "Phương thức thanh toán")]
        public string PaymentMethod { get; set; } = "MoMo";
    }

    public class PaymentStatsViewModel
    {
        public int TotalPayments { get; set; }
        public int SuccessCount { get; set; }
        public int PendingCount { get; set; }
        public int FailedCount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}