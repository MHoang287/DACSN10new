using Microsoft.AspNetCore.Mvc;
using DACSN10.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DACSN10.Controllers
{
    public class PaymentController : Controller
    {
        private readonly AppDbContext _context;

        public PaymentController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult SelectPayment(int courseId)
        {
            var course = _context.Courses.FirstOrDefault(c => c.CourseID == courseId && c.TrangThai == "Active");
            if (course == null)
            {
                return NotFound();
            }
            return View(course);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayWithVNPAY(int courseId)
        {
            return await ProcessPayment(courseId, "VNPAY");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayWithMomo(int courseId)
        {
            return await ProcessPayment(courseId, "MOMO");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayWithVisa(int courseId)
        {
            return await ProcessPayment(courseId, "VISA");
        }

        private async Task<IActionResult> ProcessPayment(int courseId, string paymentMethod)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Vui lòng đăng nhập để tiếp tục thanh toán.";
                return RedirectToAction("Login", "Account");
            }

            var course = _context.Courses.FirstOrDefault(c => c.CourseID == courseId && c.TrangThai == "Active");
            if (course == null)
            {
                TempData["Error"] = "Không tìm thấy khóa học.";
                return RedirectToAction("Index", "Course");
            }

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

            if (payment.SoTien != course.Gia)
            {
                TempData["Error"] = "Giá khóa học không hợp lệ.";
                return RedirectToAction("SelectPayment", new { courseId });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                bool paymentSuccess = await SimulatePaymentGateway(payment);

                if (paymentSuccess)
                {
                    payment.Status = PaymentStatus.Success;

                    var enrollment = new Enrollment
                    {
                        CourseID = courseId,
                        UserID = userId,
                        EnrollDate = DateTime.Now,
                        TrangThai = "Active",
                        Progress = 0
                    };
                    _context.Enrollments.Add(enrollment);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                else
                {
                    payment.Status = PaymentStatus.Failed;
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }

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
            catch
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Lỗi trong quá trình thanh toán.";
                return RedirectToAction("SelectPayment", new { courseId });
            }
        }

        private async Task<bool> SimulatePaymentGateway(Payment payment)
        {
            await Task.Delay(1000);
            return new Random().Next(0, 2) == 1;
        }

        [HttpGet]
        public IActionResult History()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var history = _context.Payments
                .AsNoTracking()
                .Include(p => p.Course)
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