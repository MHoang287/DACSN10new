using Microsoft.AspNetCore.Mvc;
using DACSN10.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace DACSN10.Controllers
{
    [Authorize]
    public class ExamController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ExamController> _logger;

        public ExamController(AppDbContext context, ILogger<ExamController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int courseId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("UserID is null");
                TempData["Error"] = "Không thể xác định người dùng.";
                return RedirectToAction("Index", "Course");
            }

            var isEnrolled = await _context.Enrollments
                .AnyAsync(e => e.CourseID == courseId && e.UserID == userId && e.TrangThai == "Active");

            if (!isEnrolled)
            {
                TempData["Error"] = "Bạn chưa đăng ký khóa học này.";
                return RedirectToAction("Details", "Course", new { id = courseId });
            }

            var course = await _context.Courses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CourseID == courseId);

            if (course == null)
            {
                TempData["Error"] = "Không tìm thấy khóa học.";
                return RedirectToAction("Index", "Course");
            }

            var quizzes = await _context.Quizzes
                .AsNoTracking()
                .Include(q => q.Questions)
                .Where(q => q.CourseID == courseId)
                .ToListAsync();

            // Get user's quiz results
            var userResults = await _context.QuizResults
                .AsNoTracking()
                .Where(qr => qr.UserID == userId)
                .Select(qr => new { qr.QuizID, qr.Score, qr.TakenAt })
                .ToListAsync();

            ViewBag.UserResults = userResults;
            ViewBag.Course = course;
            return View(quizzes);
        }

        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("UserID is null in Details");
                TempData["Error"] = "Không thể xác định người dùng.";
                return RedirectToAction("Index", "Course");
            }

            var quiz = await _context.Quizzes
                .AsNoTracking()
                .Include(q => q.Questions)
                .Include(q => q.Course)
                .FirstOrDefaultAsync(q => q.QuizID == id);

            if (quiz == null)
            {
                _logger.LogError($"Quiz not found: {id}");
                TempData["Error"] = "Không tìm thấy bài kiểm tra.";
                return NotFound();
            }

            var isEnrolled = await _context.Enrollments
                .AnyAsync(e => e.CourseID == quiz.CourseID && e.UserID == userId && e.TrangThai == "Active");

            if (!isEnrolled)
            {
                TempData["Error"] = "Bạn chưa đăng ký khóa học này.";
                return RedirectToAction("Details", "Course", new { id = quiz.CourseID });
            }

            // Check if user has already taken this quiz
            var existingResult = await _context.QuizResults
                .AsNoTracking()
                .FirstOrDefaultAsync(qr => qr.UserID == userId && qr.QuizID == id);

            var hasSubmitted = existingResult != null;
            ViewBag.HasSubmitted = hasSubmitted;

            if (hasSubmitted)
            {
                ViewBag.UserScore = existingResult.Score;
                ViewBag.TakenAt = existingResult.TakenAt;
                ViewBag.ResultId = existingResult.ResultID;
            }

            return View(quiz);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int quizId, Dictionary<int, string> answers)
        {
            try
            {
                _logger.LogInformation($"Submit started - QuizID: {quizId}");

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogError("UserID is null in Submit");
                    TempData["Error"] = "Không thể xác định người dùng. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Details", new { id = quizId });
                }

                _logger.LogInformation($"UserID: {userId}");

                var quiz = await _context.Quizzes
                    .Include(q => q.Questions)
                    .Include(q => q.Course)
                    .FirstOrDefaultAsync(q => q.QuizID == quizId);

                if (quiz == null)
                {
                    _logger.LogError($"Quiz not found: {quizId}");
                    TempData["Error"] = "Không tìm thấy bài kiểm tra.";
                    return NotFound();
                }

                _logger.LogInformation($"Quiz found: {quiz.Title}, Questions: {quiz.Questions?.Count ?? 0}");

                var isEnrolled = await _context.Enrollments
                    .AnyAsync(e => e.CourseID == quiz.CourseID && e.UserID == userId && e.TrangThai == "Active");

                if (!isEnrolled)
                {
                    _logger.LogWarning($"User {userId} not enrolled in course {quiz.CourseID}");
                    TempData["Error"] = "Bạn chưa đăng ký khóa học này.";
                    return RedirectToAction("Details", "Course", new { id = quiz.CourseID });
                }

                var hasSubmitted = await _context.QuizResults
                    .AnyAsync(qr => qr.UserID == userId && qr.QuizID == quizId);

                if (hasSubmitted)
                {
                    _logger.LogWarning($"User {userId} already submitted quiz {quizId}");
                    TempData["Error"] = "Bạn đã nộp bài kiểm tra này rồi.";
                    return RedirectToAction("Details", new { id = quizId });
                }

                // Log answers received
                _logger.LogInformation($"Answers received: {answers?.Count ?? 0}");
                if (answers != null)
                {
                    foreach (var ans in answers)
                    {
                        _logger.LogInformation($"QuestionID: {ans.Key}, Answer: {ans.Value}");
                    }
                }

                if (quiz.Questions == null || !quiz.Questions.Any())
                {
                    _logger.LogError($"Quiz {quizId} has no questions");
                    TempData["Error"] = "Bài kiểm tra không có câu hỏi.";
                    return RedirectToAction("Details", new { id = quizId });
                }

                if (answers == null || answers.Count == 0)
                {
                    _logger.LogWarning("No answers provided");
                    TempData["Error"] = "Vui lòng trả lời ít nhất một câu hỏi.";
                    return RedirectToAction("Details", new { id = quizId });
                }

                // Calculate score
                int correct = 0;
                foreach (var question in quiz.Questions)
                {
                    if (answers.TryGetValue(question.QuestionID, out string userAnswer))
                    {
                        if (question.CorrectAnswer == userAnswer)
                        {
                            correct++;
                            _logger.LogInformation($"Question {question.QuestionID}: Correct");
                        }
                        else
                        {
                            _logger.LogInformation($"Question {question.QuestionID}: Wrong (User: {userAnswer}, Correct: {question.CorrectAnswer})");
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Question {question.QuestionID}: No answer provided");
                    }
                }

                var score = quiz.Questions.Count > 0 ? (double)correct / quiz.Questions.Count * 100 : 0;
                _logger.LogInformation($"Score calculated: {correct}/{quiz.Questions.Count} = {score}%");

                // FIX: Use execution strategy for transaction
                var strategy = _context.Database.CreateExecutionStrategy();

                QuizResult quizResult = null;

                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        quizResult = new QuizResult
                        {
                            UserID = userId,
                            QuizID = quizId,
                            Score = score,
                            TakenAt = DateTime.Now
                        };

                        _logger.LogInformation($"Creating QuizResult: UserID={userId}, QuizID={quizId}, Score={score}");

                        _context.QuizResults.Add(quizResult);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation($"QuizResult saved with ID: {quizResult.ResultID}");

                        await transaction.CommitAsync();
                        _logger.LogInformation("Transaction committed successfully");
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError($"Error saving to database: {ex.Message}");
                        _logger.LogError($"Stack trace: {ex.StackTrace}");

                        if (ex.InnerException != null)
                        {
                            _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                        }

                        throw; // Re-throw to be caught by outer try-catch
                    }
                });

                TempData["Success"] = $"Hoàn thành bài kiểm tra! Điểm của bạn: {score:F1}/100";
                return RedirectToAction("Result", new { resultId = quizResult.ResultID });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error in Submit: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                TempData["Error"] = $"Có lỗi xảy ra: {ex.Message}";
                return RedirectToAction("Details", new { id = quizId });
            }
        }

        public async Task<IActionResult> Result(int resultId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Không thể xác định người dùng.";
                return RedirectToAction("Index", "Course");
            }

            var result = await _context.QuizResults
                .AsNoTracking()
                .Include(qr => qr.Quiz).ThenInclude(q => q.Course)
                .Include(qr => qr.Quiz).ThenInclude(q => q.Questions)
                .FirstOrDefaultAsync(qr => qr.ResultID == resultId && qr.UserID == userId);

            if (result == null)
            {
                _logger.LogError($"Result not found: {resultId} for user {userId}");
                TempData["Error"] = "Không tìm thấy kết quả bài kiểm tra.";
                return RedirectToAction("Index", "Course");
            }

            return View(result);
        }

        public async Task<IActionResult> MyResults()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Không thể xác định người dùng.";
                return RedirectToAction("Index", "Course");
            }

            var results = await _context.QuizResults
                .AsNoTracking()
                .Include(qr => qr.Quiz).ThenInclude(q => q.Course)
                .Where(qr => qr.UserID == userId)
                .OrderByDescending(qr => qr.TakenAt)
                .ToListAsync();

            return View(results);
        }
    }
}