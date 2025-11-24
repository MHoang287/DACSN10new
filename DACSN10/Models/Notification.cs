using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DACSN10.Models
{
    public class Notification
    {
        [Key]
        public int NotificationID { get; set; }

        [Required]
        [ForeignKey("User")]
        public string UserID { get; set; } // ID của người nhận thông báo
        public virtual User User { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } // Tiêu đề thông báo

        [Required]
        [StringLength(1000)]
        public string Message { get; set; } // Nội dung thông báo

        [StringLength(50)]
        public string Type { get; set; } // Loại thông báo (NewLesson, NewQuiz, NewCourse, LiveStream, EnrollmentSuccess, etc.)

        [StringLength(100)]
        public string? RelatedID { get; set; } // ID liên quan (CourseID, LessonID, QuizID, etc.)

        [StringLength(500)]
        public string? Link { get; set; } // Link đến trang chi tiết

        public bool IsRead { get; set; } = false; // Đã đọc chưa

        public DateTime CreatedAt { get; set; } = DateTime.Now; // Thời gian tạo

        public DateTime? ReadAt { get; set; } // Thời gian đọc

        // Computed property for display
        [NotMapped]
        public string FormattedCreatedAt => CreatedAt.ToString("dd/MM/yyyy HH:mm");

        [NotMapped]
        public string TimeAgo
        {
            get
            {
                var timeSpan = DateTime.Now - CreatedAt;

                if (timeSpan.TotalMinutes < 1)
                    return "Vừa xong";
                if (timeSpan.TotalMinutes < 60)
                    return $"{(int)timeSpan.TotalMinutes} phút trước";
                if (timeSpan.TotalHours < 24)
                    return $"{(int)timeSpan.TotalHours} giờ trước";
                if (timeSpan.TotalDays < 30)
                    return $"{(int)timeSpan.TotalDays} ngày trước";
                if (timeSpan.TotalDays < 365)
                    return $"{(int)(timeSpan.TotalDays / 30)} tháng trước";

                return $"{(int)(timeSpan.TotalDays / 365)} năm trước";
            }
        }
    }
}