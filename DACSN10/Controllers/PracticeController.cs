using Microsoft.AspNetCore.Mvc;
using DACSN10.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Http;

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
        public async Task<IActionResult> Index(int courseId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["Error"] = "Vui lòng đăng nhập để xem bài tập.";
                return RedirectToAction("Login", "Account");
            }

            // Kiểm tra người dùng đã đăng ký khóa học
            var userId = User.Identity.Name;
            var isEnrolled = await _context.Enrollments
                .AnyAsync(e => e.CourseID == courseId && e.UserID == userId && e.TrangThai == "Active");
            if (!isEnrolled)
            {
                ViewBag.Error = "Bạn chưa đăng ký khóa học này.";
                return View(new List<Assignment>());
            }

            var assignments = await _context.Assignments
                .Where(p => p.CourseID == courseId)
                .ToListAsync();
            return View(assignments);
        }

        // Chi tiết bài tập
        public async Task<IActionResult> Details(int id)
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["Error"] = "Vui lòng đăng nhập để xem bài tập.";
                return RedirectToAction("Login", "Account");
            }

            var assignment = await _context.Assignments
                .FirstOrDefaultAsync(p => p.AssignmentID == id);
            if (assignment == null)
            {
                TempData["Error"] = "Không tìm thấy bài tập.";
                return NotFound();
            }

            // Kiểm tra người dùng đã đăng ký khóa học
            var userId = User.Identity.Name;
            var isEnrolled = await _context.Enrollments
                .AnyAsync(e => e.CourseID == assignment.CourseID && e.UserID == userId && e.TrangThai == "Active");
            if (!isEnrolled)
            {
                TempData["Error"] = "Bạn chưa đăng ký khóa học này.";
                return RedirectToAction("Index", new { courseId = assignment.CourseID });
            }

            return View(assignment);
        }

        // Nộp bài tập
        [HttpPost]
        public async Task<IActionResult> Submit(int assignmentId, IFormFile file)
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["Error"] = "Vui lòng đăng nhập để nộp bài tập.";
                return RedirectToAction("Login", "Account");
            }

            var userId = User.Identity.Name;
            var assignment = await _context.Assignments
                .FirstOrDefaultAsync(a => a.AssignmentID == assignmentId);
            if (assignment == null)
            {
                TempData["Error"] = "Không tìm thấy bài tập.";
                return RedirectToAction("Index");
            }

            // Kiểm tra người dùng đã đăng ký khóa học
            var isEnrolled = await _context.Enrollments
                .AnyAsync(e => e.CourseID == assignment.CourseID && e.UserID == userId && e.TrangThai == "Active");
            if (!isEnrolled)
            {
                TempData["Error"] = "Bạn chưa đăng ký khóa học này.";
                return RedirectToAction("Details", new { id = assignmentId });
            }

            // Kiểm tra file
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn file để nộp.";
                return RedirectToAction("Details", new { id = assignmentId });
            }

            // Kiểm tra định dạng file
            var allowedExtensions = new[] { ".pdf", ".docx", ".zip" };
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
            {
                TempData["Error"] = "Chỉ chấp nhận file PDF, DOCX hoặc ZIP.";
                return RedirectToAction("Details", new { id = assignmentId });
            }

            // Kiểm tra kích thước file (ví dụ: tối đa 10MB)
            if (file.Length > 10 * 1024 * 1024)
            {
                TempData["Error"] = "File không được vượt quá 10MB.";
                return RedirectToAction("Details", new { id = assignmentId });
            }

            // Lưu file (giả sử lưu vào thư mục wwwroot/uploads)
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Lưu submission
            var submission = new Submission
            {
                AssignmentID = assignmentId,
                UserID = userId,
                FileNop = $"/uploads/{fileName}",
                NgayNop = DateTime.Now
            };
            _context.Submissions.Add(submission);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Nộp bài tập thành công!";
            return RedirectToAction("MySubmissions");
        }

        // Xem danh sách bài đã nộp của user
        public async Task<IActionResult> MySubmissions()
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["Error"] = "Vui lòng đăng nhập để xem bài đã nộp.";
                return RedirectToAction("Login", "Account");
            }

            var userId = User.Identity.Name;
            var submissions = await _context.Submissions
                .Include(s => s.Assignment)
                .Where(s => s.UserID == userId)
                .ToListAsync();
            return View(submissions);
        }
    }
}