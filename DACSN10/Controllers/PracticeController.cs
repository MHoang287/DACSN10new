using Microsoft.AspNetCore.Mvc;
using DACSN10.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace DACSN10.Controllers
{
    [Authorize]
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

            var assignments = await _context.Assignments
                .AsNoTracking()
                .Where(a => a.CourseID == courseId)
                .OrderBy(a => a.HanNop)
                .ToListAsync();

            // Get user's submissions for these assignments
            var userSubmissions = await _context.Submissions
                .AsNoTracking()
                .Where(s => s.UserID == userId && assignments.Select(a => a.AssignmentID).Contains(s.AssignmentID))
                .Select(s => new { s.AssignmentID, s.NgayNop, s.Diem })
                .ToListAsync();

            ViewBag.UserSubmissions = userSubmissions;
            ViewBag.Course = course;
            return View(assignments);
        }

        // Chi tiết bài tập
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var assignment = await _context.Assignments
                .AsNoTracking()
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.AssignmentID == id);

            if (assignment == null)
            {
                TempData["Error"] = "Không tìm thấy bài tập.";
                return NotFound();
            }

            // Kiểm tra người dùng đã đăng ký khóa học
            var isEnrolled = await _context.Enrollments
                .AnyAsync(e => e.CourseID == assignment.CourseID && e.UserID == userId && e.TrangThai == "Active");

            if (!isEnrolled)
            {
                TempData["Error"] = "Bạn chưa đăng ký khóa học này.";
                return RedirectToAction("Details", "Course", new { id = assignment.CourseID });
            }

            // Check if user has submitted
            var submission = await _context.Submissions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.AssignmentID == id && s.UserID == userId);

            ViewBag.UserSubmission = submission;
            ViewBag.HasSubmitted = submission != null;
            ViewBag.IsOverdue = assignment.HanNop < DateTime.Now;

            return View(assignment);
        }

        // Nộp bài tập
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int assignmentId, IFormFile file)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.AssignmentID == assignmentId);

            if (assignment == null)
            {
                TempData["Error"] = "Không tìm thấy bài tập.";
                return NotFound();
            }

            // Kiểm tra người dùng đã đăng ký khóa học
            var isEnrolled = await _context.Enrollments
                .AnyAsync(e => e.CourseID == assignment.CourseID && e.UserID == userId && e.TrangThai == "Active");

            if (!isEnrolled)
            {
                TempData["Error"] = "Bạn chưa đăng ký khóa học này.";
                return RedirectToAction("Details", new { id = assignmentId });
            }

            // Check if already submitted
            var existingSubmission = await _context.Submissions
                .FirstOrDefaultAsync(s => s.AssignmentID == assignmentId && s.UserID == userId);

            if (existingSubmission != null)
            {
                TempData["Error"] = "Bạn đã nộp bài tập này rồi.";
                return RedirectToAction("Details", new { id = assignmentId });
            }

            // Check deadline
            if (assignment.HanNop < DateTime.Now)
            {
                TempData["Error"] = "Đã quá hạn nộp bài tập.";
                return RedirectToAction("Details", new { id = assignmentId });
            }

            // Kiểm tra file
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn file để nộp.";
                return RedirectToAction("Details", new { id = assignmentId });
            }

            // Kiểm tra định dạng file
            var allowedExtensions = new[] { ".pdf", ".docx", ".doc", ".zip", ".rar", ".txt" };
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
            {
                TempData["Error"] = "Chỉ chấp nhận file PDF, DOC, DOCX, ZIP, RAR hoặc TXT.";
                return RedirectToAction("Details", new { id = assignmentId });
            }

            // Kiểm tra kích thước file (tối đa 10MB)
            if (file.Length > 10 * 1024 * 1024)
            {
                TempData["Error"] = "File không được vượt quá 10MB.";
                return RedirectToAction("Details", new { id = assignmentId });
            }

            try
            {
                // Lưu file
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "assignments");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = $"{userId}_{assignmentId}_{Guid.NewGuid()}{extension}";
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
                    FileNop = $"/uploads/assignments/{fileName}",
                    NgayNop = DateTime.Now,
                    Diem = null // Will be graded by teacher later
                };

                _context.Submissions.Add(submission);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Nộp bài tập thành công! Chờ giảng viên chấm điểm.";
                return RedirectToAction("MySubmissions");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi nộp bài tập. Vui lòng thử lại.";
                return RedirectToAction("Details", new { id = assignmentId });
            }
        }

        // Xem danh sách bài đã nộp của user
        public async Task<IActionResult> MySubmissions()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var submissions = await _context.Submissions
                .AsNoTracking()
                .Include(s => s.Assignment).ThenInclude(a => a.Course)
                .Where(s => s.UserID == userId)
                .OrderByDescending(s => s.NgayNop)
                .ToListAsync();

            return View(submissions);
        }

        // Tải file bài nộp
        public async Task<IActionResult> DownloadSubmission(int submissionId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var submission = await _context.Submissions
                .Include(s => s.Assignment)
                .FirstOrDefaultAsync(s => s.SubmissionID == submissionId && s.UserID == userId);

            if (submission == null)
            {
                TempData["Error"] = "Không tìm thấy bài nộp.";
                return NotFound();
            }

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", submission.FileNop.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
            {
                TempData["Error"] = "File không tồn tại.";
                return NotFound();
            }

            var fileName = Path.GetFileName(submission.FileNop);
            var contentType = "application/octet-stream";
            return PhysicalFile(filePath, contentType, fileName);
        }

        // Xóa bài nộp (chỉ khi chưa chấm điểm và chưa quá hạn)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSubmission(int submissionId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var submission = await _context.Submissions
                .Include(s => s.Assignment)
                .FirstOrDefaultAsync(s => s.SubmissionID == submissionId && s.UserID == userId);

            if (submission == null)
            {
                TempData["Error"] = "Không tìm thấy bài nộp.";
                return RedirectToAction("MySubmissions");
            }

            // Chỉ cho phép xóa nếu chưa chấm điểm và chưa quá hạn
            if (submission.Diem.HasValue)
            {
                TempData["Error"] = "Không thể xóa bài đã được chấm điểm.";
                return RedirectToAction("MySubmissions");
            }

            if (submission.Assignment.HanNop < DateTime.Now)
            {
                TempData["Error"] = "Không thể xóa bài nộp sau khi quá hạn.";
                return RedirectToAction("MySubmissions");
            }

            try
            {
                // Xóa file
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", submission.FileNop.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                // Xóa record
                _context.Submissions.Remove(submission);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Xóa bài nộp thành công!";
            }
            catch
            {
                TempData["Error"] = "Lỗi khi xóa bài nộp.";
            }

            return RedirectToAction("MySubmissions");
        }
    }
}