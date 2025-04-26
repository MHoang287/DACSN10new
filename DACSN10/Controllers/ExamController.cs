using Microsoft.AspNetCore.Mvc;
using DACSN10.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DACSN10.Controllers
{
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
                return Unauthorized();
            }

            var quizzes = await _context.Quizzes
                .AsNoTracking()
                .Where(q => q.CourseID == courseId)
                .ToListAsync();
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
                return Unauthorized();
            }

            return View(quiz);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int quizId, List<string> answers)
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
                return Unauthorized();
            }

            var hasSubmitted = await _context.QuizResults
                .AnyAsync(qr => qr.UserID == userId && qr.QuizID == quizId);
            if (hasSubmitted)
            {
                TempData["Error"] = "Bạn đã nộp bài kiểm tra này.";
                return View("Result");
            }

            if (answers.Count != quiz.Questions.Count)
            {
                TempData["Error"] = "Số câu trả lời không hợp lệ.";
                return View("Result");
            }

            int correct = 0;
            for (int i = 0; i < quiz.Questions.Count; i++)
            {
                if (quiz.Questions.ElementAt(i).CorrectAnswer == answers[i])
                    correct++;
            }

            var score = (double)correct / quiz.Questions.Count * 100;

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
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Lỗi khi lưu kết quả bài kiểm tra.";
                return View("Result");
            }

            ViewBag.Score = correct;
            ViewBag.Total = quiz.Questions.Count;
            ViewBag.Percentage = score;

            return View("Result");
        }
    }
}