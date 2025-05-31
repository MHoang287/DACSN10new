using Microsoft.AspNetCore.Mvc;
using DACSN10.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace DACSN10.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(AppDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Get featured courses (popular and recent courses)
                var featuredCourses = await GetFeaturedCoursesAsync();

                // Calculate statistics for homepage
                var statistics = await GetHomepageStatisticsAsync();

                // Pass statistics to ViewBag
                ViewBag.TotalStudents = statistics.TotalStudents;
                ViewBag.TotalCourses = statistics.TotalCourses;
                ViewBag.TotalLessons = statistics.TotalLessons;
                ViewBag.TotalTeachers = statistics.TotalTeachers;
                ViewBag.PublishedCourses = statistics.PublishedCourses;
                ViewBag.NewCoursesThisMonth = statistics.NewCoursesThisMonth;

                return View(featuredCourses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading homepage");

                // Return empty model if error occurs
                ViewBag.TotalStudents = 0;
                ViewBag.TotalCourses = 0;
                ViewBag.TotalLessons = 0;
                ViewBag.TotalTeachers = 0;
                ViewBag.PublishedCourses = 0;
                ViewBag.NewCoursesThisMonth = 0;

                return View(new List<Course>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Search(string keyword, string type = "course")
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return Json(new { success = false, message = "Vui lòng nhập từ khóa tìm kiếm." });
            }

            try
            {
                switch (type.ToLower())
                {
                    case "course":
                        return RedirectToAction("SearchByName", "Course", new { keyword });

                    case "teacher":
                        return RedirectToAction("SearchTeacher", "User", new { keyword });

                    case "category":
                        var category = await _context.Categories
                            .FirstOrDefaultAsync(c => c.Name.ToLower().Contains(keyword.ToLower()));

                        if (category != null)
                        {
                            return RedirectToAction("SearchByCategory", "Course", new { categoryId = category.CategoryID });
                        }
                        else
                        {
                            return RedirectToAction("SearchByName", "Course", new { keyword });
                        }

                    default:
                        return RedirectToAction("SearchByName", "Course", new { keyword });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in search with keyword: {Keyword}", keyword);
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSearchSuggestions(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return Json(new List<object>());
            }

            try
            {
                var suggestions = new List<object>();

                // Course suggestions
                var courseSuggestions = await _context.Courses
                    .AsNoTracking()
                    .Where(c => c.TrangThai == "Active" && c.TenKhoaHoc.ToLower().Contains(term.ToLower()))
                    .Take(5)
                    .Select(c => new
                    {
                        type = "course",
                        title = c.TenKhoaHoc,
                        subtitle = $"{c.Enrollments.Count()} học viên",
                        url = Url.Action("Details", "Course", new { id = c.CourseID })
                    })
                    .ToListAsync();

                // Teacher suggestions
                var teacherSuggestions = await _context.Users
                    .AsNoTracking()
                    .Where(u => u.LoaiNguoiDung == RoleNames.Teacher && u.HoTen.ToLower().Contains(term.ToLower()))
                    .Take(3)
                    .Select(u => new
                    {
                        type = "teacher",
                        title = u.HoTen,
                        subtitle = $"{_context.Courses.Count(c => c.UserID == u.Id && c.TrangThai == "Active")} khóa học",
                        url = Url.Action("TeacherProfile", "User", new { id = u.Id })
                    })
                    .ToListAsync();

                // Category suggestions
                var categorySuggestions = await _context.Categories
                    .AsNoTracking()
                    .Where(c => c.Name.ToLower().Contains(term.ToLower()))
                    .Take(2)
                    .Select(c => new
                    {
                        type = "category",
                        title = c.Name,
                        subtitle = $"{c.CourseCategories.Count()} khóa học",
                        url = Url.Action("SearchByCategory", "Course", new { categoryId = c.CategoryID })
                    })
                    .ToListAsync();

                suggestions.AddRange(courseSuggestions);
                suggestions.AddRange(teacherSuggestions);
                suggestions.AddRange(categorySuggestions);

                return Json(suggestions.Take(10));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting search suggestions for term: {Term}", term);
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPopularCourses(int count = 6)
        {
            try
            {
                var popularCourses = await _context.Courses
                    .AsNoTracking()
                    .Include(c => c.User)
                    .Include(c => c.Enrollments)
                    .Where(c => c.TrangThai == "Active")
                    .OrderByDescending(c => c.Enrollments.Count)
                    .Take(count)
                    .Select(c => new
                    {
                        c.CourseID,
                        c.TenKhoaHoc,
                        c.MoTa,
                        c.Gia,
                        TeacherName = c.User.HoTen,
                        StudentCount = c.Enrollments.Count,
                        LessonCount = c.Lessons.Count(),
                        Url = Url.Action("Details", "Course", new { id = c.CourseID })
                    })
                    .ToListAsync();

                return Json(popularCourses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting popular courses");
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetNewCourses(int count = 6)
        {
            try
            {
                var newCourses = await _context.Courses
                    .AsNoTracking()
                    .Include(c => c.User)
                    .Include(c => c.Enrollments)
                    .Where(c => c.TrangThai == "Active")
                    .OrderByDescending(c => c.NgayTao)
                    .Take(count)
                    .Select(c => new
                    {
                        c.CourseID,
                        c.TenKhoaHoc,
                        c.MoTa,
                        c.Gia,
                        c.NgayTao,
                        TeacherName = c.User.HoTen,
                        StudentCount = c.Enrollments.Count,
                        LessonCount = c.Lessons.Count(),
                        Url = Url.Action("Details", "Course", new { id = c.CourseID })
                    })
                    .ToListAsync();

                return Json(newCourses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting new courses");
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _context.Categories
                    .AsNoTracking()
                    .Select(c => new
                    {
                        c.CategoryID,
                        c.Name,
                        c.Description,
                        CourseCount = c.CourseCategories.Count(cc => cc.Course.TrangThai == "Active"),
                        Url = Url.Action("SearchByCategory", "Course", new { categoryId = c.CategoryID })
                    })
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                return Json(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories");
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetHomeStatistics()
        {
            try
            {
                var stats = await GetHomepageStatisticsAsync();
                return Json(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting home statistics");
                return Json(new
                {
                    TotalStudents = 0,
                    TotalCourses = 0,
                    TotalLessons = 0,
                    TotalTeachers = 0,
                    PublishedCourses = 0,
                    NewCoursesThisMonth = 0
                });
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        #region Private Helper Methods

        private async Task<List<Course>> GetFeaturedCoursesAsync()
        {
            // Get a mix of popular and recent courses for homepage
            var popularCourses = await _context.Courses
                .AsNoTracking()
                .Include(c => c.User)
                .Include(c => c.Enrollments)
                .Include(c => c.Lessons)
                .Where(c => c.TrangThai == "Active")
                .OrderByDescending(c => c.Enrollments.Count)
                .Take(6)
                .ToListAsync();

            var recentCourses = await _context.Courses
                .AsNoTracking()
                .Include(c => c.User)
                .Include(c => c.Enrollments)
                .Include(c => c.Lessons)
                .Where(c => c.TrangThai == "Active" && !popularCourses.Select(pc => pc.CourseID).Contains(c.CourseID))
                .OrderByDescending(c => c.NgayTao)
                .Take(6)
                .ToListAsync();

            var featuredCourses = new List<Course>();
            featuredCourses.AddRange(popularCourses);
            featuredCourses.AddRange(recentCourses);

            return featuredCourses.Take(12).ToList();
        }

        private async Task<HomepageStatistics> GetHomepageStatisticsAsync()
        {
            var statistics = new HomepageStatistics();

            // Get all active courses with their enrollments and lessons
            var activeCourses = await _context.Courses
                .AsNoTracking()
                .Include(c => c.Enrollments)
                .Include(c => c.Lessons)
                .Where(c => c.TrangThai == "Active")
                .ToListAsync();

            statistics.TotalCourses = activeCourses.Count;
            statistics.PublishedCourses = activeCourses.Count;
            statistics.TotalStudents = activeCourses.Sum(c => c.Enrollments?.Count ?? 0);
            statistics.TotalLessons = activeCourses.Sum(c => c.Lessons?.Count ?? 0);

            // Count teachers
            statistics.TotalTeachers = await _context.Users
                .AsNoTracking()
                .CountAsync(u => u.LoaiNguoiDung == RoleNames.Teacher);

            // Count new courses this month
            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            statistics.NewCoursesThisMonth = await _context.Courses
                .AsNoTracking()
                .CountAsync(c => c.NgayTao >= startOfMonth && c.TrangThai == "Active");

            return statistics;
        }

        #endregion

        #region Data Transfer Objects

        public class HomepageStatistics
        {
            public int TotalStudents { get; set; }
            public int TotalCourses { get; set; }
            public int TotalLessons { get; set; }
            public int TotalTeachers { get; set; }
            public int PublishedCourses { get; set; }
            public int NewCoursesThisMonth { get; set; }
        }

        #endregion
    }
}