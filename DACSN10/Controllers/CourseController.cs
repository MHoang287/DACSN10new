using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DACSN10.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using System;
using DACSN10.Services;

namespace DACSN10.Controllers
{
    public class CourseController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        public CourseController(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        #region Public Course Views

        [AllowAnonymous]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 12)
        {
            var courses = await _context.Courses
                .AsNoTracking()
                .Include(c => c.User)
                .Include(c => c.Enrollments)
                .Where(c => c.TrangThai == "Active")
                .OrderBy(c => c.TenKhoaHoc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCourses = await _context.Courses.CountAsync(c => c.TrangThai == "Active");
            ViewBag.TotalPages = (int)Math.Ceiling((double)ViewBag.TotalCourses / pageSize);
            return View(courses);
        }

        [AllowAnonymous]
        public async Task<IActionResult> PopularCourses(int page = 1, int pageSize = 12)
        {
            var courses = await _context.Courses
                .AsNoTracking()
                .Include(c => c.User)
                .Include(c => c.Enrollments)
                .Where(c => c.TrangThai == "Active")
                .OrderByDescending(c => c.Enrollments.Count)
                .ThenByDescending(c => c.NgayTao)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCourses = await _context.Courses.CountAsync(c => c.TrangThai == "Active");
            ViewBag.TotalPages = (int)Math.Ceiling((double)ViewBag.TotalCourses / pageSize);
            return View(courses);
        }

        [AllowAnonymous]
        public async Task<IActionResult> NewCourses(int page = 1, int pageSize = 12)
        {
            var courses = await _context.Courses
                .AsNoTracking()
                .Include(c => c.User)
                .Include(c => c.Enrollments)
                .Include(c => c.Lessons)
                .Include(c => c.Quizzes)
                .Where(c => c.TrangThai == "Active")
                .OrderByDescending(c => c.NgayTao)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCourses = await _context.Courses.CountAsync(c => c.TrangThai == "Active");
            ViewBag.TotalPages = (int)Math.Ceiling((double)ViewBag.TotalCourses / pageSize);
            return View(courses);
        }

        #endregion

        #region Search Functions

        [AllowAnonymous]
        public async Task<IActionResult> SearchByName(string keyword, int page = 1, int pageSize = 12)
        {
            if (string.IsNullOrWhiteSpace(keyword) || keyword.Length > 100)
            {
                TempData["Error"] = "Từ khóa không hợp lệ.";
                return View("SearchResult", new List<Course>());
            }

            var result = await _context.Courses
                .AsNoTracking()
                .Include(c => c.User)
                .Include(c => c.Enrollments)
                .Where(c => c.TrangThai == "Active" && c.TenKhoaHoc.ToLower().Contains(keyword.ToLower()))
                .OrderByDescending(c => c.Enrollments.Count)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalResults = await _context.Courses
                .CountAsync(c => c.TrangThai == "Active" && c.TenKhoaHoc.ToLower().Contains(keyword.ToLower()));

            ViewBag.Keyword = keyword;
            ViewBag.SearchType = "name";
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalResults = totalResults;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalResults / pageSize);
            return View("SearchResult", result);
        }

