using System;
using System.ComponentModel.DataAnnotations;

namespace DACSN10.Models
{
    public class SystemSettings
    {
        [Key]
        public int Id { get; set; }

        // ======================= CÀI ĐẶT CHUNG =======================
        [Required, MaxLength(256)]
        public string SiteName { get; set; } = "OnlineLearning - Nền tảng học trực tuyến";

        [MaxLength(1024)]
        public string? SiteDescription { get; set; }
            = "Nền tảng học trực tuyến hàng đầu Việt Nam với hàng ngàn khóa học chất lượng";

        [Required, MaxLength(256)]
        public string ContactEmail { get; set; } = "admin@onlinelearning.com";

        [MaxLength(32)]
        public string? ContactPhone { get; set; } = "1900-1234";

        [MaxLength(512)]
        public string? ContactAddress { get; set; }
            = "123 Nguyễn Văn Cừ, Q.5, TP.HCM";

        [MaxLength(64)]
        public string? TimeZone { get; set; } = "Asia/Ho_Chi_Minh";

        [MaxLength(8)]
        public string? DefaultLanguage { get; set; } = "vi";

        public bool MaintenanceMode { get; set; } = false;
        public bool AllowNewRegistration { get; set; } = true;


        // ======================= Giao diện =======================
        [MaxLength(32)]
        public string? Theme { get; set; } = "blue";

        [MaxLength(7)]
        public string? PrimaryColor { get; set; } = "#3b82f6";

        [MaxLength(64)]
        public string? FontFamily { get; set; } = "Inter";

        public bool EnableDarkMode { get; set; } = false;
        public bool EnableAnimations { get; set; } = true;


        // ======================= Bảo mật =======================
        public bool Require2FAForAdmins { get; set; } = true;
        public bool EncryptSession { get; set; } = true;

        [MaxLength(16)]
        public string? SessionTimeoutMinutes { get; set; } = "30";

        [MaxLength(16)]
        public string? PasswordComplexityLevel { get; set; } = "medium";

        [MaxLength(16)]
        public string? MaxLoginFailures { get; set; } = "5";

        [MaxLength(16)]
        public string? AccountLockMinutes { get; set; } = "30";

        public bool ForceHttps { get; set; } = true;


        // ======================= Notifications & SMTP =======================
        public bool NotifyNewRegistration { get; set; } = true;
        public bool NotifyNewPayment { get; set; } = true;
        public bool NotifyNewCourse { get; set; } = false;
        public bool NotifySystemError { get; set; } = true;

        public bool BrowserNotifications { get; set; } = true;
        public bool NotificationSound { get; set; } = false;

        [MaxLength(256)]
        public string? SmtpServer { get; set; } = "smtp.gmail.com";

        public int SmtpPort { get; set; } = 587;

        [MaxLength(256)]
        public string? NotificationFromEmail { get; set; }
            = "noreply@onlinelearning.com";


        // ======================= Payment gateways =======================
        public bool EnableMomo { get; set; } = true;
        public bool EnableVnPay { get; set; } = true;
        public bool EnableZaloPay { get; set; } = false;


        // ======================= Analytics =======================
        [MaxLength(64)]
        public string? GoogleAnalyticsId { get; set; } = "G-ABC123DEF4";

        [MaxLength(64)]
        public string? FacebookPixelId { get; set; } = null;

        [MaxLength(64)]
        public string? HotjarSiteId { get; set; } = null;


        // ======================= Cloud storage =======================
        public bool EnableS3 { get; set; } = false;
        public bool EnableGcs { get; set; } = false;


        // ======================= Communication =======================
        public bool EnableSlack { get; set; } = false;
        public bool EnableDiscord { get; set; } = false;


        // ======================= API Keys =======================
        [MaxLength(128)]
        public string? MainApiKeyMasked { get; set; }
            = "sk_live_51K7***************************";

        [MaxLength(128)]
        public string? WebhookApiKeyMasked { get; set; }
            = "wh_sec_***************************";


        // ======================= System =======================
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
