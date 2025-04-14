using Microsoft.AspNetCore.Mvc;
using DACSN10.Models;
using System.Linq;

namespace DACSN10.Controllers
{
    public class PaymentController : Controller
    {
        private readonly AppDbContext _context;

        public PaymentController(AppDbContext context)
        {
            _context = context;
        }

        // Thanh toán bằng VNPAY
        [HttpPost]
        public IActionResult PayWithVNPAY(int courseId)
        {
            var userId = User.Identity.Name;
            _context.Payments.Add(new Payment
            {
                CourseID = courseId,
                UserID = userId,
                PhuongThucThanhToan = "VNPAY",
                NgayThanhToan = DateTime.Now,
                SoTien = _context.Courses.FirstOrDefault(c => c.CourseID == courseId)?.Gia ?? 0
            });
            _context.SaveChanges();
            return RedirectToAction("MyCourses", "Course");
        }

        // Thanh toán bằng MOMO
        [HttpPost]
        public IActionResult PayWithMomo(int courseId)
        {
            var userId = User.Identity.Name;
            _context.Payments.Add(new Payment
            {
                CourseID = courseId,
                UserID = userId,
                PhuongThucThanhToan = "MOMO",
                NgayThanhToan = DateTime.Now,
                SoTien = _context.Courses.FirstOrDefault(c => c.CourseID == courseId)?.Gia ?? 0
            });
            _context.SaveChanges();
            return RedirectToAction("MyCourses", "Course");
        }

        // Thanh toán bằng VISA
        [HttpPost]
        public IActionResult PayWithVisa(int courseId)
        {
            var userId = User.Identity.Name;
            _context.Payments.Add(new Payment
            {
                CourseID = courseId,
                UserID = userId,
                PhuongThucThanhToan = "VISA",
                NgayThanhToan = DateTime.Now,
                SoTien = _context.Courses.FirstOrDefault(c => c.CourseID == courseId)?.Gia ?? 0
            });
            _context.SaveChanges();
            return RedirectToAction("MyCourses", "Course");
        }

        // Lịch sử thanh toán
        public IActionResult History()
        {
            var userId = User.Identity.Name;
            var history = _context.Payments
                                  .Where(p => p.UserID == userId)
                                  .OrderByDescending(p => p.NgayThanhToan)
                                  .ToList();
            return View(history);
        }
    }
}
