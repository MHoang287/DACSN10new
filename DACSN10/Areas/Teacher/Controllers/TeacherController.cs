using DACSN10.Areas.Teacher.ViewModels;
using DACSN10.Models;
using DACSN10.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace DACSN10.Controllers
{
    [Area("Teacher")]
    [Authorize(Roles = RoleNames.Teacher)]
    public class TeacherController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TeacherController> _logger;
        private readonly INotificationService _notificationService;

        public TeacherController(AppDbContext context, ILogger<TeacherController> logger, INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _notificationService = notificationService;
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
                    _logger.LogError(ex, "CreateCourse failed");
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
                    _logger.LogError(ex, "EditCourse failed");
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
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.CourseID == id && c.UserID == teacherId);

            if (course == null)
            {
                return Json(new { success = false, message = "Không tìm thấy khóa học." });
            }

            if (course.Enrollments.Any())
            {
                return Json(new { success = false, message = "Không thể xóa khóa học đã có học viên đăng ký." });
            }

            try
            {
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

        #region Quiz Management

        // Hiển thị danh sách quiz của khóa học (giáo viên)
        public async Task<IActionResult> CourseQuizzes(int courseId)
        {
            var teacherId = GetCurrentUserId();

            var course = await _context.Courses
                .Include(c => c.Lessons)
                .Include(c => c.Enrollments)
                .Include(c => c.Quizzes).ThenInclude(q => q.Questions)
                .FirstOrDefaultAsync(c => c.CourseID == courseId && c.UserID == teacherId);

            if (course == null)
            {
                TempData["Error"] = "Không tìm thấy khóa học.";
                return NotFound();
            }

            var quizzes = await _context.Quizzes
                .Include(q => q.Questions)
                .Include(q => q.QuizResults)
                .Where(q => q.CourseID == courseId)
                .OrderBy(q => q.QuizID)
                .ToListAsync();

            ViewBag.Course = course;
            return View(quizzes);
        }

        // Hiển thị form tạo quiz mới
        [HttpGet]
        public async Task<IActionResult> CreateQuiz(int courseId)
        {
            var teacherId = GetCurrentUserId();
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseID == courseId && c.UserID == teacherId);

            if (course == null)
            {
                TempData["Error"] = "Không tìm thấy khóa học.";
                return RedirectToAction("MyCourses");
            }

            ViewBag.Course = course;

            var quiz = new Quiz
            {
                CourseID = courseId,
                DurationMinutes = 30
            };

            return View(quiz);
        }

        // Xử lý tạo quiz mới - robust: đọc từ binder hoặc Request.Form, parse questions[...] nếu cần
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateQuiz(Quiz quiz, List<QuestionViewModel> questions)
        {
            try
            {
                _logger.LogInformation("=== CreateQuiz START ===");
                var teacherId = GetCurrentUserId();
                _logger.LogInformation("Teacher ID: {TeacherId}", teacherId);
                _logger.LogInformation("CourseID: {CourseId}", quiz.CourseID);
                _logger.LogInformation("Title: {Title}", quiz.Title);
                _logger.LogInformation("Duration: {Duration}", quiz.DurationMinutes);
                _logger.LogInformation("Questions count: {Count}", questions?.Count ?? 0);

                // Validate course exists and belongs to teacher
                var course = await _context.Courses
                    .FirstOrDefaultAsync(c => c.CourseID == quiz.CourseID && c.UserID == teacherId);

                if (course == null)
                {
                    _logger.LogError("Course not found. CourseID: {CourseId}, TeacherId: {TeacherId}",
                        quiz.CourseID, teacherId);
                    TempData["Error"] = "Không tìm thấy khóa học hoặc bạn không có quyền.";
                    return RedirectToAction("MyCourses");
                }

                _logger.LogInformation("Course found: {CourseName}", course.TenKhoaHoc);

                // Validate input
                if (string.IsNullOrWhiteSpace(quiz.Title))
                {
                    _logger.LogError("Title is empty");
                    TempData["Error"] = "Vui lòng nhập tiêu đề bài kiểm tra.";
                    ViewBag.Course = course;
                    return View(quiz);
                }

                if (questions == null || questions.Count == 0)
                {
                    _logger.LogError("No questions provided");
                    TempData["Error"] = "Vui lòng thêm ít nhất một câu hỏi.";
                    ViewBag.Course = course;
                    return View(quiz);
                }

                // Validate each question
                for (int i = 0; i < questions.Count; i++)
                {
                    var q = questions[i];
                    _logger.LogInformation("Question {Index}: Text={Text}, A={A}, B={B}, Correct={Correct}",
                        i, q.QuestionText, q.OptionA, q.OptionB, q.CorrectAnswer);

                    if (string.IsNullOrWhiteSpace(q.QuestionText))
                    {
                        TempData["Error"] = $"Câu hỏi {i + 1}: Vui lòng nhập nội dung câu hỏi.";
                        ViewBag.Course = course;
                        return View(quiz);
                    }

                    if (string.IsNullOrWhiteSpace(q.OptionA) || string.IsNullOrWhiteSpace(q.OptionB))
                    {
                        TempData["Error"] = $"Câu hỏi {i + 1}: Vui lòng nhập ít nhất 2 đáp án (A và B).";
                        ViewBag.Course = course;
                        return View(quiz);
                    }

                    if (string.IsNullOrWhiteSpace(q.CorrectAnswer))
                    {
                        TempData["Error"] = $"Câu hỏi {i + 1}: Vui lòng chọn đáp án đúng.";
                        ViewBag.Course = course;
                        return View(quiz);
                    }
                }

                _logger.LogInformation("✅ All validations passed");

                // Create Quiz entity
                var newQuiz = new Quiz
                {
                    Title = quiz.Title.Trim(),
                    CourseID = quiz.CourseID,
                    DurationMinutes = quiz.DurationMinutes > 0 ? quiz.DurationMinutes : 30
                };

                _logger.LogInformation("Creating quiz in database...");

                // FIX: Use execution strategy for retry-compatible transaction
                var strategy = _context.Database.CreateExecutionStrategy();

                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();

                    try
                    {
                        // Add Quiz
                        _context.Quizzes.Add(newQuiz);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation("✅ Quiz created with ID: {QuizId}", newQuiz.QuizID);

                        // Add Questions
                        foreach (var qvm in questions)
                        {
                            var question = new Question
                            {
                                QuizID = newQuiz.QuizID,
                                QuestionText = qvm.QuestionText.Trim(),
                                OptionA = qvm.OptionA.Trim(),
                                OptionB = qvm.OptionB.Trim(),
                                OptionC = qvm.OptionC?.Trim(),
                                OptionD = qvm.OptionD?.Trim(),
                                CorrectAnswer = qvm.CorrectAnswer
                            };

                            _context.Questions.Add(question);
                            _logger.LogInformation("Added question: {Text}", question.QuestionText);
                        }

                        await _context.SaveChangesAsync();
                        _logger.LogInformation("✅ {Count} questions saved", questions.Count);

                        // Commit transaction
                        await transaction.CommitAsync();
                        _logger.LogInformation("✅ Transaction committed");
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "❌ Error in transaction");
                        throw;
                    }
                });

                _logger.LogInformation("=== CreateQuiz SUCCESS ===");
                _logger.LogInformation("Created Quiz ID={QuizId}, Title={Title}, Questions={Count}",
                    newQuiz.QuizID, newQuiz.Title, questions.Count);
                await _notificationService.NotifyNewQuizAsync(
                        quizId: quiz.QuizID,
                        courseId: quiz.CourseID
                    );
                TempData["Success"] = "Tạo bài kiểm tra thành công!";
                return RedirectToAction("CourseQuizzes", new { courseId = quiz.CourseID });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== CreateQuiz FAILED ===");
                TempData["Error"] = $"Có lỗi xảy ra: {ex.Message}";

                // Try to reload course for view
                try
                {
                    var teacherId = GetCurrentUserId();
                    var course = await _context.Courses
                        .FirstOrDefaultAsync(c => c.CourseID == quiz.CourseID && c.UserID == teacherId);
                    ViewBag.Course = course;
                }
                catch
                {
                    return RedirectToAction("MyCourses");
                }

                return View(quiz);
            }
        }

        // Hiển thị form chỉnh sửa quiz
        [HttpGet]
        public async Task<IActionResult> EditQuiz(int id)
        {
            try
            {
                var teacherId = GetCurrentUserId();
                var quiz = await _context.Quizzes
                    .Include(q => q.Course)
                    .Include(q => q.Questions)
                    .Include(q => q.QuizResults)
                    .FirstOrDefaultAsync(q => q.QuizID == id && q.Course != null && q.Course.UserID == teacherId);

                if (quiz == null)
                {
                    TempData["Error"] = "Không tìm thấy bài kiểm tra hoặc bạn không có quyền chỉnh sửa.";
                    return RedirectToAction("MyCourses");
                }

                ViewBag.Course = quiz.Course;
                return View(quiz);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading quiz {QuizId}", id);
                TempData["Error"] = "Có lỗi xảy ra khi tải bài kiểm tra.";
                return RedirectToAction("MyCourses");
            }
        }

        // Xử lý cập nhật quiz
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditQuiz(Quiz quiz)
        {
            try
            {
                _logger.LogInformation("=== EditQuiz START ===");
                _logger.LogInformation("QuizID: {QuizId}", quiz.QuizID);
                _logger.LogInformation("Title: {Title}", quiz.Title);
                _logger.LogInformation("Duration: {Duration}", quiz.DurationMinutes);

                var teacherId = GetCurrentUserId();
                var existingQuiz = await _context.Quizzes
                    .Include(q => q.Course)
                    .FirstOrDefaultAsync(q => q.QuizID == quiz.QuizID && q.Course != null && q.Course.UserID == teacherId);

                if (existingQuiz == null)
                {
                    _logger.LogError("Quiz not found or unauthorized");
                    TempData["Error"] = "Không tìm thấy bài kiểm tra hoặc bạn không có quyền chỉnh sửa.";
                    return RedirectToAction("MyCourses");
                }

                // Validate
                if (string.IsNullOrWhiteSpace(quiz.Title))
                {
                    TempData["Error"] = "Vui lòng nhập tiêu đề bài kiểm tra.";
                    ViewBag.Course = existingQuiz.Course;
                    return View(existingQuiz);
                }

                // Update
                existingQuiz.Title = quiz.Title.Trim();
                existingQuiz.DurationMinutes = quiz.DurationMinutes > 0 ? quiz.DurationMinutes : 30;

                _context.Quizzes.Update(existingQuiz);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Quiz updated successfully");
                _logger.LogInformation("=== EditQuiz SUCCESS ===");

                TempData["Success"] = "Cập nhật bài kiểm tra thành công!";
                return RedirectToAction("EditQuiz", new { id = quiz.QuizID });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== EditQuiz FAILED ===");
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction("EditQuiz", new { id = quiz.QuizID });
            }
        }

        // GET question data for editing
        [HttpGet]
        public async Task<IActionResult> GetQuestion(int id)
        {
            try
            {
                var teacherId = GetCurrentUserId();
                var question = await _context.Questions
                    .Include(q => q.Quiz).ThenInclude(qz => qz.Course)
                    .FirstOrDefaultAsync(q => q.QuestionID == id
                                              && q.Quiz != null
                                              && q.Quiz.Course != null
                                              && q.Quiz.Course.UserID == teacherId);

                if (question == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy câu hỏi" });
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        questionID = question.QuestionID,
                        quizID = question.QuizID,
                        questionText = question.QuestionText,
                        optionA = question.OptionA,
                        optionB = question.OptionB,
                        optionC = question.OptionC,
                        optionD = question.OptionD,
                        correctAnswer = question.CorrectAnswer
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading question {QuestionId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải câu hỏi" });
            }
        }

        // Thêm câu hỏi vào quiz (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQuestion(int quizId, string questionText,
            string optionA, string optionB, string optionC, string optionD, string correctAnswer)
        {
            try
            {
                _logger.LogInformation("=== AddQuestion START ===");
                _logger.LogInformation("QuizID: {QuizId}", quizId);

                var teacherId = GetCurrentUserId();
                var quiz = await _context.Quizzes
                    .Include(q => q.Course)
                    .FirstOrDefaultAsync(q => q.QuizID == quizId && q.Course != null && q.Course.UserID == teacherId);

                if (quiz == null)
                {
                    _logger.LogError("Quiz not found: {QuizId}", quizId);
                    return Json(new { success = false, message = "Không tìm thấy bài kiểm tra" });
                }

                // Validate
                if (string.IsNullOrWhiteSpace(questionText))
                {
                    return Json(new { success = false, message = "Vui lòng nhập nội dung câu hỏi" });
                }

                if (string.IsNullOrWhiteSpace(optionA) || string.IsNullOrWhiteSpace(optionB))
                {
                    return Json(new { success = false, message = "Vui lòng nhập ít nhất 2 đáp án (A và B)" });
                }

                if (string.IsNullOrWhiteSpace(correctAnswer))
                {
                    return Json(new { success = false, message = "Vui lòng chọn đáp án đúng" });
                }

                // Create question
                var question = new Question
                {
                    QuizID = quizId,
                    QuestionText = questionText.Trim(),
                    OptionA = optionA.Trim(),
                    OptionB = optionB.Trim(),
                    OptionC = optionC?.Trim(),
                    OptionD = optionD?.Trim(),
                    CorrectAnswer = correctAnswer
                };

                _context.Questions.Add(question);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Question added with ID: {QuestionId}", question.QuestionID);
                _logger.LogInformation("=== AddQuestion SUCCESS ===");

                return Json(new { success = true, message = "Thêm câu hỏi thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== AddQuestion FAILED ===");
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // Cập nhật câu hỏi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuestion(int id, string questionText,
            string optionA, string optionB, string optionC, string optionD, string correctAnswer)
        {
            try
            {
                _logger.LogInformation("=== UpdateQuestion START ===");
                _logger.LogInformation("QuestionID: {Id}", id);

                var teacherId = GetCurrentUserId();
                var existingQuestion = await _context.Questions
                    .Include(q => q.Quiz).ThenInclude(qz => qz.Course)
                    .FirstOrDefaultAsync(q => q.QuestionID == id
                                              && q.Quiz != null
                                              && q.Quiz.Course != null
                                              && q.Quiz.Course.UserID == teacherId);

                if (existingQuestion == null)
                {
                    _logger.LogError("Question not found: {Id}", id);
                    return Json(new { success = false, message = "Không tìm thấy câu hỏi" });
                }

                // Validate
                if (string.IsNullOrWhiteSpace(questionText))
                {
                    return Json(new { success = false, message = "Vui lòng nhập nội dung câu hỏi" });
                }

                if (string.IsNullOrWhiteSpace(optionA) || string.IsNullOrWhiteSpace(optionB))
                {
                    return Json(new { success = false, message = "Vui lòng nhập ít nhất 2 đáp án (A và B)" });
                }

                if (string.IsNullOrWhiteSpace(correctAnswer))
                {
                    return Json(new { success = false, message = "Vui lòng chọn đáp án đúng" });
                }

                // Update
                existingQuestion.QuestionText = questionText.Trim();
                existingQuestion.OptionA = optionA.Trim();
                existingQuestion.OptionB = optionB.Trim();
                existingQuestion.OptionC = optionC?.Trim();
                existingQuestion.OptionD = optionD?.Trim();
                existingQuestion.CorrectAnswer = correctAnswer;

                _context.Questions.Update(existingQuestion);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Question updated successfully");
                _logger.LogInformation("=== UpdateQuestion SUCCESS ===");

                return Json(new { success = true, message = "Cập nhật câu hỏi thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== UpdateQuestion FAILED ===");
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // Xóa câu hỏi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var teacherId = GetCurrentUserId();
            var question = await _context.Questions
                .Include(q => q.Quiz).ThenInclude(qz => qz.Course)
                .FirstOrDefaultAsync(q => q.QuestionID == id
                                          && q.Quiz != null
                                          && q.Quiz.Course != null
                                          && q.Quiz.Course.UserID == teacherId);

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

        // Xóa quiz
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQuiz(int id)
        {
            try
            {
                _logger.LogInformation("=== DeleteQuiz START ===");
                _logger.LogInformation("QuizID: {Id}", id);

                var teacherId = GetCurrentUserId();

                if (string.IsNullOrEmpty(teacherId))
                {
                    _logger.LogError("TeacherID is null");
                    return Json(new { success = false, message = "Không thể xác định giáo viên." });
                }

                var quiz = await _context.Quizzes
                    .Include(q => q.Course)
                    .Include(q => q.Questions)
                    .Include(q => q.QuizResults)
                    .FirstOrDefaultAsync(q => q.QuizID == id);

                if (quiz == null)
                {
                    _logger.LogError("Quiz not found: {Id}", id);
                    return Json(new { success = false, message = "Không tìm thấy bài kiểm tra." });
                }

                // Check authorization
                if (quiz.Course == null || quiz.Course.UserID != teacherId)
                {
                    _logger.LogError("Unauthorized delete attempt by {TeacherId} for quiz {QuizId}", teacherId, id);
                    return Json(new { success = false, message = "Bạn không có quyền xóa bài kiểm tra này." });
                }

                int courseId = quiz.CourseID;
                string quizTitle = quiz.Title;

                // Check if there are quiz results
                int resultCount = quiz.QuizResults?.Count ?? 0;
                if (resultCount > 0)
                {
                    _logger.LogWarning("Quiz {Id} has {Count} results", id, resultCount);
                    // Still allow delete but inform user
                }

                // Delete quiz (cascade delete will handle Questions and QuizResults)
                _context.Quizzes.Remove(quiz);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Quiz deleted successfully: {Title} (ID: {Id})", quizTitle, id);
                _logger.LogInformation("=== DeleteQuiz SUCCESS ===");

                return Json(new
                {
                    success = true,
                    message = $"Đã xóa bài kiểm tra \"{quizTitle}\" thành công!",
                    courseId = courseId
                });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error while deleting quiz {Id}", id);

                // Check if it's a foreign key constraint error
                if (dbEx.InnerException != null && dbEx.InnerException.Message.Contains("REFERENCE constraint"))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Không thể xóa bài kiểm tra vì có dữ liệu liên quan. Vui lòng xóa kết quả kiểm tra trước."
                    });
                }

                return Json(new
                {
                    success = false,
                    message = "Lỗi cơ sở dữ liệu: " + dbEx.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== DeleteQuiz FAILED ===");
                return Json(new
                {
                    success = false,
                    message = "Có lỗi xảy ra: " + ex.Message
                });
            }
        }

        #endregion

        #region Lesson Management

        public async Task<IActionResult> CourseLessons(int courseId)
        {
            var teacherId = GetCurrentUserId();
            var course = await _context.Courses
                .Include(c => c.Lessons)
                .Include(c => c.Quizzes)
                .ThenInclude(q => q.Questions)
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
            var teacherId = GetCurrentUserId();
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseID == lesson.CourseID && c.UserID == teacherId);

            if (course == null)
            {
                TempData["Error"] = "Không tìm thấy khóa học.";
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Lessons.Add(lesson);
                    await _context.SaveChangesAsync();
                    await _notificationService.NotifyNewLessonAsync(
                        lessonId: lesson.LessonID,
                        courseId: lesson.CourseID
                    );
                    TempData["Success"] = "Tạo bài học thành công!";
                    return RedirectToAction("CourseLessons", new { courseId = lesson.CourseID });
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Lỗi khi tạo bài học: " + ex.Message;
                }
            }

            ViewBag.Course = course;
            return View(lesson);
        }

        public async Task<IActionResult> EditLesson(int id)
        {
            var teacherId = GetCurrentUserId();
            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.LessonID == id && l.Course != null && l.Course.UserID == teacherId);

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
            var teacherId = GetCurrentUserId();
            var existingLesson = await _context.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.LessonID == lesson.LessonID && l.Course != null && l.Course.UserID == teacherId);

            if (existingLesson == null)
            {
                TempData["Error"] = "Không tìm thấy bài học.";
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    existingLesson.TenBaiHoc = lesson.TenBaiHoc;
                    existingLesson.NoiDung = lesson.NoiDung;
                    existingLesson.VideoUrl = lesson.VideoUrl;
                    existingLesson.ThoiLuong = lesson.ThoiLuong;

                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật bài học thành công!";
                    return RedirectToAction("CourseLessons", new { courseId = existingLesson.CourseID });
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Lỗi khi cập nhật: " + ex.Message;
                }
            }

            ViewBag.Course = existingLesson.Course;
            return View(lesson);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLesson(int id)
        {
            var teacherId = GetCurrentUserId();
            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.LessonID == id && l.Course != null && l.Course.UserID == teacherId);

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

        #region Reports & Analytics

        [HttpGet]
        public async Task<IActionResult> TeacherReports(string period = "30d", string? start = null, string? end = null)
        {
            var teacherId = GetCurrentUserId();
            var now = DateTime.Now;

            // Xác định khoảng kỳ (PeriodStart, PeriodEnd exclusive)
            DateTime periodStart, periodEnd;
            string periodLabel;

            switch (period?.ToLowerInvariant())
            {
                case "month":
                    periodStart = new DateTime(now.Year, now.Month, 1);
                    periodEnd = periodStart.AddMonths(1);
                    periodLabel = "Tháng hiện tại";
                    break;
                case "prev-month":
                    periodEnd = new DateTime(now.Year, now.Month, 1);
                    periodStart = periodEnd.AddMonths(-1);
                    periodLabel = "Tháng trước";
                    break;
                case "range":
                    if (!DateTime.TryParse(start, out periodStart)) periodStart = now.Date.AddDays(-30);
                    if (!DateTime.TryParse(end, out periodEnd)) periodEnd = now;
                    // Chuẩn hóa end -> end of day
                    periodEnd = periodEnd.Date.AddDays(1);
                    periodLabel = $"{periodStart:dd/MM/yyyy} - {periodEnd.AddDays(-1):dd/MM/yyyy}";
                    break;
                case "30d":
                default:
                    periodEnd = now;
                    periodStart = now.AddDays(-30);
                    periodLabel = "30 ngày qua";
                    period = "30d";
                    break;
            }

            // Lấy courses của teacher
            var courses = await _context.Courses
                .Include(c => c.Enrollments)
                .Include(c => c.Payments)
                .Where(c => c.UserID == teacherId)
                .ToListAsync();

            // Course performance
            var coursePerformance = courses.Select(c =>
            {
                var activeEnrollments = c.Enrollments.Where(e => e.TrangThai == "Active").ToList();

                var totalStudents = activeEnrollments.Count;
                var totalRevenue = c.Payments.Where(p => p.Status == PaymentStatus.Success)
                                             .Sum(p => (decimal?)p.SoTien) ?? 0;

                var avgProgress = activeEnrollments.Any()
                    ? activeEnrollments.Average(e => (double)e.Progress)
                    : 0;

                var completionRateCourse = totalStudents > 0
                    ? activeEnrollments.Count(e => e.Progress >= 100) * 100.0 / totalStudents
                    : 0;

                var periodNewStudents = activeEnrollments.Count(e => e.EnrollDate >= periodStart && e.EnrollDate < periodEnd);
                var periodRevenue = c.Payments.Where(p => p.Status == PaymentStatus.Success
                                                       && p.NgayThanhToan >= periodStart
                                                       && p.NgayThanhToan < periodEnd)
                                              .Sum(p => (decimal?)p.SoTien) ?? 0;

                return new CoursePerformanceDto
                {
                    CourseID = c.CourseID,
                    TenKhoaHoc = c.TenKhoaHoc,
                    TrangThai = c.TrangThai,
                    TotalStudents = totalStudents,
                    TotalRevenue = totalRevenue,
                    AverageProgress = avgProgress,
                    CompletionRateCourse = completionRateCourse,
                    PeriodNewStudents = periodNewStudents,
                    PeriodRevenue = periodRevenue
                };
            })
            .OrderByDescending(c => c.TotalStudents)
            .ToList();

            // Monthly revenue 6 tháng
            var sixMonthsAgo = now.AddMonths(-6);
            var monthlyRevenue = await _context.Payments
                .Include(p => p.Course)
                .Where(p => p.Course.UserID == teacherId
                            && p.Status == PaymentStatus.Success
                            && p.NgayThanhToan >= sixMonthsAgo)
                .GroupBy(p => new { p.NgayThanhToan.Year, p.NgayThanhToan.Month })
                .Select(g => new MonthlyRevenueDto
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(p => p.SoTien),
                    Count = g.Count()
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            // Period revenue & new students
            var periodRevenueSum = await _context.Payments
                .Include(p => p.Course)
                .Where(p => p.Course.UserID == teacherId
                            && p.Status == PaymentStatus.Success
                            && p.NgayThanhToan >= periodStart
                            && p.NgayThanhToan < periodEnd)
                .SumAsync(p => (decimal?)p.SoTien) ?? 0;

            var periodNewStudentsSum = await _context.Enrollments
                .Include(e => e.Course)
                .Where(e => e.Course.UserID == teacherId
                            && e.TrangThai == "Active"
                            && e.EnrollDate >= periodStart
                            && e.EnrollDate < periodEnd)
                .CountAsync();

            // Global completion
            var totalActiveEnrollments = await _context.Enrollments
                .Include(e => e.Course)
                .CountAsync(e => e.Course.UserID == teacherId && e.TrangThai == "Active");

            var completedActiveEnrollments = await _context.Enrollments
                .Include(e => e.Course)
                .CountAsync(e => e.Course.UserID == teacherId && e.TrangThai == "Active" && e.Progress >= 100);

            var globalCompletionRate = totalActiveEnrollments > 0
                ? completedActiveEnrollments * 100.0 / totalActiveEnrollments
                : 0;

            // Quiz rating
            var quizResults = await _context.QuizResults
                .Include(qr => qr.Quiz).ThenInclude(q => q.Course)
                .Where(qr => qr.Quiz.Course.UserID == teacherId)
                .ToListAsync();

            double averageQuizScore = quizResults.Any() ? quizResults.Average(qr => qr.Score) : 0;
            double averageRating = Math.Round(averageQuizScore / 20.0, 1);

            // Weekly analytics (4 tuần tính từ hiện tại)
            var fourWeeksAgo = now.Date.AddDays(-28);
            var weeklyRaw = await _context.Enrollments
                .Include(e => e.Course)
                .Where(e => e.Course.UserID == teacherId && e.EnrollDate >= fourWeeksAgo)
                .Select(e => new
                {
                    WeekStart = e.EnrollDate.Date.AddDays(-(int)e.EnrollDate.DayOfWeek),
                    Completed = e.Progress >= 100
                })
                .ToListAsync();

            var weeklyAnalytics = weeklyRaw
                .GroupBy(x => x.WeekStart)
                .OrderByDescending(g => g.Key)
                .Take(4)
                .OrderBy(g => g.Key)
                .Select((g, idx) => new WeeklyStudentAnalyticsDto
                {
                    Label = $"Tuần {idx + 1}",
                    NewStudents = g.Count(),
                    CompletedStudents = g.Count(x => x.Completed)
                })
                .ToList();

            // Category distribution (lifetime)
            var categoryDistribution = await _context.CourseCategories
                .Include(cc => cc.Course).ThenInclude(c => c.Enrollments)
                .Include(cc => cc.Course).ThenInclude(c => c.Payments)
                .Include(cc => cc.Category)
                .Where(cc => cc.Course.UserID == teacherId)
                .GroupBy(cc => cc.Category.Name)
                .Select(g => new CategoryDistributionDto
                {
                    CategoryName = g.Key,
                    CourseCount = g.Select(x => x.CourseID).Distinct().Count(),
                    StudentCount = g.Sum(x => x.Course.Enrollments.Count(e => e.TrangThai == "Active")),
                    Revenue = g.Sum(x => x.Course.Payments
                        .Where(p => p.Status == PaymentStatus.Success)
                        .Sum(p => (decimal?)p.SoTien) ?? 0)
                })
                .OrderByDescending(x => x.StudentCount)
                .ToListAsync();

            var vm = new TeacherReportViewModel
            {
                Summary = new TeacherReportSummaryDto
                {
                    PeriodKey = period,
                    PeriodLabel = periodLabel,
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd,
                    PeriodRevenue = periodRevenueSum,
                    PeriodNewStudents = periodNewStudentsSum,
                    AverageQuizScore = Math.Round(averageQuizScore, 2),
                    AverageRating = averageRating,
                    GlobalCompletionRate = Math.Round(globalCompletionRate, 2),
                    TotalActiveStudents = totalActiveEnrollments,
                    TotalCourses = courses.Count
                },
                CoursePerformance = coursePerformance,
                MonthlyRevenue = monthlyRevenue,
                WeeklyAnalytics = weeklyAnalytics,
                CategoryDistribution = categoryDistribution
            };

            ViewBag.Period = period; // để view đặt selected cho dropdown
            return View(vm);
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

    // ViewModel cho Question khi submit từ form
    public class QuestionViewModel
    {
        public string QuestionText { get; set; }
        public string OptionA { get; set; }
        public string OptionB { get; set; }
        public string OptionC { get; set; }
        public string OptionD { get; set; }
        public string CorrectAnswer { get; set; }
    }
}