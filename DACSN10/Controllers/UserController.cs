using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DACSN10.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DACSN10.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        #region Profile Management

        public async Task<IActionResult> EditProfile()
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin người dùng.";
                return NotFound();
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(User model)
        {
            if (!ModelState.IsValid)
            {
                // Remove validation errors for fields we don't want to validate
                ModelState.Remove("Id");
                ModelState.Remove("UserName");
                ModelState.Remove("NormalizedUserName");
                ModelState.Remove("NormalizedEmail");
                ModelState.Remove("PasswordHash");
                ModelState.Remove("SecurityStamp");
                ModelState.Remove("ConcurrencyStamp");

                ModelState.Remove("Courses");
                ModelState.Remove("Submissions");
                ModelState.Remove("Payments");
                ModelState.Remove("Enrollments");
                ModelState.Remove("FavoriteCourses");
                ModelState.Remove("Followers");
                ModelState.Remove("Following");
                ModelState.Remove("QuizResults");

                if (!ModelState.IsValid)
                {
                    return View(model);
                }
            }

            var userId = GetCurrentUserId();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin người dùng.";
                return NotFound();
            }

            try
            {
                // Update allowed fields only
                user.HoTen = model.HoTen?.Trim();
                user.Email = model.Email?.Trim();
                user.PhoneNumber = model.PhoneNumber?.Trim();

                // Update normalized email if email changed
                if (!string.IsNullOrEmpty(user.Email))
                {
                    user.NormalizedEmail = user.Email.ToUpper();
                    user.UserName = user.Email;
                    user.NormalizedUserName = user.Email.ToUpper();
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật thông tin thành công!";
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating profile: {ex.Message}");
                TempData["Error"] = "Lỗi khi cập nhật thông tin. Vui lòng thử lại.";
                return View(model);
            }
        }

        #endregion

        #region Teacher Profile & Following

        [AllowAnonymous]
        public async Task<IActionResult> TeacherProfile(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["Error"] = "ID giảng viên không hợp lệ.";
                return BadRequest();
            }

            var teacher = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == id && u.LoaiNguoiDung == RoleNames.Teacher)
                .FirstOrDefaultAsync();

            if (teacher == null)
            {
                TempData["Error"] = "Không tìm thấy giảng viên.";
                return NotFound();
            }

            var courses = await _context.Courses
                .AsNoTracking()
                .Include(c => c.Enrollments)
                .Where(c => c.UserID == id && c.TrangThai == "Active")
                .OrderByDescending(c => c.NgayTao)
                .ToListAsync();

            // Calculate teacher statistics
            var totalStudents = courses.SelectMany(c => c.Enrollments).Count();
            var totalCourses = courses.Count;
            var averageRating = 0.0; // This could be calculated from course ratings if you have that feature

            var teacherProfile = new TeacherSearchViewModel
            {
                Teacher = teacher,
                Courses = courses,
                IsFollowed = false
            };

            // Check if current user is following this teacher
            if (User.Identity.IsAuthenticated)
            {
                var currentUserId = GetCurrentUserId();
                teacherProfile.IsFollowed = await _context.Follows
                    .AnyAsync(f => f.FollowerID == currentUserId && f.FollowedTeacherID == id);
            }

            ViewBag.TotalStudents = totalStudents;
            ViewBag.TotalCourses = totalCourses;
            ViewBag.AverageRating = averageRating;

            return View(teacherProfile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.User)]
        public async Task<IActionResult> FollowTeacher(string teacherId)
        {
            if (string.IsNullOrWhiteSpace(teacherId))
            {
                TempData["Error"] = "ID giảng viên không hợp lệ.";
                return RedirectToAction("TeacherProfile", new { id = teacherId });
            }

            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
            {
                TempData["Error"] = "Vui lòng đăng nhập để theo dõi giảng viên.";
                return RedirectToAction("TeacherProfile", new { id = teacherId });
            }

            // Can't follow yourself
            if (currentUserId == teacherId)
            {
                TempData["Error"] = "Không thể theo dõi chính mình.";
                return RedirectToAction("TeacherProfile", new { id = teacherId });
            }

            var teacher = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == teacherId && u.LoaiNguoiDung == RoleNames.Teacher);

            if (teacher == null)
            {
                TempData["Error"] = "Không tìm thấy giảng viên.";
                return RedirectToAction("TeacherProfile", new { id = teacherId });
            }

            var exists = await _context.Follows
                .AnyAsync(f => f.FollowerID == currentUserId && f.FollowedTeacherID == teacherId);

            if (exists)
            {
                TempData["Error"] = "Bạn đã theo dõi giảng viên này.";
                return RedirectToAction("TeacherProfile", new { id = teacherId });
            }

            try
            {
                _context.Follows.Add(new Follow
                {
                    FollowerID = currentUserId,
                    FollowedTeacherID = teacherId
                });

                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã theo dõi giảng viên!";
                return RedirectToAction("TeacherProfile", new { id = teacherId });
            }
            catch
            {
                TempData["Error"] = "Lỗi khi theo dõi giảng viên.";
                return RedirectToAction("TeacherProfile", new { id = teacherId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.User)]
        public async Task<IActionResult> UnfollowTeacher(string teacherId)
        {
            if (string.IsNullOrWhiteSpace(teacherId))
            {
                TempData["Error"] = "ID giảng viên không hợp lệ.";
                return RedirectToAction("TeacherProfile", new { id = teacherId });
            }

            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
            {
                TempData["Error"] = "Vui lòng đăng nhập để bỏ theo dõi giảng viên.";
                return RedirectToAction("TeacherProfile", new { id = teacherId });
            }

            var follow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerID == currentUserId && f.FollowedTeacherID == teacherId);

            if (follow == null)
            {
                TempData["Error"] = "Bạn chưa theo dõi giảng viên này.";
                return RedirectToAction("TeacherProfile", new { id = teacherId });
            }

            try
            {
                _context.Follows.Remove(follow);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã bỏ theo dõi giảng viên!";
                return RedirectToAction("TeacherProfile", new { id = teacherId });
            }
            catch
            {
                TempData["Error"] = "Lỗi khi bỏ theo dõi giảng viên.";
                return RedirectToAction("TeacherProfile", new { id = teacherId });
            }
        }

        #endregion

        #region Teacher Search with Course Suggestions

        [AllowAnonymous]
        public async Task<IActionResult> SearchTeacher(string keyword, int page = 1, int pageSize = 12)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 50) pageSize = 12;

            var query = _context.Users
                .AsNoTracking()
                .Where(u => u.LoaiNguoiDung == RoleNames.Teacher);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                if (keyword.Length > 100)
                {
                    TempData["Error"] = "Từ khóa tìm kiếm quá dài (tối đa 100 ký tự).";
                    return View(new List<TeacherSearchViewModel>());
                }

                query = query.Where(u => u.HoTen.Contains(keyword) || u.Email.Contains(keyword));
            }

            var teachers = await query
                .OrderBy(u => u.HoTen)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var currentUserId = User.Identity.IsAuthenticated ? GetCurrentUserId() : null;

            var result = new List<TeacherSearchViewModel>();

            foreach (var teacher in teachers)
            {
                var courses = await _context.Courses
                    .AsNoTracking()
                    .Include(c => c.Enrollments)
                    .Where(c => c.UserID == teacher.Id && c.TrangThai == "Active")
                    .OrderByDescending(c => c.Enrollments.Count)
                    .Take(5) // Show only 5 most popular courses
                    .ToListAsync();

                var isFollowed = !string.IsNullOrEmpty(currentUserId) &&
                    await _context.Follows.AnyAsync(f => f.FollowerID == currentUserId && f.FollowedTeacherID == teacher.Id);

                result.Add(new TeacherSearchViewModel
                {
                    Teacher = teacher,
                    Courses = courses,
                    IsFollowed = isFollowed
                });
            }

            var totalTeachers = await query.CountAsync();

            ViewBag.Keyword = keyword;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalTeachers = totalTeachers;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalTeachers / pageSize);

            return View(result);
        }

        // API endpoint for auto-suggest teacher search
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> SuggestTeachers(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return Json(new List<object>());
            }

            var suggestions = await _context.Users
                .AsNoTracking()
                .Where(u => u.LoaiNguoiDung == RoleNames.Teacher &&
                           (u.HoTen.Contains(term) || u.Email.Contains(term)))
                .Take(10)
                .Select(u => new
                {
                    id = u.Id,
                    name = u.HoTen,
                    email = u.Email,
                    courseCount = _context.Courses.Count(c => c.UserID == u.Id && c.TrangThai == "Active")
                })
                .ToListAsync();

            return Json(suggestions);
        }

        #endregion

        #region My Following

        public async Task<IActionResult> MyFollowedTeachers(int page = 1, int pageSize = 12)
        {
            var userId = GetCurrentUserId();

            var followedTeachers = await _context.Follows
                .AsNoTracking()
                .Include(f => f.FollowedTeacher)
                .Where(f => f.FollowerID == userId)
                .OrderBy(f => f.FollowedTeacher.HoTen)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => f.FollowedTeacher)
                .ToListAsync();

            var result = new List<TeacherSearchViewModel>();

            foreach (var teacher in followedTeachers)
            {
                var courses = await _context.Courses
                    .AsNoTracking()
                    .Include(c => c.Enrollments)
                    .Where(c => c.UserID == teacher.Id && c.TrangThai == "Active")
                    .OrderByDescending(c => c.NgayTao)
                    .Take(3)
                    .ToListAsync();

                result.Add(new TeacherSearchViewModel
                {
                    Teacher = teacher,
                    Courses = courses,
                    IsFollowed = true
                });
            }

            var totalFollowed = await _context.Follows
                .CountAsync(f => f.FollowerID == userId);

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalFollowed = totalFollowed;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalFollowed / pageSize);

            return View(result);
        }

        #endregion

        #region Dashboard

        public async Task<IActionResult> Dashboard()
        {
            var userId = GetCurrentUserId();

            // Get user statistics
            var enrollmentCount = await _context.Enrollments
                .CountAsync(e => e.UserID == userId && e.TrangThai == "Active");

            var favoriteCount = await _context.FavoriteCourses
                .CountAsync(f => f.UserID == userId);

            var followingTeachersCount = await _context.Follows
                .CountAsync(f => f.FollowerID == userId);

            var followingCoursesCount = await _context.CourseFollows
                .CountAsync(f => f.UserID == userId);

            var averageProgress = await _context.Enrollments
                .Where(e => e.UserID == userId && e.TrangThai == "Active")
                .AverageAsync(e => (float?)e.Progress) ?? 0;

            // Recent enrollments
            var recentEnrollments = await _context.Enrollments
                .AsNoTracking()
                .Include(e => e.Course)
                    .ThenInclude(c => c.User)
                .Where(e => e.UserID == userId && e.TrangThai == "Active")
                .OrderByDescending(e => e.EnrollDate)
                .Take(5)
                .ToListAsync();

            // Recent quiz results
            var recentQuizResults = await _context.QuizResults
                .AsNoTracking()
                .Include(qr => qr.Quiz)
                    .ThenInclude(q => q.Course)
                .Where(qr => qr.UserID == userId)
                .OrderByDescending(qr => qr.TakenAt)
                .Take(5)
                .ToListAsync();

            // Payment statistics
            var totalSpent = await _context.Payments
                .Where(p => p.UserID == userId && p.Status == PaymentStatus.Success)
                .SumAsync(p => (decimal?)p.SoTien) ?? 0;

            var pendingPayments = await _context.Payments
                .CountAsync(p => p.UserID == userId &&
                    (p.Status == PaymentStatus.Pending || p.Status == PaymentStatus.WaitingConfirm));

            ViewBag.EnrollmentCount = enrollmentCount;
            ViewBag.FavoriteCount = favoriteCount;
            ViewBag.FollowingTeachersCount = followingTeachersCount;
            ViewBag.FollowingCoursesCount = followingCoursesCount;
            ViewBag.AverageProgress = Math.Round(averageProgress, 2);
            ViewBag.RecentEnrollments = recentEnrollments;
            ViewBag.RecentQuizResults = recentQuizResults;
            ViewBag.TotalSpent = totalSpent;
            ViewBag.PendingPayments = pendingPayments;

            return View();
        }

        #endregion

        #region Activity Tracking

        public async Task<IActionResult> MyActivity()
        {
            var userId = GetCurrentUserId();

            // Get all user activities
            var enrollments = await _context.Enrollments
                .AsNoTracking()
                .Include(e => e.Course)
                .Where(e => e.UserID == userId)
                .OrderByDescending(e => e.EnrollDate)
                .ToListAsync();

            var quizResults = await _context.QuizResults
                .AsNoTracking()
                .Include(qr => qr.Quiz)
                    .ThenInclude(q => q.Course)
                .Where(qr => qr.UserID == userId)
                .OrderByDescending(qr => qr.TakenAt)
                .ToListAsync();

            var submissions = await _context.Submissions
                .AsNoTracking()
                .Include(s => s.Assignment)
                    .ThenInclude(a => a.Course)
                .Where(s => s.UserID == userId)
                .OrderByDescending(s => s.NgayNop)
                .ToListAsync();

            ViewBag.Enrollments = enrollments;
            ViewBag.QuizResults = quizResults;
            ViewBag.Submissions = submissions;

            return View();
        }

        #endregion

        #region Statistics

        [HttpGet]
        public async Task<IActionResult> GetUserStats()
        {
            var userId = GetCurrentUserId();

            var stats = new
            {
                totalCourses = await _context.Enrollments.CountAsync(e => e.UserID == userId && e.TrangThai == "Active"),
                completedCourses = await _context.Enrollments.CountAsync(e => e.UserID == userId && e.Progress >= 100),
                inProgress = await _context.Enrollments.CountAsync(e => e.UserID == userId && e.Progress > 0 && e.Progress < 100),
                notStarted = await _context.Enrollments.CountAsync(e => e.UserID == userId && e.Progress == 0),
                totalQuizzes = await _context.QuizResults.CountAsync(qr => qr.UserID == userId),
                averageQuizScore = await _context.QuizResults
                    .Where(qr => qr.UserID == userId)
                    .AverageAsync(qr => (double?)qr.Score) ?? 0,
                totalAssignments = await _context.Submissions.CountAsync(s => s.UserID == userId),
                gradedAssignments = await _context.Submissions.CountAsync(s => s.UserID == userId && s.Diem.HasValue),
                averageAssignmentScore = await _context.Submissions
                    .Where(s => s.UserID == userId && s.Diem.HasValue)
                    .AverageAsync(s => s.Diem) ?? 0
            };

            return Json(stats);
        }

        #endregion

        #region Helper Methods

        private string GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        }

        #endregion
    }
}