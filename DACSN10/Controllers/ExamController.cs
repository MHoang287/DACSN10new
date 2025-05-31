using Microsoft.AspNetCore.Mvc;
using DACSN10.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace DACSN10.Controllers
{
    [Authorize]
    public class ExamController : Controller
    {
        private readonly AppDbContext _context;

        public ExamController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int courseId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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
            var quiz = await _context.Quizzes
                .AsNoTracking()
                .Include(q => q.Questions)
                .Include(q => q.Course)
                .FirstOrDefaultAsync(q => q.QuizID == id);

            if (quiz == null)
            {
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
            var hasSubmitted = await _context.QuizResults
                .AnyAsync(qr => qr.UserID == userId && qr.QuizID == id);

            ViewBag.HasSubmitted = hasSubmitted;

            if (hasSubmitted)
            {
                var result = await _context.QuizResults
                    .AsNoTracking()
                    .FirstOrDefaultAsync(qr => qr.UserID == userId && qr.QuizID == id);
                ViewBag.UserScore = result?.Score;
                ViewBag.TakenAt = result?.TakenAt;
            }

            return View(quiz);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int quizId, Dictionary<int, string> answers)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .Include(q => q.Course)
                .FirstOrDefaultAsync(q => q.QuizID == quizId);

            if (quiz == null)
            {
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

            var hasSubmitted = await _context.QuizResults
                .AnyAsync(qr => qr.UserID == userId && qr.QuizID == quizId);

            if (hasSubmitted)
            {
                TempData["Error"] = "Bạn đã nộp bài kiểm tra này rồi.";
                return RedirectToAction("Details", new { id = quizId });
            }

            if (answers == null || answers.Count != quiz.Questions.Count)
            {
                TempData["Error"] = "Vui lòng trả lời tất cả các câu hỏi.";
                return RedirectToAction("Details", new { id = quizId });
            }

            // Calculate score
            int correct = 0;
            foreach (var question in quiz.Questions)
            {
                if (answers.TryGetValue(question.QuestionID, out string userAnswer) &&
                    question.CorrectAnswer == userAnswer)
                {
                    correct++;
                }
            }

            var score = quiz.Questions.Count > 0 ? (double)correct / quiz.Questions.Count * 100 : 0;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var quizResult = new QuizResult
                {
                    UserID = userId,
                    QuizID = quizId,
                    Score = score,
                    TakenAt = DateTime.Now
                };

                _context.QuizResults.Add(quizResult);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = $"Hoàn thành bài kiểm tra! Điểm của bạn: {score:F1}/100";
                return RedirectToAction("Result", new { resultId = quizResult.ResultID });
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Lỗi khi lưu kết quả bài kiểm tra.";
                return RedirectToAction("Details", new { id = quizId });
            }
        }

        public async Task<IActionResult> Result(int resultId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _context.QuizResults
                .AsNoTracking()
                .Include(qr => qr.Quiz).ThenInclude(q => q.Course)
                .Include(qr => qr.Quiz).ThenInclude(q => q.Questions)
                .FirstOrDefaultAsync(qr => qr.ResultID == resultId && qr.UserID == userId);

            if (result == null)
            {
                TempData["Error"] = "Không tìm thấy kết quả bài kiểm tra.";
                return RedirectToAction("Index", "Course");
            }

            return View(result);
        }

        public async Task<IActionResult> MyResults()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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