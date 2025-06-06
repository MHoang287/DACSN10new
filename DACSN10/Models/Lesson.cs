using System.ComponentModel.DataAnnotations;

namespace DACSN10.Models
{
    public class Lesson
    {
        [Key]
        public int LessonID { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn khóa học")]
        public int CourseID { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên bài học")]
        [StringLength(200, ErrorMessage = "Tên bài học không được vượt quá 200 ký tự")]
        [Display(Name = "Tên bài học")]
        public string TenBaiHoc { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung bài học")]
        [Display(Name = "Nội dung")]
        public string NoiDung { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập thời lượng")]
        [Range(1, int.MaxValue, ErrorMessage = "Thời lượng phải lớn hơn 0")]
        [Display(Name = "Thời lượng (phút)")]
        public int ThoiLuong { get; set; }

        [Display(Name = "Video URL")]
        [Url(ErrorMessage = "URL không hợp lệ")]
        public string? VideoUrl { get; set; } // Nullable - optional field

        // Navigation property
        public virtual Course? Course { get; set; }

        // Constructor
        public Lesson()
        {
            VideoUrl = string.Empty; // Default to empty string
        }
    }
}