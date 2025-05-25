using System.Diagnostics;
using DACSN10.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DACSN10.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Lấy tất cả khóa học đã được publish với thông tin liên quan
                var courses = await _context.Courses
                    .AsNoTracking()
                    .Include(c => c.Enrollments)
                    .Include(c => c.Lessons)
                    .Include(c => c.User)
                    .Where(c => c.TrangThai == "Published")
                    .OrderByDescending(c => c.NgayTao)
                    .ToListAsync();

                _logger.LogInformation($"Loaded {courses.Count} courses for home page");
                return View(courses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading courses for home page");
                return View(new List<Course>());
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // API để lấy thống kê
        [HttpGet]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var stats = new
                {
                    TotalCourses = await _context.Courses.CountAsync(c => c.TrangThai == "Published"),
                    TotalStudents = await _context.Enrollments.Select(e => e.UserID).Distinct().CountAsync(),
                    TotalLessons = await _context.Lessons
                        .Where(l => l.Course.TrangThai == "Published")
                        .CountAsync(),
                    TotalEnrollments = await _context.Enrollments.CountAsync()
                };

                return Json(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting statistics");
                return Json(new { error = "Unable to load statistics" });
            }
        }
    }
}