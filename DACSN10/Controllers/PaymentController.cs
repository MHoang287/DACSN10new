using DACSN10.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Net;

namespace DACSN10.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;

        public PaymentController(AppDbContext context, UserManager<User> userManager, IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
        }

        // GET: Payment/Create/5 (CourseID)
        public async Task<IActionResult> Create(int courseId)
        {
            var course = await _context.Courses
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CourseID == courseId && c.TrangThai == "Active");

            if (course == null)
            {
                TempData["Error"] = "Khóa học không tồn tại hoặc không hoạt động.";
                return NotFound();
            }

            // Kiểm tra khóa học có phải miễn phí
            if (course.Gia <= 0)
            {
                TempData["Error"] = "Khóa học này là miễn phí, không cần thanh toán.";
                return RedirectToAction("Details", "Course", new { id = courseId });
            }

            var currentUser = await _userManager.GetUserAsync(User);

            // Kiểm tra xem user đã thanh toán khóa học này chưa
            var existingPayment = await _context.Payments
                .Where(p => p.UserID == currentUser.Id && p.CourseID == courseId &&
                           (p.Status == PaymentStatus.Success || p.Status == PaymentStatus.Pending))
                .FirstOrDefaultAsync();

            if (existingPayment != null)
            {
                if (existingPayment.Status == PaymentStatus.Success)
                {
                    TempData["Error"] = "Bạn đã thanh toán thành công khóa học này!";
                }
                else
                {
                    TempData["Info"] = "Bạn có một giao dịch đang chờ xử lý cho khóa học này.";
                }
                return RedirectToAction("Details", "Course", new { id = courseId });
            }

            // Kiểm tra đã đăng ký chưa
            var isEnrolled = await _context.Enrollments
                .AnyAsync(e => e.UserID == currentUser.Id && e.CourseID == courseId && e.TrangThai == "Active");

            if (isEnrolled)
            {
                TempData["Error"] = "Bạn đã đăng ký khóa học này rồi!";
                return RedirectToAction("Details", "Course", new { id = courseId });
            }

            var viewModel = new PaymentViewModel
            {
                CourseID = course.CourseID,
                CourseName = course.TenKhoaHoc,
                CoursePrice = course.Gia,
                TeacherName = course.User.HoTen,
                UserEmail = currentUser.Email,
                UserName = currentUser.HoTen
            };

            return View(viewModel);
        }

        // POST: Payment/ProcessPayment - Tạo giao dịch và redirect đến MoMo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(int courseId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var course = await _context.Courses
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CourseID == courseId && c.TrangThai == "Active");

            if (course == null)
            {
                TempData["Error"] = "Khóa học không tồn tại.";
                return NotFound();
            }

            // Kiểm tra lại các điều kiện
            if (course.Gia <= 0)
            {
                TempData["Error"] = "Khóa học này là miễn phí.";
                return RedirectToAction("Details", "Course", new { id = courseId });
            }

            var existingPayment = await _context.Payments
                .Where(p => p.UserID == currentUser.Id && p.CourseID == courseId &&
                           (p.Status == PaymentStatus.Success || p.Status == PaymentStatus.Pending))
                .FirstOrDefaultAsync();

            if (existingPayment != null)
            {
                TempData["Error"] = "Bạn đã có giao dịch cho khóa học này.";
                return RedirectToAction("Details", "Course", new { id = courseId });
            }

            try
            {
                // Tạo payment record với trạng thái Pending
                var payment = new Payment
                {
                    UserID = currentUser.Id,
                    CourseID = courseId,
                    SoTien = course.Gia,
                    NgayThanhToan = DateTime.Now,
                    PhuongThucThanhToan = "MoMo",
                    Status = PaymentStatus.Pending
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                // Gửi email hóa đơn
                await SendInvoiceEmail(currentUser, course, payment);

                // Lưu PaymentID vào TempData để sử dụng sau khi quay về
                TempData["PaymentId"] = payment.PaymentID;
                TempData["CourseId"] = courseId;

                // Redirect đến MoMo
                return Redirect("https://me.momo.vn/xoanws");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra khi tạo giao dịch. Vui lòng thử lại.";
                return RedirectToAction("Create", new { courseId });
            }
        }

        // GET: Payment/PaymentReturn - Trang thông báo chờ duyệt
        public async Task<IActionResult> PaymentReturn()
        {
            var paymentId = TempData["PaymentId"];
            var courseId = TempData["CourseId"];

            if (paymentId == null || courseId == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin giao dịch.";
                return RedirectToAction("Index", "Home");
            }

            var payment = await _context.Payments
                .Include(p => p.Course)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PaymentID == (int)paymentId);

            if (payment == null)
            {
                TempData["Error"] = "Không tìm thấy giao dịch.";
                return RedirectToAction("Index", "Home");
            }

            return View(payment);
        }

        // GET: Payment/History
        public async Task<IActionResult> History(string status = "", int page = 1, int pageSize = 10)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var query = _context.Payments
                .Include(p => p.Course)
                .Where(p => p.UserID == currentUser.Id);

            // Lọc theo trạng thái nếu có
            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<PaymentStatus>(status, true, out var statusEnum))
                {
                    query = query.Where(p => p.Status == statusEnum);
                }
            }

            var totalPayments = await query.CountAsync();

            var payments = await query
                .OrderByDescending(p => p.NgayThanhToan)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Thống kê
            var stats = await _context.Payments
                .Where(p => p.UserID == currentUser.Id)
                .GroupBy(p => p.Status)
                .Select(g => new { Status = g.Key, Count = g.Count(), Total = g.Sum(p => p.SoTien) })
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalPayments / pageSize);
            ViewBag.TotalPayments = totalPayments;
            ViewBag.CurrentStatus = status;

            // Truyền thống kê
            ViewBag.TotalAmount = stats.Where(s => s.Status == PaymentStatus.Success).Sum(s => s.Total);
            ViewBag.SuccessCount = stats.Where(s => s.Status == PaymentStatus.Success).Sum(s => s.Count);
            ViewBag.PendingCount = stats.Where(s => s.Status == PaymentStatus.Pending).Sum(s => s.Count);
            ViewBag.FailedCount = stats.Where(s => s.Status == PaymentStatus.Failed).Sum(s => s.Count);

            return View(payments);
        }

        // GET: Payment/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var payment = await _context.Payments
                .Include(p => p.Course)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PaymentID == id && p.UserID == currentUser.Id);

            if (payment == null)
            {
                TempData["Error"] = "Không tìm thấy giao dịch.";
                return NotFound();
            }

            return View(payment);
        }

        // POST: Payment/Cancel/5 - Hủy giao dịch đang chờ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.PaymentID == id && p.UserID == currentUser.Id);

            if (payment == null)
            {
                return Json(new { success = false, message = "Không tìm thấy giao dịch." });
            }

            if (payment.Status != PaymentStatus.Pending)
            {
                return Json(new { success = false, message = "Chỉ có thể hủy giao dịch đang chờ xử lý." });
            }

            try
            {
                payment.Status = PaymentStatus.Failed;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã hủy giao dịch thành công." });
            }
            catch
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi hủy giao dịch." });
            }
        }

        private async Task SendInvoiceEmail(User user, Course course, Payment payment)
        {
            try
            {
                var smtpSettings = _configuration.GetSection("SmtpSettings");

                if (string.IsNullOrEmpty(smtpSettings["Host"]))
                {
                    // Nếu chưa cấu hình SMTP, chỉ log và không gửi email
                    Console.WriteLine($"Email would be sent to {user.Email} for payment {payment.PaymentID}");
                    return;
                }

                using (var client = new SmtpClient(smtpSettings["Host"], int.Parse(smtpSettings["Port"])))
                {
                    client.EnableSsl = bool.Parse(smtpSettings["EnableSsl"]);
                    client.Credentials = new NetworkCredential(smtpSettings["Username"], smtpSettings["Password"]);

                    var invoiceNumber = $"INV-{payment.PaymentID:D6}";
                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(smtpSettings["Username"], "OnlineLearning Platform"),
                        Subject = $"Hóa đơn thanh toán #{invoiceNumber}",
                        Body = GenerateInvoiceEmailBody(user, course, payment, invoiceNumber),
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(user.Email);
                    await client.SendMailAsync(mailMessage);
                }
            }
            catch (Exception ex)
            {
                // Log error nhưng không throw để không ảnh hưởng đến luồng thanh toán
                Console.WriteLine($"Lỗi gửi email: {ex.Message}");
            }
        }

        private string GenerateInvoiceEmailBody(User user, Course course, Payment payment, string invoiceNumber)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px; }}
        .invoice-details {{ background: white; padding: 20px; border-radius: 5px; margin: 20px 0; }}
        .row {{ display: flex; justify-content: space-between; margin: 10px 0; }}
        .label {{ font-weight: bold; }}
        .amount {{ font-size: 1.5em; color: #667eea; font-weight: bold; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; }}
        .status {{ background: #fff3cd; color: #856404; padding: 15px; border-radius: 5px; margin: 20px 0; text-align: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎓 OnlineLearning</h1>
            <h2>Hóa đơn thanh toán</h2>
            <p>Số hóa đơn: {invoiceNumber}</p>
        </div>
        
        <div class='content'>
            <h3>Xin chào {user.HoTen},</h3>
            <p>Cảm ơn bạn đã chọn OnlineLearning! Chúng tôi đã nhận được yêu cầu thanh toán của bạn.</p>
            
            <div class='invoice-details'>
                <h4>Chi tiết giao dịch:</h4>
                <div class='row'>
                    <span class='label'>Mã giao dịch:</span>
                    <span>#{payment.PaymentID:D6}</span>
                </div>
                <div class='row'>
                    <span class='label'>Khóa học:</span>
                    <span>{course.TenKhoaHoc}</span>
                </div>
                <div class='row'>
                    <span class='label'>Giảng viên:</span>
                    <span>{course.User.HoTen}</span>
                </div>
                <div class='row'>
                    <span class='label'>Ngày thanh toán:</span>
                    <span>{payment.NgayThanhToan:dd/MM/yyyy HH:mm}</span>
                </div>
                <div class='row'>
                    <span class='label'>Phương thức:</span>
                    <span>{payment.PhuongThucThanhToan}</span>
                </div>
                <hr>
                <div class='row'>
                    <span class='label'>Tổng tiền:</span>
                    <span class='amount'>{payment.SoTien:N0} VNĐ</span>
                </div>
            </div>
            
            <div class='status'>
                <strong>⏳ Trạng thái: Chờ admin duyệt</strong>
                <p>Giao dịch của bạn đang được xử lý. Chúng tôi sẽ kích hoạt khóa học trong vòng 24 giờ làm việc.</p>
            </div>
            
            <h4>Các bước tiếp theo:</h4>
            <ul>
                <li>✅ Giao dịch đã được tạo thành công</li>
                <li>⏳ Đang chờ admin xác nhận thanh toán</li>
                <li>📧 Bạn sẽ nhận email thông báo khi khóa học được kích hoạt</li>
                <li>🎓 Bắt đầu học ngay sau khi được duyệt</li>
            </ul>
            
            <p><strong>Lưu ý:</strong> Vui lòng giữ lại email này làm bằng chứng thanh toán.</p>
        </div>
        
        <div class='footer'>
            <p>Cảm ơn bạn đã tin tưởng OnlineLearning!</p>
            <p>Liên hệ hỗ trợ: support@onlinelearning.vn | 1900 1234</p>
            <p>&copy; 2024 OnlineLearning. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}