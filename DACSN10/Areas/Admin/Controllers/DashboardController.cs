using DACSN10.Areas.Admin.Models;
using DACSN10.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DACSN10.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = RoleNames.Admin)]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;

        public DashboardController(AppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Admin/Dashboard
        public async Task<IActionResult> Index()
        {
            var viewModel = new DashboardViewModel();

            // Thống kê người dùng
            viewModel.TotalUsers = await _userManager.GetUsersInRoleAsync(RoleNames.User)
                .ContinueWith(t => t.Result.Count);

            viewModel.TotalTeachers = await _userManager.GetUsersInRoleAsync(RoleNames.Teacher)
                .ContinueWith(t => t.Result.Count);

            viewModel.TotalStudents = viewModel.TotalUsers; // Số học viên = số người dùng vai trò User

            // Thống kê khóa học và danh mục
            viewModel.TotalCourses = await _context.Courses.CountAsync();
            viewModel.TotalCategories = await _context.Categories.CountAsync();

            // Thống kê doanh thu
            viewModel.TotalRevenue = await _context.Payments
                .Where(p => p.Status == PaymentStatus.Success)
                .SumAsync(p => p.SoTien);

            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek); // Lấy chủ nhật tuần này

            viewModel.MonthlyRevenue = await _context.Payments
                .Where(p => p.Status == PaymentStatus.Success && p.NgayThanhToan >= startOfMonth)
                .SumAsync(p => p.SoTien);

            viewModel.WeeklyRevenue = await _context.Payments
                .Where(p => p.Status == PaymentStatus.Success && p.NgayThanhToan >= startOfWeek)
                .SumAsync(p => p.SoTien);

            // Thống kê khóa học chờ duyệt
            viewModel.PendingCourses = await _context.Courses
                .CountAsync(c => c.TrangThai == "Chờ duyệt");

            // Thống kê khóa học mới trong tuần
            viewModel.NewCoursesThisWeek = await _context.Courses
                .CountAsync(c => c.NgayTao >= startOfWeek);

            // Thống kê người dùng mới trong tuần
            viewModel.NewUsersThisWeek = await _context.Users
                .CountAsync(u => u.NgayDangKy >= startOfWeek);

            return View(viewModel);
        }
    }
}