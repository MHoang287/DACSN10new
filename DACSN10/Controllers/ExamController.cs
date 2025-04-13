using Microsoft.AspNetCore.Mvc;
using DACSN10.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace DACSN10.Controllers
{
    public class ExamController : Controller
    {
        private readonly AppDbContext _context;

        public ExamController(AppDbContext context)
        {
            _context = context;
        }

        // Danh sách bài kiểm tra trong khóa học
        public IActionResult Index(int courseId)
        {
            var quizzes = _context.Quizzes
                                  .Where(q => q.CourseID == courseId)
                                  .ToList();
            return View(quizzes);
        }

        // Chi tiết bài kiểm tra
        public IActionResult Details(int id)
        {
            var quiz = _context.Quizzes
                               .Include(q => q.Questions)
                               .FirstOrDefault(q => q.QuizID == id); // 👈 sửa tại đây
            if (quiz == null) return NotFound();
            return View(quiz);
        }

        // Nộp bài kiểm tra
        [HttpPost]
        public IActionResult Submit(int quizId, List<string> answers)
        {
            var quiz = _context.Quizzes
                               .Include(q => q.Questions)
                               .FirstOrDefault(q => q.QuizID == quizId); // 👈 sửa tại đây
            if (quiz == null) return NotFound();

            int correct = 0;
            for (int i = 0; i < quiz.Questions.Count; i++)
            {
                if (i < answers.Count && quiz.Questions.ElementAt(i).CorrectAnswer == answers[i])
                    correct++;
            }

            ViewBag.Score = correct;
            ViewBag.Total = quiz.Questions.Count;

            return View("Result");
        }
    }
}
