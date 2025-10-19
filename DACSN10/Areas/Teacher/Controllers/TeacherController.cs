using DACSN10.Areas.Teacher.Service;
using DACSN10.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DACSN10.Controllers
{
    [Area("Teacher")]
    [Authorize(Roles = RoleNames.Teacher)]
    public class TeacherController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TeacherController> _logger;

        public TeacherController(AppDbContext context, ILogger<TeacherController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Live()
        {
            // Tuỳ bạn bind từ DB hay form; đây là ví dụ trống để View tạo mới
            var vm = new StreamSession();
            return View("Livestream", vm);
        }
        #region Dashboard

        public async Task<IActionResult> Dashboard()
        {
            var teacherId = GetCurrentUserId();

            // Statistics
            var totalCourses = await _context.Courses.CountAsync(c => c.UserID == teacherId);
            var activeCourses = await _context.Courses.CountAsync(c => c.UserID == teacherId && c.TrangThai == "Active");
            var totalStudents = await _context.Enrollments
                .Include(e => e.Course)
                .CountAsync(e => e.Course.UserID == teacherId && e.TrangThai == "Active");
            var totalRevenue = await _context.Payments
                .Include(p => p.Course)
                .Where(p => p.Course.UserID == teacherId && p.Status == PaymentStatus.Success)
                .SumAsync(p => (decimal?)p.SoTien) ?? 0;

            ViewBag.TotalCourses = totalCourses;
            ViewBag.ActiveCourses = activeCourses;
            ViewBag.TotalStudents = totalStudents;
            ViewBag.TotalRevenue = totalRevenue;

            // Recent enrollments
            var recentEnrollments = await _context.Enrollments
                .Include(e => e.Course)
                .Include(e => e.User)
                .Where(e => e.Course.UserID == teacherId)
                .OrderByDescending(e => e.EnrollDate)
                .Take(10)
                .ToListAsync();

            return View(recentEnrollments);
        }

        #endregion

        #region Course Management

        public async Task<IActionResult> MyCourses(int page = 1, int pageSize = 10)
        {
            var teacherId = GetCurrentUserId();

            var courses = await _context.Courses
                .Include(c => c.Enrollments)
                .Include(c => c.Lessons)
                .Include(c => c.CourseCategories).ThenInclude(cc => cc.Category)
                .Where(c => c.UserID == teacherId)
                .OrderByDescending(c => c.NgayTao)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalCourses = await _context.Courses.CountAsync(c => c.UserID == teacherId);

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCourses / pageSize);

            return View(courses);
        }

        public async Task<IActionResult> CreateCourse()
        {
            ViewBag.Categories = await GetCategoriesSelectList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourse(Course course, int[] selectedCategories)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    course.UserID = GetCurrentUserId();
                    course.NgayTao = DateTime.Now;
                    course.TrangThai = "Pending"; // Chờ admin duyệt

                    _context.Courses.Add(course);
                    await _context.SaveChangesAsync();

                    // Add categories
                    if (selectedCategories != null && selectedCategories.Length > 0)
                    {
                        foreach (var categoryId in selectedCategories)
                        {
                            _context.CourseCategories.Add(new CourseCategory
                            {
                                CourseID = course.CourseID,
                                CategoryID = categoryId
                            });
                        }
                        await _context.SaveChangesAsync();
                    }

                    TempData["Success"] = "Tạo khóa học thành công! Chờ admin duyệt.";
                    return RedirectToAction("MyCourses");
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Lỗi khi tạo khóa học: " + ex.Message;
                }
            }

            ViewBag.Categories = await GetCategoriesSelectList();
            return View(course);
        }

        public async Task<IActionResult> EditCourse(int id)
        {
            var teacherId = GetCurrentUserId();
            var course = await _context.Courses
                .Include(c => c.CourseCategories)
                .FirstOrDefaultAsync(c => c.CourseID == id && c.UserID == teacherId);

            if (course == null)
            {
                TempData["Error"] = "Không tìm thấy khóa học.";
                return NotFound();
            }

            ViewBag.Categories = await GetCategoriesSelectList();
            ViewBag.SelectedCategories = course.CourseCategories.Select(cc => cc.CategoryID).ToArray();

            return View(course);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourse(Course course, int[] selectedCategories)
        {
            var teacherId = GetCurrentUserId();
            var existingCourse = await _context.Courses
                .Include(c => c.CourseCategories)
                .FirstOrDefaultAsync(c => c.CourseID == course.CourseID && c.UserID == teacherId);

            if (existingCourse == null)
            {
                TempData["Error"] = "Không tìm thấy khóa học.";
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    existingCourse.TenKhoaHoc = course.TenKhoaHoc;
                    existingCourse.MoTa = course.MoTa;
                    existingCourse.Gia = course.Gia;
                    existingCourse.TrangThai = course.TrangThai;

                    // Update categories
                    _context.CourseCategories.RemoveRange(existingCourse.CourseCategories);

                    if (selectedCategories != null && selectedCategories.Length > 0)
                    {
                        foreach (var categoryId in selectedCategories)
                        {
                            _context.CourseCategories.Add(new CourseCategory
                            {
                                CourseID = course.CourseID,
                                CategoryID = categoryId
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật khóa học thành công!";
                    return RedirectToAction("MyCourses");
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Lỗi khi cập nhật: " + ex.Message;
                }
            }

            ViewBag.Categories = await GetCategoriesSelectList();
            ViewBag.SelectedCategories = selectedCategories;
            return View(course);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var teacherId = GetCurrentUserId();
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseID == id && c.UserID == teacherId);

            if (course == null)
            {
                return Json(new { success = false, message = "Không tìm thấy khóa học." });
            }

            // Check if course has enrollments
            var hasEnrollments = await _context.Enrollments.AnyAsync(e => e.CourseID == id);
            if (hasEnrollments)
            {
                return Json(new { success = false, message = "Không thể xóa khóa học đã có học viên đăng ký." });
            }

            try
            {
                // Remove related data first
                var courseCategories = await _context.CourseCategories.Where(cc => cc.CourseID == id).ToListAsync();
                _context.CourseCategories.RemoveRange(courseCategories);

                var lessons = await _context.Lessons.Where(l => l.CourseID == id).ToListAsync();
                _context.Lessons.RemoveRange(lessons);

                var quizzes = await _context.Quizzes.Where(q => q.CourseID == id).ToListAsync();
                foreach (var quiz in quizzes)
                {
                    var questions = await _context.Questions.Where(q => q.QuizID == quiz.QuizID).ToListAsync();
                    _context.Questions.RemoveRange(questions);
                }
                _context.Quizzes.RemoveRange(quizzes);

                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa khóa học thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi xóa: " + ex.Message });
            }
        }

        #endregion

        #region Lesson Management

        public async Task<IActionResult> CourseLessons(int courseId)
        {
            var teacherId = GetCurrentUserId();
            var course = await _context.Courses
                .Include(c => c.Lessons)
                .FirstOrDefaultAsync(c => c.CourseID == courseId && c.UserID == teacherId);

            if (course == null)
            {
                TempData["Error"] = "Không tìm thấy khóa học.";
                return NotFound();
            }

            ViewBag.Course = course;
            return View(course.Lessons.OrderBy(l => l.LessonID));
        }

        public async Task<IActionResult> CreateLesson(int courseId)
        {
            var teacherId = GetCurrentUserId();
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseID == courseId && c.UserID == teacherId);

            if (course == null)
            {
                TempData["Error"] = "Không tìm thấy khóa học.";
                return NotFound();
            }

            ViewBag.Course = course;
            var lesson = new Lesson { CourseID = courseId };
            return View(lesson);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLesson(Lesson lesson)
        {
            try
            {
                var teacherId = GetCurrentUserId();

                // Debug logging
                _logger?.LogInformation($"CreateLesson called - CourseID: {lesson.CourseID}, TenBaiHoc: {lesson.TenBaiHoc}");

                // Verify course exists and belongs to teacher
                var course = await _context.Courses
                    .FirstOrDefaultAsync(c => c.CourseID == lesson.CourseID && c.UserID == teacherId);

                if (course == null)
                {
                    TempData["Error"] = "Không tìm thấy khóa học hoặc bạn không có quyền.";
                    return RedirectToAction("MyCourses");
                }

                // Clear model state for navigation properties
                var keysToRemove = ModelState.Keys.Where(k => k.StartsWith("Course.")).ToList();
                foreach (var key in keysToRemove)
                {
                    ModelState.Remove(key);
                }

                // Basic validation
                if (string.IsNullOrWhiteSpace(lesson.TenBaiHoc))
                {
                    ModelState.AddModelError("TenBaiHoc", "Tên bài học là bắt buộc.");
                }

                if (string.IsNullOrWhiteSpace(lesson.NoiDung))
                {
                    ModelState.AddModelError("NoiDung", "Nội dung bài học là bắt buộc.");
                }

                if (lesson.ThoiLuong <= 0)
                {
                    ModelState.AddModelError("ThoiLuong", "Thời lượng phải lớn hơn 0.");
                }

                if (!ModelState.IsValid)
                {
                    ViewBag.Course = course;
                    return View(lesson);
                }

                // Ensure VideoUrl is not null
                if (string.IsNullOrEmpty(lesson.VideoUrl))
                {
                    lesson.VideoUrl = string.Empty;
                }

                // Create the lesson
                _context.Lessons.Add(lesson);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Tạo bài học thành công!";
                return RedirectToAction("CourseLessons", new { courseId = lesson.CourseID });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating lesson");
                TempData["Error"] = "Có lỗi xảy ra khi tạo bài học. Vui lòng thử lại.";

                // Reload course for view
                var teacherId = GetCurrentUserId();
                var courseForError = await _context.Courses
                    .FirstOrDefaultAsync(c => c.CourseID == lesson.CourseID && c.UserID == teacherId);

                ViewBag.Course = courseForError;
                return View(lesson);
            }
        }

        // Thêm action này vào TeacherController.cs trong phần #region Lesson Management

        public async Task<IActionResult> LessonDetails(int id)
        {
            var teacherId = GetCurrentUserId();
            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.LessonID == id && l.Course.UserID == teacherId);

            if (lesson == null)
            {
                TempData["Error"] = "Không tìm thấy bài học.";
                return NotFound();
            }

            ViewBag.Course = lesson.Course;
            return View(lesson);
        }


        public async Task<IActionResult> EditLesson(int id)
        {
            var teacherId = GetCurrentUserId();
            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.LessonID == id && l.Course.UserID == teacherId);

            if (lesson == null)
            {
                TempData["Error"] = "Không tìm thấy bài học.";
                return NotFound();
            }

            ViewBag.Course = lesson.Course;
            return View(lesson);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLesson(Lesson lesson)
        {
            try
            {
                var teacherId = GetCurrentUserId();

                // Lấy bài học hiện tại từ database
                var existingLesson = await _context.Lessons
                    .Include(l => l.Course)
                    .FirstOrDefaultAsync(l => l.LessonID == lesson.LessonID && l.Course.UserID == teacherId);

                if (existingLesson == null)
                {
                    TempData["Error"] = "Không tìm thấy bài học hoặc bạn không có quyền chỉnh sửa.";
                    return NotFound();
                }

                // Xóa các ModelState errors cho navigation properties
                var keysToRemove = ModelState.Keys.Where(k => k.StartsWith("Course.")).ToList();
                foreach (var key in keysToRemove)
                {
                    ModelState.Remove(key);
                }

                // Validate dữ liệu
                if (string.IsNullOrWhiteSpace(lesson.TenBaiHoc))
                {
                    ModelState.AddModelError("TenBaiHoc", "Tên bài học là bắt buộc.");
                }

                if (string.IsNullOrWhiteSpace(lesson.NoiDung))
                {
                    ModelState.AddModelError("NoiDung", "Nội dung bài học là bắt buộc.");
                }

                if (lesson.ThoiLuong <= 0)
                {
                    ModelState.AddModelError("ThoiLuong", "Thời lượng phải lớn hơn 0.");
                }

                if (!ModelState.IsValid)
                {
                    ViewBag.Course = existingLesson.Course;
                    return View(lesson);
                }

                // Cập nhật thông tin bài học
                existingLesson.TenBaiHoc = lesson.TenBaiHoc;
                existingLesson.NoiDung = lesson.NoiDung;
                existingLesson.ThoiLuong = lesson.ThoiLuong;
                existingLesson.VideoUrl = lesson.VideoUrl ?? string.Empty;

                // Lưu thay đổi
                _context.Lessons.Update(existingLesson);
                await _context.SaveChangesAsync();

                _logger?.LogInformation($"Lesson {lesson.LessonID} updated successfully by teacher {teacherId}");

                TempData["Success"] = "Cập nhật bài học thành công!";
                return RedirectToAction("CourseLessons", new { courseId = existingLesson.CourseID });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error updating lesson {lesson.LessonID}");
                TempData["Error"] = "Có lỗi xảy ra khi cập nhật bài học. Vui lòng thử lại.";

                // Reload course data for view
                var teacherId = GetCurrentUserId();
                var courseForError = await _context.Courses
                    .FirstOrDefaultAsync(c => c.CourseID == lesson.CourseID && c.UserID == teacherId);

                ViewBag.Course = courseForError;
                return View(lesson);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLesson(int id)
        {
            var teacherId = GetCurrentUserId();
            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.LessonID == id && l.Course.UserID == teacherId);

            if (lesson == null)
            {
                return Json(new { success = false, message = "Không tìm thấy bài học." });
            }

            try
            {
                _context.Lessons.Remove(lesson);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Xóa bài học thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi xóa: " + ex.Message });
            }
        }

        #endregion

        #region Student Management

        public async Task<IActionResult> MyStudents(int page = 1, int pageSize = 20)
        {
            var teacherId = GetCurrentUserId();

            var students = await _context.Enrollments
                .Include(e => e.User)
                .Include(e => e.Course)
                .Where(e => e.Course.UserID == teacherId && e.TrangThai == "Active")
                .OrderByDescending(e => e.EnrollDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalStudents = await _context.Enrollments
                .Include(e => e.Course)
                .CountAsync(e => e.Course.UserID == teacherId && e.TrangThai == "Active");

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalStudents / pageSize);

            return View(students);
        }

        public async Task<IActionResult> CourseStudents(int courseId, int page = 1, int pageSize = 20)
        {
            var teacherId = GetCurrentUserId();
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseID == courseId && c.UserID == teacherId);

            if (course == null)
            {
                TempData["Error"] = "Không tìm thấy khóa học.";
                return NotFound();
            }

            var enrollments = await _context.Enrollments
                .Include(e => e.User)
                .Where(e => e.CourseID == courseId && e.TrangThai == "Active")
                .OrderByDescending(e => e.EnrollDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalEnrollments = await _context.Enrollments
                .CountAsync(e => e.CourseID == courseId && e.TrangThai == "Active");

            ViewBag.Course = course;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalEnrollments / pageSize);

            return View(enrollments);
        }

        public async Task<IActionResult> MyFollowers()
        {
            var teacherId = GetCurrentUserId();
            var followers = await _context.Follows
                .Include(f => f.Follower)
                .Where(f => f.FollowedTeacherID == teacherId)
                .OrderByDescending(f => f.Follower.NgayDangKy)
                .ToListAsync();

            return View(followers);
        }

        #endregion

        #region Quiz Management

        public async Task<IActionResult> CourseQuizzes(int courseId)
        {
            var teacherId = GetCurrentUserId();
            var course = await _context.Courses
                .Include(c => c.Quizzes).ThenInclude(q => q.Questions)
                .FirstOrDefaultAsync(c => c.CourseID == courseId && c.UserID == teacherId);

            if (course == null)
            {
                TempData["Error"] = "Không tìm thấy khóa học.";
                return NotFound();
            }

            ViewBag.Course = course;
            return View(course.Quizzes);
        }

        public async Task<IActionResult> CreateQuiz(int courseId)
        {
            var teacherId = GetCurrentUserId();
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseID == courseId && c.UserID == teacherId);

            if (course == null)
            {
                TempData["Error"] = "Không tìm thấy khóa học.";
                return NotFound();
            }

            ViewBag.Course = course;
            return View(new Quiz { CourseID = courseId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateQuiz(Quiz quiz)
        {
            var teacherId = GetCurrentUserId();
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseID == quiz.CourseID && c.UserID == teacherId);

            if (course == null)
            {
                TempData["Error"] = "Không tìm thấy khóa học.";
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Quizzes.Add(quiz);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Tạo bài kiểm tra thành công!";
                    return RedirectToAction("EditQuiz", new { id = quiz.QuizID });
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Lỗi khi tạo bài kiểm tra: " + ex.Message;
                }
            }

            ViewBag.Course = course;
            return View(quiz);
        }

        public async Task<IActionResult> EditQuiz(int id)
        {
            var teacherId = GetCurrentUserId();
            var quiz = await _context.Quizzes
                .Include(q => q.Course)
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.QuizID == id && q.Course.UserID == teacherId);

            if (quiz == null)
            {
                TempData["Error"] = "Không tìm thấy bài kiểm tra.";
                return NotFound();
            }

            return View(quiz);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQuestion(int quizId, Question question)
        {
            var teacherId = GetCurrentUserId();
            var quiz = await _context.Quizzes
                .Include(q => q.Course)
                .FirstOrDefaultAsync(q => q.QuizID == quizId && q.Course.UserID == teacherId);

            if (quiz == null)
            {
                return Json(new { success = false, message = "Không tìm thấy bài kiểm tra." });
            }

            if (string.IsNullOrWhiteSpace(question.QuestionText) ||
                string.IsNullOrWhiteSpace(question.OptionA) ||
                string.IsNullOrWhiteSpace(question.OptionB) ||
                string.IsNullOrWhiteSpace(question.CorrectAnswer))
            {
                return Json(new { success = false, message = "Vui lòng nhập đầy đủ thông tin câu hỏi." });
            }

            try
            {
                question.QuizID = quizId;
                _context.Questions.Add(question);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Thêm câu hỏi thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi thêm câu hỏi: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var teacherId = GetCurrentUserId();
            var question = await _context.Questions
                .Include(q => q.Quiz).ThenInclude(q => q.Course)
                .FirstOrDefaultAsync(q => q.QuestionID == id && q.Quiz.Course.UserID == teacherId);

            if (question == null)
            {
                return Json(new { success = false, message = "Không tìm thấy câu hỏi." });
            }

            try
            {
                _context.Questions.Remove(question);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Xóa câu hỏi thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi xóa: " + ex.Message });
            }
        }

        #endregion

        #region Statistics

        public async Task<IActionResult> Statistics()
        {
            var teacherId = GetCurrentUserId();

            var stats = new
            {
                TotalCourses = await _context.Courses.CountAsync(c => c.UserID == teacherId),
                ActiveCourses = await _context.Courses.CountAsync(c => c.UserID == teacherId && c.TrangThai == "Active"),
                TotalStudents = await _context.Enrollments
                    .Include(e => e.Course)
                    .CountAsync(e => e.Course.UserID == teacherId && e.TrangThai == "Active"),
                TotalRevenue = await _context.Payments
                    .Include(p => p.Course)
                    .Where(p => p.Course.UserID == teacherId && p.Status == PaymentStatus.Success)
                    .SumAsync(p => (decimal?)p.SoTien) ?? 0,
                Followers = await _context.Follows.CountAsync(f => f.FollowedTeacherID == teacherId)
            };

            ViewBag.Stats = stats;

            // Course performance
            var courseStats = await _context.Courses
                .Include(c => c.Enrollments)
                .Where(c => c.UserID == teacherId)
                .Select(c => new
                {
                    c.TenKhoaHoc,
                    c.CourseID,
                    StudentCount = c.Enrollments.Count(e => e.TrangThai == "Active"),
                    Revenue = c.Payments.Where(p => p.Status == PaymentStatus.Success).Sum(p => p.SoTien)
                })
                .OrderByDescending(c => c.StudentCount)
                .Take(10)
                .ToListAsync();

            ViewBag.CourseStats = courseStats;

            return View();
        }

        #endregion

        #region Assignment Management

        public async Task<IActionResult> CourseAssignments(int courseId)
        {
            var teacherId = GetCurrentUserId();
            var course = await _context.Courses
                .Include(c => c.Assignments).ThenInclude(a => a.Submissions).ThenInclude(s => s.User)
                .FirstOrDefaultAsync(c => c.CourseID == courseId && c.UserID == teacherId);

            if (course == null)
            {
                TempData["Error"] = "Không tìm thấy khóa học.";
                return NotFound();
            }

            ViewBag.Course = course;
            return View(course.Assignments.OrderBy(a => a.HanNop));
        }

        public async Task<IActionResult> CreateAssignment(int courseId)
        {
            var teacherId = GetCurrentUserId();
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseID == courseId && c.UserID == teacherId);

            if (course == null)
            {
                TempData["Error"] = "Không tìm thấy khóa học.";
                return NotFound();
            }

            ViewBag.Course = course;
            return View(new Assignment { CourseID = courseId, HanNop = DateTime.Now.AddDays(7) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAssignment(Assignment assignment)
        {
            var teacherId = GetCurrentUserId();
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseID == assignment.CourseID && c.UserID == teacherId);

            if (course == null)
            {
                TempData["Error"] = "Không tìm thấy khóa học.";
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Assignments.Add(assignment);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Tạo bài tập thành công!";
                    return RedirectToAction("CourseAssignments", new { courseId = assignment.CourseID });
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Lỗi khi tạo bài tập: " + ex.Message;
                }
            }

            ViewBag.Course = course;
            return View(assignment);
        }

        public async Task<IActionResult> EditAssignment(int id)
        {
            var teacherId = GetCurrentUserId();
            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.AssignmentID == id && a.Course.UserID == teacherId);

            if (assignment == null)
            {
                TempData["Error"] = "Không tìm thấy bài tập.";
                return NotFound();
            }

            ViewBag.Course = assignment.Course;
            return View(assignment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAssignment(Assignment assignment)
        {
            var teacherId = GetCurrentUserId();
            var existingAssignment = await _context.Assignments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.AssignmentID == assignment.AssignmentID && a.Course.UserID == teacherId);

            if (existingAssignment == null)
            {
                TempData["Error"] = "Không tìm thấy bài tập.";
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    existingAssignment.TenBaiTap = assignment.TenBaiTap;
                    existingAssignment.MoTa = assignment.MoTa;
                    existingAssignment.HanNop = assignment.HanNop;

                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật bài tập thành công!";
                    return RedirectToAction("CourseAssignments", new { courseId = existingAssignment.CourseID });
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Lỗi khi cập nhật: " + ex.Message;
                }
            }

            ViewBag.Course = existingAssignment.Course;
            return View(assignment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAssignment(int id)
        {
            var teacherId = GetCurrentUserId();
            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .Include(a => a.Submissions)
                .FirstOrDefaultAsync(a => a.AssignmentID == id && a.Course.UserID == teacherId);

            if (assignment == null)
            {
                return Json(new { success = false, message = "Không tìm thấy bài tập." });
            }

            try
            {
                // Remove submissions first
                _context.Submissions.RemoveRange(assignment.Submissions);
                _context.Assignments.Remove(assignment);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa bài tập thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi xóa: " + ex.Message });
            }
        }

        public async Task<IActionResult> AssignmentSubmissions(int assignmentId)
        {
            var teacherId = GetCurrentUserId();
            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .Include(a => a.Submissions).ThenInclude(s => s.User)
                .FirstOrDefaultAsync(a => a.AssignmentID == assignmentId && a.Course.UserID == teacherId);

            if (assignment == null)
            {
                TempData["Error"] = "Không tìm thấy bài tập.";
                return NotFound();
            }

            return View(assignment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GradeSubmission(int submissionId, double grade)
        {
            var teacherId = GetCurrentUserId();
            var submission = await _context.Submissions
                .Include(s => s.Assignment).ThenInclude(a => a.Course)
                .FirstOrDefaultAsync(s => s.SubmissionID == submissionId && s.Assignment.Course.UserID == teacherId);

            if (submission == null)
            {
                return Json(new { success = false, message = "Không tìm thấy bài nộp." });
            }

            if (grade < 0 || grade > 10)
            {
                return Json(new { success = false, message = "Điểm phải từ 0 đến 10." });
            }

            try
            {
                submission.Diem = grade;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Chấm điểm thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi chấm điểm: " + ex.Message });
            }
        }

        #endregion

        #region Reports & Analytics

        public async Task<IActionResult> TeacherReports()
        {
            var teacherId = GetCurrentUserId();

            // Course performance
            var coursePerformance = await _context.Courses
                .Include(c => c.Enrollments)
                .Include(c => c.Payments)
                .Where(c => c.UserID == teacherId)
                .Select(c => new
                {
                    c.CourseID,
                    c.TenKhoaHoc,
                    c.TrangThai,
                    StudentCount = c.Enrollments.Count(e => e.TrangThai == "Active"),
                    Revenue = c.Payments.Where(p => p.Status == PaymentStatus.Success).Sum(p => p.SoTien),
                    AverageProgress = c.Enrollments.Any() ? c.Enrollments.Average(e => e.Progress) : 0
                })
                .ToListAsync();

            // Monthly revenue
            var monthlyRevenue = await _context.Payments
                .Include(p => p.Course)
                .Where(p => p.Course.UserID == teacherId && p.Status == PaymentStatus.Success &&
                           p.NgayThanhToan >= DateTime.Now.AddMonths(-6))
                .GroupBy(p => new { p.NgayThanhToan.Year, p.NgayThanhToan.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(p => p.SoTien),
                    Count = g.Count()
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            ViewBag.CoursePerformance = coursePerformance;
            ViewBag.MonthlyRevenue = monthlyRevenue;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetCourseStats(int courseId)
        {
            var teacherId = GetCurrentUserId();
            var course = await _context.Courses
                .Include(c => c.Enrollments)
                .Include(c => c.Lessons)
                .Include(c => c.Quizzes).ThenInclude(q => q.QuizResults)
                .Include(c => c.Assignments).ThenInclude(a => a.Submissions)
                .FirstOrDefaultAsync(c => c.CourseID == courseId && c.UserID == teacherId);

            if (course == null)
            {
                return Json(new { success = false, message = "Không tìm thấy khóa học." });
            }

            var stats = new
            {
                totalStudents = course.Enrollments.Count(e => e.TrangThai == "Active"),
                averageProgress = course.Enrollments.Any() ? course.Enrollments.Average(e => e.Progress) : 0,
                totalLessons = course.Lessons.Count,
                totalQuizzes = course.Quizzes.Count,
                totalAssignments = course.Assignments.Count,
                quizAttempts = course.Quizzes.Sum(q => q.QuizResults.Count),
                averageQuizScore = course.Quizzes.SelectMany(q => q.QuizResults).Any()
                    ? course.Quizzes.SelectMany(q => q.QuizResults).Average(qr => qr.Score) : 0,
                submittedAssignments = course.Assignments.Sum(a => a.Submissions.Count),
                gradedAssignments = course.Assignments.Sum(a => a.Submissions.Count(s => s.Diem.HasValue))
            };

            return Json(stats);
        }

        #endregion

        #region Helper Methods

        private string GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        }

        private async Task<SelectList> GetCategoriesSelectList()
        {
            var categories = await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();

            return new SelectList(categories, "CategoryID", "Name");
        }

        #endregion
    }
}