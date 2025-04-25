using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DACSN10.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace DACSN10.Controllers
{
    [Authorize] // Require authentication for most actions
    public class CourseController : Controller
    {
        private readonly AppDbContext _context;

        public CourseController(AppDbContext context)
        {
            _context = context;
        }

        // 1. View all courses (with pagination)
        public async Task<IActionResult> Index(int page = 1, int pageSize = 12)
        {
            var courses = await _context.Courses
                .AsNoTracking()
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCourses = await _context.Courses.CountAsync();
            return View(courses);
        }

        // 2. Popular courses
        public async Task<IActionResult> PopularCourses()
        {
            var courses = await _context.Courses
                .AsNoTracking()
                .Include(c => c.Enrollments)
                .OrderByDescending(c => c.Enrollments.Count)
                .Take(10)
                .ToListAsync();
            return View(courses);
        }

        // 3. New courses
        public async Task<IActionResult> NewCourses()
        {
            var courses = await _context.Courses
                .AsNoTracking()
                .OrderByDescending(c => c.NgayTao)
                .Take(10)
                .ToListAsync();
            return View(courses); // <-- Tự động tìm View có tên NewCourses.cshtml
        }

        // 4. Search by name
        public async Task<IActionResult> SearchByName(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return View("SearchResult", new List<Course>());
            var result = await _context.Courses
                .AsNoTracking()
                .Where(c => c.TenKhoaHoc.ToLower().Contains(keyword.ToLower()))
                .ToListAsync();
            ViewBag.Keyword = keyword;
            return View("SearchResult", result);
        }

        // 5. Search by topic (using MoTa as per model)
        public async Task<IActionResult> SearchByTopic(string topic)
        {
            if (string.IsNullOrWhiteSpace(topic))
                return View("SearchResult", new List<Course>());
            var result = await _context.Courses
                .AsNoTracking()
                .Where(c => c.MoTa.ToLower().Contains(topic.ToLower()))
                .ToListAsync();
            ViewBag.Topic = topic;
            return View("SearchResult", result);
        }

        // 6. Search by category
        public async Task<IActionResult> SearchByCategory(int categoryId)
        {
            var result = await _context.CourseCategories
                .AsNoTracking()
                .Include(cc => cc.Course)
                .Where(cc => cc.CategoryID == categoryId)
                .Select(cc => cc.Course)
                .ToListAsync();
            ViewBag.CategoryId = categoryId;
            return View("SearchResult", result);
        }

        // 7. Course details
        public async Task<IActionResult> Details(int id)
        {
            var course = await _context.Courses
                .AsNoTracking()
                .Include(c => c.User)
                .Include(c => c.Lessons)
                .Include(c => c.CourseCategories).ThenInclude(cc => cc.Category)
                .FirstOrDefaultAsync(c => c.CourseID == id);
            if (course == null) return NotFound("Course not found.");
            return View(course);
        }

        // 8. Preview video (first lesson)
        public async Task<IActionResult> PreviewVideo(int courseId)
        {
            var lesson = await _context.Lessons
                .AsNoTracking()
                .Where(l => l.CourseID == courseId)
                .OrderBy(l => l.LessonID)
                .FirstOrDefaultAsync();
            if (lesson == null) return NotFound("No lessons found for this course.");
            return View(lesson);
        }

        // 9. Enroll in a course
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enroll(int courseId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized("User not authenticated.");

            if (!await _context.Courses.AnyAsync(c => c.CourseID == courseId))
                return NotFound("Course not found.");

            var exists = await _context.Enrollments.AnyAsync(e => e.UserID == userId && e.CourseID == courseId);
            if (!exists)
            {
                _context.Enrollments.Add(new Enrollment
                {
                    CourseID = courseId,
                    UserID = userId,
                    EnrollDate = DateTime.Now,
                    TrangThai = "Đã đăng ký",
                    Progress = 0
                });
                await _context.SaveChangesAsync();
            }
            return Json(new { success = true, message = "Enrolled successfully!" });
        }

        // 10. Follow teacher (assumed to be in UserController, not implemented here)

        // 11. Add to favorites
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToFavorite(int courseId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized("User not authenticated.");

            if (!await _context.Courses.AnyAsync(c => c.CourseID == courseId))
                return NotFound("Course not found.");

            var exists = await _context.FavoriteCourses.AnyAsync(f => f.CourseID == courseId && f.UserID == userId);
            if (!exists)
            {
                _context.FavoriteCourses.Add(new FavoriteCourse
                {
                    CourseID = courseId,
                    UserID = userId
                });
                await _context.SaveChangesAsync();
            }
            return Json(new { success = true, message = "Added to favorites!" });
        }

        // 12. Remove from favorites
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromFavorite(int courseId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized("User not authenticated.");

            var fav = await _context.FavoriteCourses
                .FirstOrDefaultAsync(f => f.CourseID == courseId && f.UserID == userId);
            if (fav != null)
            {
                _context.FavoriteCourses.Remove(fav);
                await _context.SaveChangesAsync();
            }
            return Json(new { success = true, message = "Removed from favorites!" });
        }

        // 13. View enrolled courses
        public async Task<IActionResult> MyCourses()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var courses = await _context.Enrollments
                .AsNoTracking()
                .Include(e => e.Course)
                .Where(e => e.UserID == userId)
                .Select(e => e.Course)
                .ToListAsync();
            return View(courses);
        }

        // 14. View course progress
        public async Task<IActionResult> Progress(int courseId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var enrollment = await _context.Enrollments
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.UserID == userId && e.CourseID == courseId);
            if (enrollment == null)
            {
                ViewBag.ErrorMessage = "Bạn chưa đăng ký khóa học nào";
                return View("EnrollmentNotFound");
            }
            ViewBag.Progress = enrollment.Progress;
            ViewBag.CourseID = courseId;
            return View();
        }

        // 15. View favorite courses
        public async Task<IActionResult> FavoriteCourses()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var favs = await _context.FavoriteCourses
                .AsNoTracking()
                .Include(f => f.Course)
                .Where(f => f.UserID == userId)
                .Select(f => f.Course)
                .ToListAsync();
            return View(favs);
        }
    }
}