// Services/EmailService.cs
using System.Net;
using System.Net.Mail;
using System.Text;
using DACSN10.Models;

namespace DACSN10.Services
{
    public interface IEmailService
    {
        Task SendPaymentSuccessEmailAsync(Payment payment, User user);
        Task SendPaymentFailedEmailAsync(Payment payment, User user);
        Task SendPaymentWaitingConfirmationAsync(Payment payment, User user);
        Task SendEnrollmentConfirmationAsync(Enrollment enrollment, User user);
        Task SendPasswordResetEmailAsync(User user, string resetLink);
        Task SendWelcomeEmailAsync(User user);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendPaymentSuccessEmailAsync(Payment payment, User user)
        {
            var subject = $"✅ Thanh toán thành công - Khóa học {payment.Course.TenKhoaHoc}";

            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #28a745, #20c997); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f8f9fa; padding: 30px; }}
        .success-badge {{ background: #28a745; color: white; padding: 10px 20px; display: inline-block; border-radius: 50px; font-weight: bold; }}
        .details {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; }}
        .footer {{ background: #6c757d; color: white; padding: 20px; text-align: center; border-radius: 0 0 10px 10px; }}
        .btn {{ background: #007bff; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; display: inline-block; margin: 10px; }}
        .amount {{ font-size: 24px; font-weight: bold; color: #28a745; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 Thanh toán thành công!</h1>
            <p>Chúc mừng bạn đã đăng ký khóa học thành công</p>
        </div>
        
        <div class='content'>
            <p>Xin chào <strong>{user.HoTen}</strong>,</p>
            
            <p>Cảm ơn bạn đã tin tưởng và thanh toán khóa học tại OnlineLearning. Giao dịch của bạn đã được xử lý thành công!</p>
            
            <div class='details'>
                <h3>📋 Chi tiết giao dịch</h3>
                <p><strong>Mã giao dịch:</strong> #{payment.PaymentID:D6}</p>
                <p><strong>Khóa học:</strong> {payment.Course.TenKhoaHoc}</p>
                <p><strong>Giảng viên:</strong> {payment.Course.User?.HoTen ?? "Chưa có thông tin"}</p>
                <p><strong>Phương thức thanh toán:</strong> {payment.PhuongThucThanhToan}</p>
                <p><strong>Số tiền:</strong> <span class='amount'>{payment.SoTien:N0} VNĐ</span></p>
                <p><strong>Thời gian:</strong> {payment.NgayThanhToan:dd/MM/yyyy HH:mm}</p>
                <p><strong>Trạng thái:</strong> <span class='success-badge'>Thành công</span></p>
            </div>
            
            <div style='text-align: center; margin: 30px 0;'>
                <a href='#' class='btn'>🚀 Bắt đầu học ngay</a>
                <a href='#' class='btn' style='background: #6c757d;'>📄 Xem hóa đơn</a>
            </div>
            
            <div class='details'>
                <h3>📚 Bước tiếp theo</h3>
                <ul>
                    <li>Truy cập mục ""Khóa học của tôi"" để bắt đầu học</li>
                    <li>Theo dõi tiến độ học tập của bạn</li>
                    <li>Hoàn thành các bài tập và kiểm tra</li>
                    <li>Nhận chứng chỉ sau khi hoàn thành khóa học</li>
                </ul>
            </div>
        </div>
        
        <div class='footer'>
            <p>Cảm ơn bạn đã chọn OnlineLearning!</p>
            <p>📞 Hotline: 1900-xxxx | 📧 Email: support@onlinelearning.com</p>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(user.Email, subject, body);
        }

        public async Task SendPaymentFailedEmailAsync(Payment payment, User user)
        {
            var subject = $"❌ Thanh toán thất bại - Khóa học {payment.Course.TenKhoaHoc}";

            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #dc3545, #c82333); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f8f9fa; padding: 30px; }}
        .failed-badge {{ background: #dc3545; color: white; padding: 10px 20px; display: inline-block; border-radius: 50px; font-weight: bold; }}
        .details {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; }}
        .footer {{ background: #6c757d; color: white; padding: 20px; text-align: center; border-radius: 0 0 10px 10px; }}
        .btn {{ background: #007bff; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; display: inline-block; margin: 10px; }}
        .retry-btn {{ background: #28a745; }}
        .amount {{ font-size: 24px; font-weight: bold; color: #dc3545; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>😔 Thanh toán thất bại</h1>
            <p>Giao dịch của bạn không thể hoàn tất</p>
        </div>
        
        <div class='content'>
            <p>Xin chào <strong>{user.HoTen}</strong>,</p>
            
            <p>Rất tiếc, giao dịch thanh toán khóa học của bạn không thể hoàn tất. Đừng lo lắng, bạn có thể thử lại hoặc chọn phương thức thanh toán khác.</p>
            
            <div class='details'>
                <h3>📋 Chi tiết giao dịch</h3>
                <p><strong>Mã giao dịch:</strong> #{payment.PaymentID:D6}</p>
                <p><strong>Khóa học:</strong> {payment.Course.TenKhoaHoc}</p>
                <p><strong>Phương thức thanh toán:</strong> {payment.PhuongThucThanhToan}</p>
                <p><strong>Số tiền:</strong> <span class='amount'>{payment.SoTien:N0} VNĐ</span></p>
                <p><strong>Thời gian:</strong> {payment.NgayThanhToan:dd/MM/yyyy HH:mm}</p>
                <p><strong>Trạng thái:</strong> <span class='failed-badge'>Thất bại</span></p>
            </div>
            
            <div style='text-align: center; margin: 30px 0;'>
                <a href='#' class='btn retry-btn'>🔄 Thử lại thanh toán</a>
                <a href='#' class='btn'>👁 Xem khóa học</a>
            </div>
            
            <div class='details'>
                <h3>💡 Nguyên nhân có thể</h3>
                <ul>
                    <li>Thông tin thẻ không chính xác</li>
                    <li>Tài khoản không đủ số dư</li>
                    <li>Kết nối mạng không ổn định</li>
                    <li>Ngân hàng từ chối giao dịch</li>
                </ul>
                
                <h3>🆘 Cần hỗ trợ?</h3>
                <p>Liên hệ với chúng tôi qua:</p>
                <ul>
                    <li>📞 Hotline: 1900-xxxx (8:00 - 22:00)</li>
                    <li>📧 Email: support@onlinelearning.com</li>
                    <li>💬 Live chat trên website</li>
                </ul>
            </div>
        </div>
        
        <div class='footer'>
            <p>Chúng tôi luôn sẵn sàng hỗ trợ bạn!</p>
            <p>OnlineLearning - Học tập không giới hạn</p>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(user.Email, subject, body);
        }

        public async Task SendPaymentWaitingConfirmationAsync(Payment payment, User user)
        {
            var subject = $"⏳ Chờ xác nhận thanh toán - Khóa học {payment.Course.TenKhoaHoc}";

            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #ffc107, #e0a800); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f8f9fa; padding: 30px; }}
        .waiting-badge {{ background: #ffc107; color: #212529; padding: 10px 20px; display: inline-block; border-radius: 50px; font-weight: bold; }}
        .details {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; }}
        .footer {{ background: #6c757d; color: white; padding: 20px; text-align: center; border-radius: 0 0 10px 10px; }}
        .btn {{ background: #007bff; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; display: inline-block; margin: 10px; }}
        .amount {{ font-size: 24px; font-weight: bold; color: #ffc107; }}
        .highlight {{ background: #fff3cd; padding: 15px; border-radius: 8px; border-left: 4px solid #ffc107; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>⏳ Chờ xác nhận thanh toán</h1>
            <p>Vui lòng chuyển khoản theo thông tin bên dưới</p>
        </div>
        
        <div class='content'>
            <p>Xin chào <strong>{user.HoTen}</strong>,</p>
            
            <p>Cảm ơn bạn đã chọn thanh toán qua MOMO. Vui lòng thực hiện chuyển khoản theo thông tin bên dưới và chờ admin xác nhận.</p>
            
            <div class='highlight'>
                <h3>💳 Thông tin chuyển khoản MOMO</h3>
                <p><strong>Link chuyển khoản:</strong> <a href='https://me.momo.vn/xoanws' target='_blank'>https://me.momo.vn/xoanws</a></p>
                <p><strong>Số tiền:</strong> <span class='amount'>{payment.SoTien:N0} VNĐ</span></p>
                <p><strong>Nội dung:</strong> COURSE{payment.CourseID}USER{payment.UserID}</p>
                <p><strong>⚠️ Lưu ý:</strong> Vui lòng ghi đúng nội dung để admin có thể xác nhận nhanh chóng!</p>
            </div>
            
            <div class='details'>
                <h3>📋 Chi tiết đơn hàng</h3>
                <p><strong>Mã giao dịch:</strong> #{payment.PaymentID:D6}</p>
                <p><strong>Khóa học:</strong> {payment.Course.TenKhoaHoc}</p>
                <p><strong>Giảng viên:</strong> {payment.Course.User?.HoTen ?? "Chưa có thông tin"}</p>
                <p><strong>Phương thức:</strong> {payment.PhuongThucThanhToan}</p>
                <p><strong>Thời gian tạo:</strong> {payment.NgayThanhToan:dd/MM/yyyy HH:mm}</p>
                <p><strong>Trạng thái:</strong> <span class='waiting-badge'>Chờ xác nhận</span></p>
            </div>
            
            <div style='text-align: center; margin: 30px 0;'>
                <a href='https://me.momo.vn/xoanws' class='btn' style='background: #d91a72;'>💰 Chuyển khoản MOMO</a>
                <a href='#' class='btn' style='background: #6c757d;'>📊 Kiểm tra trạng thái</a>
            </div>
            
            <div class='details'>
                <h3>⏰ Thời gian xử lý</h3>
                <ul>
                    <li>Thời gian làm việc: 8:00 - 22:00 (T2-CN)</li>
                    <li>Xác nhận trong vòng 30 phút - 2 giờ</li>
                    <li>Ngoài giờ làm việc: Xác nhận vào ngày hôm sau</li>
                </ul>
                
                <h3>📞 Liên hệ hỗ trợ</h3>
                <p>Nếu bạn đã chuyển khoản nhưng chưa được xác nhận sau 2 giờ, vui lòng liên hệ:</p>
                <ul>
                    <li>📞 Hotline: 1900-xxxx</li>
                    <li>📧 Email: support@onlinelearning.com</li>
                    <li>💬 Live chat trên website</li>
                </ul>
            </div>
        </div>
        
        <div class='footer'>
            <p>Cảm ơn bạn đã chọn OnlineLearning!</p>
            <p>Chúng tôi sẽ xác nhận thanh toán của bạn sớm nhất có thể.</p>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(user.Email, subject, body);
        }

        public async Task SendEnrollmentConfirmationAsync(Enrollment enrollment, User user)
        {
            var subject = $"🎓 Chào mừng bạn đến với khóa học: {enrollment.Course.TenKhoaHoc}";

            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #007bff, #0056b3); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f8f9fa; padding: 30px; }}
        .welcome-badge {{ background: #007bff; color: white; padding: 10px 20px; display: inline-block; border-radius: 50px; font-weight: bold; }}
        .course-info {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 5px solid #007bff; }}
        .footer {{ background: #6c757d; color: white; padding: 20px; text-align: center; border-radius: 0 0 10px 10px; }}
        .btn {{ background: #28a745; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; display: inline-block; margin: 10px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 Chào mừng bạn!</h1>
            <p>Bạn đã đăng ký thành công khóa học</p>
        </div>
        
        <div class='content'>
            <p>Xin chào <strong>{user.HoTen}</strong>,</p>
            
            <p>Chúc mừng bạn đã đăng ký thành công khóa học! Hành trình học tập của bạn bắt đầu từ đây.</p>
            
            <div class='course-info'>
                <h3>📚 Thông tin khóa học</h3>
                <p><strong>Tên khóa học:</strong> {enrollment.Course.TenKhoaHoc}</p>
                <p><strong>Giảng viên:</strong> {enrollment.Course.User?.HoTen ?? "Chưa có thông tin"}</p>
                <p><strong>Số bài học:</strong> {enrollment.Course.Lessons?.Count ?? 0} bài</p>
                <p><strong>Ngày đăng ký:</strong> {enrollment.EnrollDate:dd/MM/yyyy}</p>
                <p><strong>Tiến độ hiện tại:</strong> <span class='welcome-badge'>{enrollment.Progress:F1}%</span></p>
            </div>
            
            <div style='text-align: center; margin: 30px 0;'>
                <a href='#' class='btn'>🚀 Bắt đầu học ngay</a>
            </div>
            
            <div class='course-info'>
                <h3>🎯 Lộ trình học tập</h3>
                <ol>
                    <li>Xem video bài giảng theo thứ tự</li>
                    <li>Hoàn thành các bài tập thực hành</li>
                    <li>Tham gia các bài kiểm tra</li>
                    <li>Nhận chứng chỉ sau khi hoàn thành</li>
                </ol>
                
                <h3>💡 Mẹo học tập hiệu quả</h3>
                <ul>
                    <li>Học đều đặn mỗi ngày 30-60 phút</li>
                    <li>Ghi chú những điểm quan trọng</li>
                    <li>Thực hành ngay sau khi học lý thuyết</li>
                    <li>Đặt câu hỏi khi gặp khó khăn</li>
                </ul>
            </div>
        </div>
        
        <div class='footer'>
            <p>Chúc bạn học tập thành công!</p>
            <p>📞 Hotline: 1900-xxxx | 📧 Email: support@onlinelearning.com</p>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(user.Email, subject, body);
        }

        public async Task SendPasswordResetEmailAsync(User user, string resetLink)
        {
            var subject = "🔐 Đặt lại mật khẩu - OnlineLearning";

            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #6f42c1, #5a32a3); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f8f9fa; padding: 30px; }}
        .footer {{ background: #6c757d; color: white; padding: 20px; text-align: center; border-radius: 0 0 10px 10px; }}
        .btn {{ background: #6f42c1; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; display: inline-block; margin: 20px 0; }}
        .warning {{ background: #fff3cd; padding: 15px; border-radius: 8px; border-left: 4px solid #ffc107; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔐 Đặt lại mật khẩu</h1>
            <p>Yêu cầu đặt lại mật khẩu cho tài khoản của bạn</p>
        </div>
        
        <div class='content'>
            <p>Xin chào <strong>{user.HoTen}</strong>,</p>
            
            <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn. Vui lòng nhấp vào nút bên dưới để đặt lại mật khẩu:</p>
            
            <div style='text-align: center;'>
                <a href='{resetLink}' class='btn'>🔄 Đặt lại mật khẩu</a>
            </div>
            
            <div class='warning'>
                <h3>⚠️ Lưu ý bảo mật</h3>
                <ul>
                    <li>Link này sẽ hết hạn sau 30 phút</li>
                    <li>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này</li>
                    <li>Không chia sẻ link này với bất kỳ ai</li>
                </ul>
            </div>
        </div>
        
        <div class='footer'>
            <p>OnlineLearning - Bảo mật thông tin của bạn là ưu tiên hàng đầu</p>
            <p>📞 Hotline: 1900-xxxx | 📧 Email: support@onlinelearning.com</p>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(user.Email, subject, body);
        }

        public async Task SendWelcomeEmailAsync(User user)
        {
            var subject = "🎉 Chào mừng bạn đến với OnlineLearning!";

            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #17a2b8, #138496); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f8f9fa; padding: 30px; }}
        .feature {{ background: white; padding: 15px; border-radius: 8px; margin: 10px 0; border-left: 4px solid #17a2b8; }}
        .footer {{ background: #6c757d; color: white; padding: 20px; text-align: center; border-radius: 0 0 10px 10px; }}
        .btn {{ background: #17a2b8; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; display: inline-block; margin: 10px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 Chào mừng bạn!</h1>
            <p>Bắt đầu hành trình học tập cùng OnlineLearning</p>
        </div>
        
        <div class='content'>
            <p>Xin chào <strong>{user.HoTen}</strong>,</p>
            
            <p>Chào mừng bạn đến với OnlineLearning! Chúng tôi rất vui mừng được đồng hành cùng bạn trong hành trình học tập và phát triển bản thân.</p>
            
            <div class='feature'>
                <h3>🚀 Bắt đầu ngay</h3>
                <ul>
                    <li>Khám phá hàng ngàn khóa học chất lượng cao</li>
                    <li>Học từ các chuyên gia hàng đầu</li>
                    <li>Nhận chứng chỉ sau khi hoàn thành</li>
                </ul>
            </div>
            
            <div class='feature'>
                <h3>💡 Tính năng nổi bật</h3>
                <ul>
                    <li>Video bài giảng HD với phụ đề</li>
                    <li>Bài tập thực hành và kiểm tra</li>
                    <li>Cộng đồng học tập sôi động</li>
                    <li>Hỗ trợ 24/7 từ đội ngũ chuyên nghiệp</li>
                </ul>
            </div>
            
            <div style='text-align: center; margin: 30px 0;'>
                <a href='#' class='btn'>🔍 Khám phá khóa học</a>
                <a href='#' class='btn' style='background: #28a745;'>📚 Khóa học miễn phí</a>
            </div>
        </div>
        
        <div class='footer'>
            <p>Chúc bạn có những trải nghiệm học tập tuyệt vời!</p>
            <p>📞 Hotline: 1900-xxxx | 📧 Email: support@onlinelearning.com</p>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(user.Email, subject, body);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
                var smtpUser = _configuration["Email:SmtpUser"];
                var smtpPass = _configuration["Email:SmtpPass"];
                var fromEmail = _configuration["Email:FromEmail"];
                var fromName = _configuration["Email:FromName"] ?? "OnlineLearning";
                var enableSsl = bool.Parse(_configuration["Email:EnableSsl"] ?? "true");

                // Validation
                if (string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPass))
                {
                    Console.WriteLine("Email configuration is missing. Please check appsettings.json");
                    return;
                }

                using (var client = new SmtpClient(smtpHost, smtpPort))
                {
                    client.EnableSsl = enableSsl;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(smtpUser, smtpPass);
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.Timeout = 30000; // 30 seconds timeout

                    var message = new MailMessage
                    {
                        From = new MailAddress(fromEmail ?? smtpUser, fromName),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true,
                        BodyEncoding = Encoding.UTF8,
                        SubjectEncoding = Encoding.UTF8,
                        Priority = MailPriority.Normal
                    };

                    message.To.Add(new MailAddress(toEmail));

                    // Add headers for better deliverability
                    message.Headers.Add("X-Mailer", "OnlineLearning Platform");
                    message.Headers.Add("X-Priority", "3");

                    Console.WriteLine($"Sending email to: {toEmail}");
                    await client.SendMailAsync(message);
                    Console.WriteLine($"Email sent successfully to: {toEmail}");
                }
            }
            catch (SmtpException smtpEx)
            {
                Console.WriteLine($"SMTP Error: {smtpEx.Message}");
                Console.WriteLine($"Status Code: {smtpEx.StatusCode}");

                // Log specific SMTP errors
                switch (smtpEx.StatusCode)
                {
                    case SmtpStatusCode.MailboxBusy:
                        Console.WriteLine("Mailbox busy - try again later");
                        break;
                    case SmtpStatusCode.InsufficientStorage:
                        Console.WriteLine("Insufficient storage");
                        break;
                    case SmtpStatusCode.TransactionFailed:
                        Console.WriteLine("Transaction failed - check credentials");
                        break;
                    default:
                        Console.WriteLine($"SMTP Error Code: {smtpEx.StatusCode}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General email error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}