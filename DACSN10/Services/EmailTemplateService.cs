using DACSN10.Models;

namespace DACSN10.Services
{
    public interface IEmailTemplateService
    {
        string GetPaymentSuccessTemplate(Payment payment, User user);
        string GetPaymentFailedTemplate(Payment payment, User user);
        string GetEnrollmentConfirmationTemplate(Enrollment enrollment, User user);
        string GetPasswordResetTemplate(User user, string resetLink);
        string GetWelcomeTemplate(User user);
    }

    public class EmailTemplateService : IEmailTemplateService
    {
        public string GetPaymentSuccessTemplate(Payment payment, User user)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Thanh toán thành công</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background: #f4f4f4; }}
        .container {{ max-width: 600px; margin: 20px auto; background: white; border-radius: 10px; overflow: hidden; box-shadow: 0 0 20px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #28a745, #20c997); color: white; padding: 40px 30px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 28px; font-weight: 300; }}
        .header p {{ margin: 10px 0 0 0; opacity: 0.9; }}
        .content {{ padding: 40px 30px; }}
        .success-badge {{ background: #28a745; color: white; padding: 8px 16px; border-radius: 20px; font-size: 12px; font-weight: bold; display: inline-block; }}
        .info-box {{ background: #f8f9fa; border-left: 4px solid #28a745; padding: 20px; margin: 20px 0; border-radius: 0 5px 5px 0; }}
        .amount {{ font-size: 32px; font-weight: bold; color: #28a745; }}
        .btn {{ background: #007bff; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block; margin: 10px 5px; font-weight: 500; }}
        .btn-success {{ background: #28a745; }}
        .footer {{ background: #6c757d; color: white; padding: 30px; text-align: center; }}
        .footer p {{ margin: 5px 0; }}
        .divider {{ height: 1px; background: #eee; margin: 30px 0; }}
        @media (max-width: 600px) {{
            .container {{ margin: 10px; }}
            .content {{ padding: 20px; }}
            .header {{ padding: 30px 20px; }}
        }}
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
            
            <p>Cảm ơn bạn đã tin tưởng và sử dụng dịch vụ của OnlineLearning. Giao dịch thanh toán của bạn đã được xử lý thành công!</p>
            
            <div class='info-box'>
                <h3 style='margin-top: 0; color: #28a745;'>📋 Chi tiết giao dịch</h3>
                <table style='width: 100%; border-collapse: collapse;'>
                    <tr><td style='padding: 8px 0; border-bottom: 1px solid #eee;'><strong>Mã giao dịch:</strong></td><td style='padding: 8px 0; border-bottom: 1px solid #eee; text-align: right;'>#{payment.PaymentID:D6}</td></tr>
                    <tr><td style='padding: 8px 0; border-bottom: 1px solid #eee;'><strong>Khóa học:</strong></td><td style='padding: 8px 0; border-bottom: 1px solid #eee; text-align: right;'>{payment.Course.TenKhoaHoc}</td></tr>
                    <tr><td style='padding: 8px 0; border-bottom: 1px solid #eee;'><strong>Giảng viên:</strong></td><td style='padding: 8px 0; border-bottom: 1px solid #eee; text-align: right;'>{payment.Course.User?.HoTen ?? "Chưa có thông tin"}</td></tr>
                    <tr><td style='padding: 8px 0; border-bottom: 1px solid #eee;'><strong>Phương thức:</strong></td><td style='padding: 8px 0; border-bottom: 1px solid #eee; text-align: right;'>{payment.PhuongThucThanhToan}</td></tr>
                    <tr><td style='padding: 8px 0; border-bottom: 1px solid #eee;'><strong>Thời gian:</strong></td><td style='padding: 8px 0; border-bottom: 1px solid #eee; text-align: right;'>{payment.NgayThanhToan:dd/MM/yyyy HH:mm}</td></tr>
                    <tr><td style='padding: 8px 0;'><strong>Số tiền:</strong></td><td style='padding: 8px 0; text-align: right;'><span class='amount'>{payment.SoTien:N0} VNĐ</span></td></tr>
                    <tr><td style='padding: 8px 0;'><strong>Trạng thái:</strong></td><td style='padding: 8px 0; text-align: right;'><span class='success-badge'>Thành công</span></td></tr>
                </table>
            </div>
            
            <div style='text-align: center; margin: 30px 0;'>
                <a href='#' class='btn btn-success'>🚀 Bắt đầu học ngay</a>
                <a href='#' class='btn'>📄 Xem hóa đơn</a>
            </div>
            
            <div class='divider'></div>
            
            <div class='info-box'>
                <h3 style='margin-top: 0; color: #007bff;'>📚 Bước tiếp theo</h3>
                <ul style='padding-left: 20px;'>
                    <li>Truy cập mục ""Khóa học của tôi"" để bắt đầu học</li>
                    <li>Theo dõi tiến độ học tập của bạn</li>
                    <li>Hoàn thành các bài tập và kiểm tra</li>
                    <li>Nhận chứng chỉ sau khi hoàn thành khóa học</li>
                </ul>
            </div>
        </div>
        
        <div class='footer'>
            <p><strong>OnlineLearning Platform</strong></p>
            <p>📞 Hotline: 1900-xxxx | 📧 Email: support@onlinelearning.com</p>
            <p>Cảm ơn bạn đã chọn OnlineLearning!</p>
        </div>
    </div>
</body>
</html>";
        }

        public string GetPaymentFailedTemplate(Payment payment, User user)
        {
            // Similar implementation for failed payment template
            return ""; // Implementation here
        }

        public string GetEnrollmentConfirmationTemplate(Enrollment enrollment, User user)
        {
            // Similar implementation for enrollment confirmation template
            return ""; // Implementation here
        }

        public string GetPasswordResetTemplate(User user, string resetLink)
        {
            // Implementation for password reset template
            return ""; // Implementation here
        }

        public string GetWelcomeTemplate(User user)
        {
            // Implementation for welcome email template
            return ""; // Implementation here
        }
    }
}