using Microsoft.AspNetCore.Mvc;
using DACSN10.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DACSN10.Controllers
{
    public class PaymentController : Controller
    {
        private readonly AppDbContext _context;

        public PaymentController(AppDbContext context)
        {
            _context = context;
        }

        // Hiển thị các tùy chọn thanh toán cho một khóa học
        [HttpGet]
        public IActionResult SelectPayment(int courseId)
        {
            var course = _context.Courses.FirstOrDefault(c => c.CourseID == courseId);
            if (course == null)
            {
                return NotFound();
            }
            return View(course);
        }

        // Thanh toán bằng VNPAY
        [HttpPost]
        public async Task<IActionResult> PayWithVNPAY(int courseId)
        {
            return await ProcessPayment(courseId, "VNPAY");
        }

        // Thanh toán bằng MOMO
        [HttpPost]
        public async Task<IActionResult> PayWithMomo(int courseId)
        {
            return await ProcessPayment(courseId, "MOMO");
        }

        // Thanh toán bằng VISA
        [HttpPost]
        public async Task<IActionResult> PayWithVisa(int courseId)
        {
            return await ProcessPayment(courseId, "VISA");
        }

        // Logic xử lý thanh toán chung
        private async Task<IActionResult> ProcessPayment(int courseId, string paymentMethod)
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["Error"] = "Vui lòng đăng nhập để tiếp tục thanh toán.";
                return RedirectToAction("Login", "Account");
            }

            var userId = User.Identity.Name;
            var course = _context.Courses.FirstOrDefault(c => c.CourseID == courseId);
            if (course == null)
            {
                TempData["Error"] = "Không tìm thấy khóa học.";
                return RedirectToAction("Index", "Course");
            }

            // Kiểm tra xem người dùng đã đăng ký chưa
            var existingEnrollment = _context.Enrollments
                .FirstOrDefault(e => e.CourseID == courseId && e.UserID == userId);
            if (existingEnrollment != null)
            {
                TempData["Error"] = "Bạn đã đăng ký khóa học này rồi.";
                return RedirectToAction("SelectPayment", new { courseId });
            }

            var payment = new Payment
            {
                CourseID = courseId,
                UserID = userId,
                PhuongThucThanhToan = paymentMethod,
                NgayThanhToan = DateTime.Now,
                SoTien = course.Gia,
                Status = PaymentStatus.Pending
            };

            _context.Payments.Add(payment);
            _context.SaveChanges();

            // Giả lập gọi đến cổng thanh toán
            bool paymentSuccess = await SimulatePaymentGateway(payment);

            if (paymentSuccess)
            {
                payment.Status = PaymentStatus.Success;

                // Tạo đăng ký khóa học
                var enrollment = new Enrollment
                {
                    CourseID = courseId,
                    UserID = userId,
                    EnrollDate = DateTime.Now,
                    TrangThai = "Active",
                    Progress = 0
                };
                _context.Enrollments.Add(enrollment);
            }
            else
            {
                payment.Status = PaymentStatus.Failed;
            }

            _context.SaveChanges();

            if (paymentSuccess)
            {
                TempData["Success"] = "Thanh toán thành công! Bạn đã được đăng ký vào khóa học.";
                return RedirectToAction("MyCourses", "Course");
            }
            else
            {
                TempData["Error"] = "Thanh toán thất bại. Vui lòng thử lại.";
                return RedirectToAction("SelectPayment", new { courseId });
            }
        }

        // Giả lập cổng thanh toán (sẽ được thay thế bằng tích hợp cổng thực tế)
        private async Task<bool> SimulatePaymentGateway(Payment payment)
        {
            // Giả lập gọi bất đồng bộ đến cổng thanh toán
            await Task.Delay(1000);
            // Ngẫu nhiên thành công hoặc thất bại để mô phỏng (sẽ được thay thế bằng phản hồi thực tế từ cổng thanh toán)
            return new Random().Next(0, 2) == 1;
        }

        // Lịch sử thanh toán
        [HttpGet]
        public IActionResult History()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = User.Identity.Name;
            var history = _context.Payments
                .Where(p => p.UserID == userId)
                .OrderByDescending(p => p.NgayThanhToan)
                .Select(p => new
                {
                    p.PaymentID,
                    p.SoTien,
                    p.NgayThanhToan,
                    p.PhuongThucThanhToan,
                    p.Status,
                    CourseName = p.Course.TenKhoaHoc
                })
                .ToList();

            return View(history);
        }
    }
}