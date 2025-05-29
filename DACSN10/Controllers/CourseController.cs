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
    [Authorize]
    public class CourseController : Controller
    {
        private readonly AppDbContext _context;

        public CourseController(AppDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 12)
        {
            var courses = await _context.Courses
                .AsNoTracking()
                .Where(c => c.TrangThai == "Published")
                .OrderBy(c => c.TenKhoaHoc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCourses = await _context.Courses.CountAsync(c => c.TrangThai == "Published");
            return View(courses);
        }

        [AllowAnonymous]
        public async Task<IActionResult> PopularCourses()
        {
            var courses = await _context.Courses
                .AsNoTracking()
                .Include(c => c.Enrollments)
                .Where(c => c.TrangThai == "Published")
                .OrderByDescending(c => c.Enrollments.Count)
                .Take(10)
                .ToListAsync();
            return View(courses);
        }

        [AllowAnonymous]
        public async Task<IActionResult> NewCourses()
        {
            var courses = await _context.Courses
                .AsNoTracking()
                .Where(c => c.TrangThai == "Published")
                .OrderByDescending(c => c.NgayTao)
                .Take(10)
                .ToListAsync();
            return View(courses);
        }

        [AllowAnonymous]
        public async Task<IActionResult> SearchByName(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword) || keyword.Length > 100)
            {
                TempData["Error"] = "Từ khóa không hợp lệ.";
                return View("SearchResult", new List<Course>());
            }
            var result = await _context.Courses
                .AsNoTracking()
                .Where(c => c.TrangThai == "Published" && c.TenKhoaHoc.ToLower().Contains(keyword.ToLower()))
                .ToListAsync();
            ViewBag.Keyword = keyword;
            return View("SearchResult", result);
        }

        [AllowAnonymous]
        public async Task<IActionResult> SearchByTopic(string topic)
        {
            if (string.IsNullOrWhiteSpace(topic) || topic.Length > 100)
            {
                TempData["Error"] = "Chủ đề không hợp lệ.";
                return View("SearchResult", new List<Course>());
            }
            var result = await _context.Courses
                .AsNoTracking()
                .Where(c => c.TrangThai == "Published" && c.MoTa.ToLower().Contains(topic.ToLower()))
                .ToListAsync();
            ViewBag.Topic = topic;
            return View("SearchResult", result);
        }

        [AllowAnonymous]
        public async Task<IActionResult> SearchByCategory(int categoryId)
        {
            var result = await _context.CourseCategories
                .AsNoTracking()
                .Include(cc => cc.Course)
                .Where(cc => cc.CategoryID == categoryId && cc.Course.TrangThai == "Published")
                .Select(cc => cc.Course)
                .ToListAsync();
            if (!result.Any())
            {
                TempData["Error"] = "Không tìm thấy khóa học trong danh mục này.";
            }
            ViewBag.CategoryId = categoryId;
            return View("SearchResult", result);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var course = await _context.Courses
                .AsNoTracking()
                .Include(c => c.User)
                .Include(c => c.Lessons)
                .Include(c => c.CourseCategories).ThenInclude(cc => cc.Category)
                .FirstOrDefaultAsync(c => c.CourseID == id && c.TrangThai == "Published");
            if (course == null)
            {
                TempData["Error"] = "Không tìm thấy khóa học.";
                return NotFound();
            }
            return View(course);
        }

        [AllowAnonymous]
        public async Task<IActionResult> PreviewVideo(int courseId)
        {
            var lesson = await _context.Lessons
                .AsNoTracking()
                .Where(l => l.CourseID == courseId)
                .OrderBy(l => l.LessonID)
                .FirstOrDefaultAsync();
            if (lesson == null)
            {
                TempData["Error"] = "Không có bài học nào để xem trước.";
                return NotFound();
            }
            return View(lesson);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> Enroll(int courseId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Vui lòng đăng nhập để đăng ký khóa học.";
                return Unauthorized();
            }

            var course = await _context.Courses.FirstOrDefaultAsync(c => c.CourseID == courseId && c.TrangThai == "Published");
            if (course == null)
            {
                TempData["Error"] = "Khóa học không tồn tại hoặc không hoạt động.";
                return NotFound();
            }

            var exists = await _context.Enrollments.AnyAsync(e => e.UserID == userId && e.CourseID == courseId);
            if (exists)
            {
                TempData["Error"] = "Bạn đã đăng ký khóa học này.";
                return RedirectToAction("MyCourses");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Enrollments.Add(new Enrollment
                {
                    CourseID = courseId,
                    UserID = userId,
                    EnrollDate = DateTime.Now,
                    TrangThai = "Published",
                    Progress = 0
                });
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Lỗi khi đăng ký khóa học.";
                return RedirectToAction("Details", new { id = courseId });
            }

            TempData["Success"] = "Đăng ký khóa học thành công!";
            return RedirectToAction("MyCourses");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> FollowCourse(int courseId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Vui lòng đăng nhập để theo dõi khóa học.";
                return Unauthorized();
            }

            var course = await _context.Courses.FirstOrDefaultAsync(c => c.CourseID == courseId && c.TrangThai == "Published");
            if (course == null)
            {
                TempData["Error"] = "Khóa học không tồn tại hoặc không hoạt động.";
                return NotFound();
            }

            var exists = await _context.CourseFollows.AnyAsync(f => f.CourseID == courseId && f.UserID == userId);
            if (exists)
            {
                TempData["Error"] = "Bạn đã theo dõi khóa học này.";
                return RedirectToAction("MyFollowedCourses");
            }

            _context.CourseFollows.Add(new CourseFollow
            {
                CourseID = courseId,
                UserID = userId,
                FollowDate = DateTime.Now
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã theo dõi khóa học!";
            return RedirectToAction("MyFollowedCourses");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> UnfollowCourse(int courseId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Vui lòng đăng nhập để bỏ theo dõi khóa học.";
                return Unauthorized();
            }

            var follow = await _context.CourseFollows
                .FirstOrDefaultAsync(f => f.CourseID == courseId && f.UserID == userId);
            if (follow == null)
            {
                TempData["Error"] = "Bạn chưa theo dõi khóa học này.";
                return RedirectToAction("MyFollowedCourses");
            }

            _context.CourseFollows.Remove(follow);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã bỏ theo dõi khóa học!";
            return RedirectToAction("MyFollowedCourses");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> AddToFavorite(int courseId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Vui lòng đăng nhập để thêm vào yêu thích.";
                return Unauthorized();
            }

            var course = await _context.Courses.FirstOrDefaultAsync(c => c.CourseID == courseId && c.TrangThai == "Published");
            if (course == null)
            {
                TempData["Error"] = "Khóa học không tồn tại hoặc không hoạt động.";
                return NotFound();
            }

            var exists = await _context.FavoriteCourses.AnyAsync(f => f.CourseID == courseId && f.UserID == userId);
            if (exists)
            {
                TempData["Error"] = "Khóa học đã có trong danh sách yêu thích.";
                return RedirectToAction("FavoriteCourses");
            }

            _context.FavoriteCourses.Add(new FavoriteCourse
            {
                CourseID = courseId,
                UserID = userId
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã thêm vào danh sách yêu thích!";
            return RedirectToAction("FavoriteCourses");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> RemoveFromFavorite(int courseId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Vui lòng đăng nhập để xóa khỏi yêu thích.";
                return Unauthorized();
            }

            var fav = await _context.FavoriteCourses
                .FirstOrDefaultAsync(f => f.CourseID == courseId && f.UserID == userId);
            if (fav == null)
            {
                TempData["Error"] = "Khóa học không có trong danh sách yêu thích.";
                return RedirectToAction("FavoriteCourses");
            }

            _context.FavoriteCourses.Remove(fav);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã xóa khỏi danh sách yêu thích!";
            return RedirectToAction("FavoriteCourses");
        }

        public async Task<IActionResult> MyCourses()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var courses = await _context.Enrollments
                .AsNoTracking()
                .Include(e => e.Course)
                .Where(e => e.UserID == userId && e.TrangThai == "Published")
                .Select(e => e.Course)
                .ToListAsync();
            return View(courses);
        }

        public async Task<IActionResult> MyFollowedCourses()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var courses = await _context.CourseFollows
                .AsNoTracking()
                .Include(f => f.Course)
                .Where(f => f.UserID == userId && f.Course.TrangThai == "Published")
                .Select(f => f.Course)
                .ToListAsync();
            return View(courses);
        }

        public async Task<IActionResult> Progress(int courseId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var enrollment = await _context.Enrollments
                .AsNoTracking()
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.UserID == userId && e.CourseID == courseId);
            if (enrollment == null)
            {
                TempData["Error"] = "Bạn chưa đăng ký khóa học này.";
                return View("EnrollmentNotFound");
            }
            ViewBag.Progress = enrollment.Progress;
            ViewBag.CourseID = courseId;
            ViewBag.CourseName = enrollment.Course.TenKhoaHoc;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> UpdateProgress(int courseId, int lessonId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Vui lòng đăng nhập để cập nhật tiến độ.";
                return Unauthorized();
            }

            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.UserID == userId && e.CourseID == courseId && e.TrangThai == "Published");
            if (enrollment == null)
            {
                TempData["Error"] = "Bạn chưa đăng ký khóa học này.";
                return RedirectToAction("MyCourses");
            }

            var totalLessons = await _context.Lessons
                .CountAsync(l => l.CourseID == courseId);
            if (totalLessons == 0)
            {
                TempData["Error"] = "Khóa học không có bài học.";
                return RedirectToAction("Progress", new { courseId });
            }

            // Giả sử mỗi bài học hoàn thành tăng tiến độ đều
            var progressPerLesson = 100f / totalLessons;
            enrollment.Progress = Math.Min(100f, enrollment.Progress + progressPerLesson);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật tiến độ thành công!";
            return RedirectToAction("Progress", new { courseId });
        }

        public async Task<IActionResult> FavoriteCourses()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var favs = await _context.FavoriteCourses
                .AsNoTracking()
                .Include(f => f.Course)
                .Where(f => f.UserID == userId && f.Course.TrangThai == "Published")
                .Select(f => f.Course)
                .ToListAsync();
            return View(favs);
        }

        [HttpGet]
        public async Task<IActionResult> GetAverageProgress()
        {
            if (!User.Identity.IsAuthenticated)
                return Json(0);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var averageProgress = await _context.Enrollments
                .Where(e => e.UserID == userId && e.TrangThai == "Published")
                .AverageAsync(e => (float?)e.Progress) ?? 0;

            return Json(averageProgress);
        }

        // Hiển thị danh sách bài học trong khoá học
        [AllowAnonymous]
        public async Task<IActionResult> CourseLessons(int courseId)
        {
            var course = await _context.Courses
                .Include(c => c.Lessons)
                .FirstOrDefaultAsync(c => c.CourseID == courseId && c.TrangThai == "Published");
            if (course == null) return NotFound();

            return View(course);
        }

        // Hiển thị chi tiết 1 bài học cho học viên
        public async Task<IActionResult> Learn(int lessonId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.LessonID == lessonId);

            if (lesson == null) return NotFound();

            // lấy tiến trình cũ nếu có
            var progress = await _context.LessonProgresses.FirstOrDefaultAsync(p => p.LessonID == lessonId && p.UserID == userId);

            ViewBag.Progress = progress;
            return View(lesson);
        }

        // API: Ghi nhận tiến trình học bài (AJAX gọi)
        [HttpPost]
        public async Task<IActionResult> RecordProgress(int lessonId, double watchedSeconds, bool complete = false)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var lesson = await _context.Lessons.FindAsync(lessonId);
            if (lesson == null) return NotFound();

            var progress = await _context.LessonProgresses.FirstOrDefaultAsync(p => p.LessonID == lessonId && p.UserID == userId);

            if (progress == null)
            {
                progress = new LessonProgress
                {
                    LessonID = lessonId,
                    UserID = userId,
                    WatchedSeconds = watchedSeconds,
                    IsCompleted = complete,
                    CompletedAt = complete ? DateTime.Now : null
                };
                _context.LessonProgresses.Add(progress);
            }
            else
            {
                progress.WatchedSeconds = watchedSeconds;
                if (complete && !progress.IsCompleted)
                {
                    progress.IsCompleted = true;
                    progress.CompletedAt = DateTime.Now;
                }
            }
            await _context.SaveChangesAsync();

            // Gọi cập nhật tổng tiến trình khoá học nếu muốn (tùy)
            return Json(new { success = true });
        }
    }
}