        [AllowAnonymous]
        public async Task<IActionResult> SearchByTopic(string topic, int page = 1, int pageSize = 12)
        {
            if (string.IsNullOrWhiteSpace(topic) || topic.Length > 100)
            {
                TempData["Error"] = "Chủ đề không hợp lệ.";
                return View("SearchResult", new List<Course>());
            }

            var result = await _context.Courses
                .AsNoTracking()
                .Include(c => c.User)
                .Include(c => c.Enrollments)
                .Where(c => c.TrangThai == "Active" && c.MoTa.ToLower().Contains(topic.ToLower()))
                .OrderByDescending(c => c.Enrollments.Count)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalResults = await _context.Courses
                .CountAsync(c => c.TrangThai == "Active" && c.MoTa.ToLower().Contains(topic.ToLower()));

            ViewBag.Topic = topic;
            ViewBag.SearchType = "topic";
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalResults = totalResults;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalResults / pageSize);
            return View("SearchResult", result);
        }

        [AllowAnonymous]
        public async Task<IActionResult> SearchByCategory(int categoryId, int page = 1, int pageSize = 12)
        {
            var category = await _context.Categories.FindAsync(categoryId);
            if (category == null)
            {
                TempData["Error"] = "Danh mục không tồn tại.";
                return View("SearchResult", new List<Course>());
            }

            var result = await _context.CourseCategories
                .AsNoTracking()
                .Include(cc => cc.Course).ThenInclude(c => c.User)
                .Include(cc => cc.Course).ThenInclude(c => c.Enrollments)
                .Where(cc => cc.CategoryID == categoryId && cc.Course.TrangThai == "Active")
                .Select(cc => cc.Course)
                .OrderByDescending(c => c.Enrollments.Count)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalResults = await _context.CourseCategories
                .CountAsync(cc => cc.CategoryID == categoryId && cc.Course.TrangThai == "Active");

            ViewBag.CategoryId = categoryId;
            ViewBag.CategoryName = category.Name;
            ViewBag.SearchType = "category";
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalResults = totalResults;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalResults / pageSize);
            return View("SearchResult", result);
        }

        #endregion

        #region Course Details & Preview

        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var course = await _context.Courses
                .AsNoTracking()
                .Include(c => c.User)
                .Include(c => c.Lessons)
                .Include(c => c.Enrollments)
                .Include(c => c.CourseCategories).ThenInclude(cc => cc.Category)
                .Include(c => c.Quizzes).ThenInclude(q => q.Questions)
                .Include(c => c.Assignments)
                .FirstOrDefaultAsync(c => c.CourseID == id && c.TrangThai == "Active");

            if (course == null)
            {
                TempData["Error"] = "Không tìm thấy khóa học.";
                return NotFound();
            }

            // Initialize default ViewBag values
            ViewBag.IsEnrolled = false;
            ViewBag.IsFollowed = false;
            ViewBag.IsFavorite = false;
            ViewBag.HasPaid = false;
            ViewBag.PendingPayment = false;
            ViewBag.Progress = 0f;

            // Check if user is logged in and get user status
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!string.IsNullOrEmpty(userId))
                {
                    // Check enrollment status
                    var enrollment = await _context.Enrollments
                        .FirstOrDefaultAsync(e => e.UserID == userId && e.CourseID == id && e.TrangThai == "Active");

                    ViewBag.IsEnrolled = enrollment != null;
                    ViewBag.Progress = enrollment?.Progress ?? 0f;

                    // Check payment status
                    var paymentInfo = await GetUserPaymentStatus(userId, id);
                    ViewBag.HasPaid = paymentInfo.HasSuccessfulPayment;
                    ViewBag.PendingPayment = paymentInfo.HasPendingPayment;

                    // Check follow status
                    ViewBag.IsFollowed = await _context.CourseFollows
                        .AnyAsync(f => f.UserID == userId && f.CourseID == id);

                    // Check favorite status
                    ViewBag.IsFavorite = await _context.FavoriteCourses
                        .AnyAsync(f => f.UserID == userId && f.CourseID == id);
                }
            }

            return View(course);
        }

        [AllowAnonymous]
        public async Task<IActionResult> PreviewVideo(int courseId)
        {
            var lesson = await _context.Lessons
                .AsNoTracking()
                .Include(l => l.Course)
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

        #endregion

        #region Payment Status Helper

        /// <summary>
        /// Get user payment status for a specific course
        /// </summary>
        private async Task<(bool HasSuccessfulPayment, bool HasPendingPayment)> GetUserPaymentStatus(string userId, int courseId)
        {
            var payments = await _context.Payments
                .Where(p => p.UserID == userId && p.CourseID == courseId)
                .Select(p => p.Status)
                .ToListAsync();

            var hasSuccessfulPayment = payments.Any(s => s == PaymentStatus.Success);
            var hasPendingPayment = payments.Any(s => s == PaymentStatus.Pending);

            return (hasSuccessfulPayment, hasPendingPayment);
        }

        /// <summary>
        /// Check if user can access course content (enrolled or has successful payment)
        /// </summary>
        [Authorize]
        public async Task<bool> CanAccessCourseContent(string userId, int courseId)
        {
            // Check if user is enrolled
            var isEnrolled = await _context.Enrollments
                .AnyAsync(e => e.UserID == userId && e.CourseID == courseId && e.TrangThai == "Active");

            if (isEnrolled) return true;

            // Check if user has successful payment (for courses that require admin approval)
            var hasSuccessfulPayment = await _context.Payments
                .AnyAsync(p => p.UserID == userId && p.CourseID == courseId && p.Status == PaymentStatus.Success);

            return hasSuccessfulPayment;
        }

        #endregion

        #region Enrollment

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Enroll(int courseId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Vui lòng đăng nhập để đăng ký khóa học.";
                return RedirectToAction("Login", "Account");
            }

            var course = await _context.Courses
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CourseID == courseId && c.TrangThai == "Active");
            if (course == null)
            {
                TempData["Error"] = "Khóa học không tồn tại hoặc không hoạt động.";
                return NotFound();
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin người dùng.";
                return RedirectToAction("Login", "Account");
            }

            var exists = await _context.Enrollments.AnyAsync(e => e.UserID == userId && e.CourseID == courseId);
            if (exists)
            {
                TempData["Error"] = "Bạn đã đăng ký khóa học này.";
                return RedirectToAction("MyCourses");
            }

            // Check if course is free or requires payment
            if (course.Gia > 0)
            {
                // Check if user has already paid successfully
                var hasSuccessfulPayment = await _context.Payments
                    .AnyAsync(p => p.UserID == userId && p.CourseID == courseId && p.Status == PaymentStatus.Success);

                if (!hasSuccessfulPayment)
                {
                    TempData["Info"] = "Khóa học này yêu cầu thanh toán. Vui lòng chọn phương thức thanh toán.";
                    return RedirectToAction("Create", "Payment", new { courseId });
                }
            }

            // Free course enrollment or enrollment after successful payment
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
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

                // Send enrollment confirmation email
                enrollment.Course = course;
                await _emailService.SendEnrollmentConfirmationAsync(enrollment, user);

                await transaction.CommitAsync();

                TempData["Success"] = "Đăng ký khóa học thành công! Email xác nhận đã được gửi.";
                return RedirectToAction("MyCourses");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Enrollment error: {ex.Message}");
                TempData["Error"] = "Lỗi khi đăng ký khóa học.";
                return RedirectToAction("Details", new { id = courseId });
            }
        }

        #endregion

        #region Course Following

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> FollowCourse(int courseId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để theo dõi khóa học." });
            }

            var course = await _context.Courses.FirstOrDefaultAsync(c => c.CourseID == courseId && c.TrangThai == "Active");
            if (course == null)
            {
                return Json(new { success = false, message = "Khóa học không tồn tại hoặc không hoạt động." });
            }

            var exists = await _context.CourseFollows.AnyAsync(f => f.CourseID == courseId && f.UserID == userId);
            if (exists)
            {
                return Json(new { success = false, message = "Bạn đã theo dõi khóa học này." });
            }

            try
            {
                _context.CourseFollows.Add(new CourseFollow
                {
                    CourseID = courseId,
                    UserID = userId,
                    FollowDate = DateTime.Now
                });
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã theo dõi khóa học!" });
            }
            catch
            {
                return Json(new { success = false, message = "Lỗi khi theo dõi khóa học." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> UnfollowCourse(int courseId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để bỏ theo dõi khóa học." });
            }

            var follow = await _context.CourseFollows
                .FirstOrDefaultAsync(f => f.CourseID == courseId && f.UserID == userId);
            if (follow == null)
            {
                return Json(new { success = false, message = "Bạn chưa theo dõi khóa học này." });
            }

            try
            {
                _context.CourseFollows.Remove(follow);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã bỏ theo dõi khóa học!" });
            }
            catch
            {
                return Json(new { success = false, message = "Lỗi khi bỏ theo dõi khóa học." });
            }
        }

        #endregion

        #region Favorite Courses

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> AddToFavorite(int courseId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để thêm vào yêu thích." });
            }

            var course = await _context.Courses.FirstOrDefaultAsync(c => c.CourseID == courseId && c.TrangThai == "Active");
            if (course == null)
            {
                return Json(new { success = false, message = "Khóa học không tồn tại hoặc không hoạt động." });
            }

            var exists = await _context.FavoriteCourses.AnyAsync(f => f.CourseID == courseId && f.UserID == userId);
            if (exists)
            {
                return Json(new { success = false, message = "Khóa học đã có trong danh sách yêu thích." });
            }

            try
            {
                _context.FavoriteCourses.Add(new FavoriteCourse
                {
                    CourseID = courseId,
                    UserID = userId
                });
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã thêm vào danh sách yêu thích!" });
            }
            catch
            {
                return Json(new { success = false, message = "Lỗi khi thêm vào yêu thích." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> RemoveFromFavorite(int courseId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để xóa khỏi yêu thích." });
            }

            var fav = await _context.FavoriteCourses
                .FirstOrDefaultAsync(f => f.CourseID == courseId && f.UserID == userId);
            if (fav == null)
            {
                return Json(new { success = false, message = "Khóa học không có trong danh sách yêu thích." });
            }

            try
            {
                _context.FavoriteCourses.Remove(fav);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã xóa khỏi danh sách yêu thích!" });
            }
            catch
            {
                return Json(new { success = false, message = "Lỗi khi xóa khỏi yêu thích." });
            }
        }

        #endregion

        #region My Courses & Lists

        [Authorize]
        public async Task<IActionResult> MyCourses(int page = 1, int pageSize = 12)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Lấy dữ liệu enrollments
            var enrollments = await _context.Enrollments
                .AsNoTracking()
                .Include(e => e.Course).ThenInclude(c => c.User)
                .Include(e => e.Course).ThenInclude(c => c.Lessons)
                .Where(e => e.UserID == userId && e.TrangThai == "Active")
                .OrderByDescending(e => e.EnrollDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Chuyển đổi sang List<dynamic> - FIXED for float type
            var model = enrollments.Select(e => new
            {
                Course = e.Course,
                Progress = e.Progress, // Direct assignment since Progress is float, not float?
                EnrollDate = e.EnrollDate
            }).Cast<dynamic>().ToList();

            var totalCourses = await _context.Enrollments
                .CountAsync(e => e.UserID == userId && e.TrangThai == "Active");

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCourses = totalCourses;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCourses / pageSize);

            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> MyFollowedCourses(int page = 1, int pageSize = 12)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var courses = await _context.CourseFollows
                .AsNoTracking()
                .Include(f => f.Course).ThenInclude(c => c.User)
                .Include(f => f.Course).ThenInclude(c => c.Enrollments)
                .Where(f => f.UserID == userId && f.Course.TrangThai == "Active")
                .OrderByDescending(f => f.FollowDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => f.Course)
                .ToListAsync();

            var totalCourses = await _context.CourseFollows
                .CountAsync(f => f.UserID == userId && f.Course.TrangThai == "Active");

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCourses = totalCourses;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCourses / pageSize);

            return View(courses);
        }

        [Authorize]
        public async Task<IActionResult> FavoriteCourses(int page = 1, int pageSize = 12)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var favs = await _context.FavoriteCourses
                .AsNoTracking()
                .Include(f => f.Course).ThenInclude(c => c.User)
                .Include(f => f.Course).ThenInclude(c => c.Enrollments)
                .Where(f => f.UserID == userId && f.Course.TrangThai == "Active")
                .OrderBy(f => f.Course.TenKhoaHoc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => f.Course)
                .ToListAsync();

            var totalFavorites = await _context.FavoriteCourses
                .CountAsync(f => f.UserID == userId && f.Course.TrangThai == "Active");

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalFavorites = totalFavorites;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalFavorites / pageSize);

            return View(favs);
        }

        #endregion

        #region Progress Management

        [Authorize]
        public async Task<IActionResult> Progress(int courseId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var enrollment = await _context.Enrollments
                .AsNoTracking()
                .Include(e => e.Course).ThenInclude(c => c.Lessons)
                .Include(e => e.Course).ThenInclude(c => c.User)
                .FirstOrDefaultAsync(e => e.UserID == userId && e.CourseID == courseId && e.TrangThai == "Active");

            if (enrollment == null)
            {
                TempData["Error"] = "Bạn chưa đăng ký khóa học này.";
                return RedirectToAction("MyCourses");
            }

            ViewBag.Progress = enrollment.Progress;
            ViewBag.CourseID = courseId;
            ViewBag.CourseName = enrollment.Course.TenKhoaHoc;
            ViewBag.TotalLessons = enrollment.Course.Lessons?.Count ?? 0;
            return View(enrollment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> UpdateProgress(int courseId, int lessonId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để cập nhật tiến độ." });
            }

            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.UserID == userId && e.CourseID == courseId && e.TrangThai == "Active");
            if (enrollment == null)
            {
                return Json(new { success = false, message = "Bạn chưa đăng ký khóa học này." });
            }

            var totalLessons = await _context.Lessons
                .CountAsync(l => l.CourseID == courseId);
            if (totalLessons == 0)
            {
                return Json(new { success = false, message = "Khóa học không có bài học." });
            }

            try
            {
                // Calculate progress based on completed lessons - FIXED for float type
                var progressPerLesson = 100f / totalLessons;
                enrollment.Progress = Math.Min(100f, enrollment.Progress + progressPerLesson);

                await _context.SaveChangesAsync();

                return Json(new { success = true, progress = enrollment.Progress, message = "Cập nhật tiến độ thành công!" });
            }
            catch
            {
                return Json(new { success = false, message = "Lỗi khi cập nhật tiến độ." });
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAverageProgress()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // FIXED for float type - Use direct Average method
            var averageProgress = await _context.Enrollments
                .Where(e => e.UserID == userId && e.TrangThai == "Active")
                .AverageAsync(e => (double)e.Progress); // Cast to double for Average

            return Json(Math.Round(averageProgress, 2));
        }

        #endregion

        #region Lesson Details

        [Authorize]
        public async Task<IActionResult> LessonDetails(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Vui lòng đăng nhập để xem bài học.";
                return RedirectToAction("Login", "Account");
            }

            // Lấy đúng 1 bài học với LessonID
            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(l => l.LessonID == id);

            if (lesson == null)
            {
                TempData["Error"] = "Không tìm thấy bài học.";
                return NotFound();
            }

            // Kiểm tra quyền truy cập khóa học chứa bài học này
            var canAccess = await CanAccessCourseContent(userId, lesson.CourseID);
            if (!canAccess)
            {
                TempData["Error"] = "Bạn cần đăng ký khóa học để xem bài học này.";
                return RedirectToAction("Details", new { id = lesson.CourseID });
            }

            // Lấy Enrollment nếu muốn hiển thị tiến độ (nếu cần)
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.UserID == userId && e.CourseID == lesson.CourseID && e.TrangThai == "Active");
            ViewBag.Enrollment = enrollment;

            // Lấy danh sách tất cả bài học trong khóa học để chuyển bài (nếu muốn)
            var allLessons = await _context.Lessons
                .Where(l => l.CourseID == lesson.CourseID)
                .OrderBy(l => l.LessonID)
                .ToListAsync();
            ViewBag.AllLessons = allLessons;
            ViewBag.CurrentLessonIndex = allLessons.FindIndex(l => l.LessonID == id);

            // Truyền lesson đúng kiểu sang View
            return View(lesson);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> MarkLessonCompleted(int lessonId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập." });
            }

            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.LessonID == lessonId);

            if (lesson == null)
            {
                return Json(new { success = false, message = "Không tìm thấy bài học." });
            }

            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.UserID == userId && e.CourseID == lesson.CourseID && e.TrangThai == "Active");

            if (enrollment == null)
            {
                return Json(new { success = false, message = "Bạn chưa đăng ký khóa học này." });
            }

            try
            {
                // Check if lesson is already marked as completed
                var existingProgress = await _context.UserLessonProgress
                    .FirstOrDefaultAsync(p => p.UserID == userId && p.LessonID == lessonId);

                if (existingProgress == null)
                {
                    // Add new progress record
                    var progress = new UserLessonProgress
                    {
                        UserID = userId,
                        LessonID = lessonId,
                        IsCompleted = true,
                        CompletedAt = DateTime.Now
                    };
                    _context.UserLessonProgress.Add(progress);
                }
                else if (!existingProgress.IsCompleted)
                {
                    // Update existing record
                    existingProgress.IsCompleted = true;
                    existingProgress.CompletedAt = DateTime.Now;
                }

                // Calculate overall course progress
                var totalLessons = await _context.Lessons.CountAsync(l => l.CourseID == lesson.CourseID);
                var completedLessonsCount = await _context.UserLessonProgress
                    .CountAsync(p => p.UserID == userId &&
                                   p.Lesson.CourseID == lesson.CourseID &&
                                   p.IsCompleted);

                if (totalLessons > 0)
                {
                    enrollment.Progress = (float)completedLessonsCount / totalLessons * 100;
                    await _context.SaveChangesAsync();

                    return Json(new
                    {
                        success = true,
                        message = "Đã đánh dấu hoàn thành bài học!",
                        progress = enrollment.Progress
                    });
                }

                return Json(new { success = false, message = "Không thể cập nhật tiến độ." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi cập nhật tiến độ: " + ex.Message });
            }
        }

        // Helper method to get completed lessons for a user in a course
        private async Task<List<int>> GetCompletedLessons(string userId, int courseId)
        {
            return await _context.UserLessonProgress
                .Where(p => p.UserID == userId &&
                           p.Lesson.CourseID == courseId &&
                           p.IsCompleted)
                .Select(p => p.LessonID)
                .ToListAsync();
        }

        // Method to handle "Học ngay" button click
        [Authorize]
        public async Task<IActionResult> StartLesson(int courseId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Vui lòng đăng nhập để học.";
                return RedirectToAction("Login", "Account");
            }

            // Check if user is enrolled in the course
            var canAccess = await CanAccessCourseContent(userId, courseId);
            if (!canAccess)
            {
                TempData["Error"] = "Bạn cần đăng ký khóa học để bắt đầu học.";
                return RedirectToAction("Details", new { id = courseId });
            }

            // Get the first lesson or the next uncompleted lesson
            var completedLessons = await GetCompletedLessons(userId, courseId);

            var nextLesson = await _context.Lessons
                .Where(l => l.CourseID == courseId && !completedLessons.Contains(l.LessonID))
                .OrderBy(l => l.LessonID)
                .FirstOrDefaultAsync();

            // If all lessons are completed, go to the first lesson
            if (nextLesson == null)
            {
                nextLesson = await _context.Lessons
                    .Where(l => l.CourseID == courseId)
                    .OrderBy(l => l.LessonID)
                    .FirstOrDefaultAsync();
            }

            if (nextLesson == null)
            {
                TempData["Error"] = "Khóa học này chưa có bài học nào.";
                return RedirectToAction("Details", new { id = courseId });
            }

            // Redirect to the lesson details page
            return RedirectToAction("LessonDetails", new { id = nextLesson.LessonID });
        }

        #endregion
    }
}