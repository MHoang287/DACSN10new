using Microsoft.AspNetCore.Mvc;
using DACSN10.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace DACSN10.Controllers
{
    public class CourseController : Controller
    {
        private readonly AppDbContext _context;

        public CourseController(AppDbContext context)
        {
            _context = context;
        }

        // 1. Khóa h?c ph? bi?n
        public IActionResult PopularCourses()
        {
            var courses = _context.Courses
                .Include(c => c.Enrollments)
                .OrderByDescending(c => c.Enrollments.Count)
                .Take(10)
                .ToList();

            return View(courses);
        }

        // 2. Khóa h?c m?i
        public IActionResult NewCourses()
        {
            var courses = _context.Courses
                .OrderByDescending(c => c.NgayTao)
                .Take(10)
                .ToList();

            return View(courses);
        }

        // 3. Tìm theo tên
        public IActionResult SearchByName(string keyword)
        {
            var result = _context.Courses
                .Where(c => c.TenKhoaHoc.Contains(keyword))
                .ToList();

            return View("SearchResult", result);
        }

        // 4. Tìm theo ch? ?? (gi? s? ChuDe là 1 tr??ng riêng)
        public IActionResult SearchByTopic(string topic)
        {
            var result = _context.Courses
                .Where(c => c.MoTa.Contains(topic))
                .ToList();

            return View("SearchResult", result);
        }

        // 5. Tìm theo danh m?c
        public IActionResult SearchByCategory(int categoryId)
        {
            var result = _context.CourseCategories
                .Include(cc => cc.Course)
                .Where(cc => cc.CategoryID == categoryId)
                .Select(cc => cc.Course)
                .ToList();

            return View("SearchResult", result);
        }

        // 6. Chi ti?t khóa h?c
        public IActionResult Details(int id)
        {
            var course = _context.Courses
                .Include(c => c.User)
                .Include(c => c.Lessons)
                .Include(c => c.CourseCategories).ThenInclude(cc => cc.Category)
                .FirstOrDefault(c => c.CourseID == id);

            if (course == null) return NotFound();
            return View(course);
        }

        // 7. Xem tr??c video bài gi?ng (l?y bài ??u tiên)
        public IActionResult PreviewVideo(int courseId)
        {
            var lesson = _context.Lessons
                .Where(l => l.CourseID == courseId)
                .OrderBy(l => l.LessonID)
                .FirstOrDefault();

            return View(lesson);
        }

        // 8. ??ng ký khóa h?c
        [HttpPost]
        public IActionResult Enroll(int courseId)
        {
            var userId = User.Identity.Name;

            var exists = _context.Enrollments.Any(e => e.UserID == userId && e.CourseID == courseId);
            if (!exists)
            {
                _context.Enrollments.Add(new Enrollment
                {
                    CourseID = courseId,
                    UserID = userId,
                    EnrollDate = DateTime.Now,
                    TrangThai = "?ã ??ng ký",
                    Progress = 0
                });
                _context.SaveChanges();
            }

            return RedirectToAction("MyCourses");
        }

        // 9. Theo dõi gi?ng viên (?ã có trong UserController)

        // 10. Thêm vào yêu thích
        [HttpPost]
        public IActionResult AddToFavorite(int courseId)
        {
            var userId = User.Identity.Name;

            var exists = _context.FavoriteCourses.Any(f => f.CourseID == courseId && f.UserID == userId);
            if (!exists)
            {
                _context.FavoriteCourses.Add(new FavoriteCourse
                {
                    CourseID = courseId,
                    UserID = userId
                });
                _context.SaveChanges();
            }

            return RedirectToAction("Details", new { id = courseId });
        }

        // 11. B? kh?i yêu thích
        [HttpPost]
        public IActionResult RemoveFromFavorite(int courseId)
        {
            var userId = User.Identity.Name;

            var fav = _context.FavoriteCourses
                .FirstOrDefault(f => f.CourseID == courseId && f.UserID == userId);
            if (fav != null)
            {
                _context.FavoriteCourses.Remove(fav);
                _context.SaveChanges();
            }

            return RedirectToAction("FavoriteCourses");
        }

        // 12. Danh sách khóa h?c ?ã ??ng ký
        public IActionResult MyCourses()
        {
            var userId = User.Identity.Name;

            var courses = _context.Enrollments
                .Include(e => e.Course)
                .Where(e => e.UserID == userId)
                .Select(e => e.Course)
                .ToList();

            return View(courses);
        }

        // 13. Ti?n ?? hoàn thành
        public IActionResult Progress(int courseId)
        {
            var userId = User.Identity.Name;

            var enrollment = _context.Enrollments
                .FirstOrDefault(e => e.UserID == userId && e.CourseID == courseId);

            if (enrollment == null) return NotFound();

            ViewBag.Progress = enrollment.Progress;
            ViewBag.CourseID = courseId;

            return View();
        }

        // 14. Danh sách khóa h?c yêu thích
        public IActionResult FavoriteCourses()
        {
            var userId = User.Identity.Name;

            var favs = _context.FavoriteCourses
                .Include(f => f.Course)
                .Where(f => f.UserID == userId)
                .Select(f => f.Course)
                .ToList();

            return View(favs);
        }

        public IActionResult Index()
        {
            var allCourses = _context.Courses.ToList();
            return View(allCourses);
        }
    }
}
