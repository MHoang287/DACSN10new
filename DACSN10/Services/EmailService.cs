using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DACSN10.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
        Task SendNotificationEmailAsync(string to, string title, string message, string link = null);
        Task SendEnrollmentConfirmationAsync(string to, string courseName, string teacherName);
        Task SendPasswordResetEmailAsync(string to, string resetLink);
        Task SendEmailConfirmationAsync(string to, string confirmationLink);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPass;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly bool _enableSsl;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // Load email configuration
            _smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
            _smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            _smtpUser = _configuration["Email:SmtpUser"];
            _smtpPass = _configuration["Email:SmtpPass"];
            _fromEmail = _configuration["Email:FromEmail"];
            _fromName = _configuration["Email:FromName"] ?? "OnlineLearning";
            _enableSsl = bool.Parse(_configuration["Email:EnableSsl"] ?? "true");
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                using var client = new SmtpClient(_smtpHost, _smtpPort)
                {
                    EnableSsl = _enableSsl,
                    Credentials = new NetworkCredential(_smtpUser, _smtpPass)
                };

                using var message = new MailMessage
                {
                    From = new MailAddress(_fromEmail, _fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                message.To.Add(to);

                await client.SendMailAsync(message);
                _logger.LogInformation("Email sent successfully to {Email}", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", to);
                // Don't throw - email failure shouldn't break the main flow
            }
        }

        public async Task SendNotificationEmailAsync(string to, string title, string message, string link = null)
        {
            var subject = title;

            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
        }}
        .container {{
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 30px;
            border-radius: 10px 10px 0 0;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            font-size: 28px;
        }}
        .content {{
            background: white;
            padding: 30px;
            border-radius: 0 0 10px 10px;
            box-shadow: 0 4px 6px rgba(0,0,0,0.1);
        }}
        .notification-icon {{
            font-size: 48px;
            text-align: center;
            margin-bottom: 20px;
        }}
        .message {{
            font-size: 16px;
            margin-bottom: 25px;
            padding: 20px;
            background: #f8f9fa;
            border-left: 4px solid #007bff;
            border-radius: 5px;
        }}
        .button {{
            display: inline-block;
            padding: 12px 30px;
            background: #007bff;
            color: white;
            text-decoration: none;
            border-radius: 5px;
            font-weight: bold;
            text-align: center;
        }}
        .button:hover {{
            background: #0056b3;
        }}
        .footer {{
            text-align: center;
            padding: 20px;
            color: #666;
            font-size: 14px;
        }}
        .footer a {{
            color: #007bff;
            text-decoration: none;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔔 {title}</h1>
        </div>
        <div class='content'>
            <div class='notification-icon'>
                📬
            </div>
            <div class='message'>
                {message}
            </div>
            {(string.IsNullOrEmpty(link) ? "" : $@"
            <div style='text-align: center;'>
                <a href='{_configuration["AppSettings:BaseUrl"]}{link}' class='button'>
                    Xem chi tiết →
                </a>
            </div>")}
        </div>
        <div class='footer'>
            <p>Email này được gửi tự động từ <a href='{_configuration["AppSettings:BaseUrl"]}'>OnlineLearning</a></p>
            <p>Vui lòng không trả lời email này. Nếu cần hỗ trợ, hãy liên hệ qua trang web.</p>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(to, subject, body);
        }

        public async Task SendEnrollmentConfirmationAsync(string to, string courseName, string teacherName)
        {
            var subject = $"✅ Xác nhận đăng ký khóa học: {courseName}";

            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        /* Same styles as above */
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 Chúc mừng bạn!</h1>
        </div>
        <div class='content'>
            <h2>Đăng ký khóa học thành công</h2>
            <p>Bạn đã đăng ký thành công khóa học:</p>
            <div class='message'>
                <strong>📚 Khóa học:</strong> {courseName}<br>
                <strong>👨‍🏫 Giảng viên:</strong> {teacherName}<br>
                <strong>📅 Thời gian:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}
            </div>
            <p>Bạn có thể bắt đầu học ngay bây giờ!</p>
            <div style='text-align: center;'>
                <a href='{_configuration["AppSettings:BaseUrl"]}/Course' class='button'>
                    Vào học ngay →
                </a>
            </div>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(to, subject, body);
        }

        public async Task SendPasswordResetEmailAsync(string to, string resetLink)
        {
            var subject = "🔐 Đặt lại mật khẩu";

            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
</head>
<body>
    <div class='container'>
        <h2>Yêu cầu đặt lại mật khẩu</h2>
        <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn.</p>
        <p>Nhấp vào liên kết bên dưới để đặt lại mật khẩu:</p>
        <a href='{resetLink}' class='button'>Đặt lại mật khẩu</a>
        <p>Liên kết này sẽ hết hạn sau 24 giờ.</p>
        <p>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.</p>
    </div>
</body>
</html>";

            await SendEmailAsync(to, subject, body);
        }

        public async Task SendEmailConfirmationAsync(string to, string confirmationLink)
        {
            var subject = "✉️ Xác nhận địa chỉ email";

            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
</head>
<body>
    <div class='container'>
        <h2>Xác nhận địa chỉ email</h2>
        <p>Cảm ơn bạn đã đăng ký tài khoản tại OnlineLearning.</p>
        <p>Vui lòng nhấp vào liên kết bên dưới để xác nhận địa chỉ email của bạn:</p>
        <a href='{confirmationLink}' class='button'>Xác nhận email</a>
        <p>Sau khi xác nhận, bạn có thể đăng nhập và sử dụng đầy đủ các tính năng của hệ thống.</p>
    </div>
</body>
</html>";

            await SendEmailAsync(to, subject, body);
        }
    }
}