using Microsoft.AspNetCore.Mvc;
using DACSN10.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace DACSN10.Controllers
{
    public class PracticeController : Controller
    {
        private readonly AppDbContext _context;

        public PracticeController(AppDbContext context)
        {
            _context = context;
        }

        // Danh sách bài tập thực hành trong khóa học
        public IActionResult Index(int courseId)
        {
            var assignments = _context.Assignments
                                      .Where(p => p.CourseID == courseId)
                                      .ToList();
            return View(assignments);
        }

        // Chi tiết bài tập
        public IActionResult Details(int id)
        {
            var assignment = _context.Assignments.FirstOrDefault(p => p.AssignmentID == id);
            if (assignment == null) return NotFound();
            return View(assignment);
        }

        // Nộp bài tập
        [HttpPost]
        public IActionResult Submit(int assignmentId, string answer)
        {
            var userId = User.Identity.Name;
            var submission = new Submission
            {
                AssignmentID = assignmentId,
                UserID = userId,
                FileNop = answer,
                NgayNop = DateTime.Now
            };
            _context.Submissions.Add(submission);
            _context.SaveChanges();

            return RedirectToAction("MySubmissions");
        }

        // Xem danh sách bài đã nộp của user
        public IActionResult MySubmissions()
        {
            var userId = User.Identity.Name;
            var submissions = _context.Submissions
                                      .Include(s => s.Assignment)
                                      .Where(s => s.UserID == userId)
                                      .ToList();
            return View(submissions);
        }
    }
}
