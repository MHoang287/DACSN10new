using System.ComponentModel.DataAnnotations;

namespace DACSN10.Models
{
    public class Payment
    {
        public int PaymentID { get; set; }
        public decimal SoTien { get; set; }
        public DateTime NgayThanhToan { get; set; }
        public string PhuongThucThanhToan { get; set; }
        public PaymentStatus Status { get; set; }
        public int CourseID { get; set; }
        public Course Course { get; set; }

        public string UserID { get; set; }
        public User User { get; set; }
    }
}