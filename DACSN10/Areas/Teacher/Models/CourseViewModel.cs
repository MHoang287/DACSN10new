using DACSN10.Models;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace DACSN10.Areas.Teacher.Models
{
    public class CourseViewModel
    {
        public int CourseID { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên khóa học")]
        [Display(Name = "Tên Khóa Học")]
        public string TenKhoaHoc { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mô tả")]
        [Display(Name = "Mô Tả")]
        public string MoTa { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá")]
        [Display(Name = "Giá")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá không được âm")]
        public decimal Gia { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn trạng thái")]
        [Display(Name = "Trạng Thái")]
        public string TrangThai { get; set; }

        [Display(Name = "Ngày Tạo")]
        public DateTime NgayTao { get; set; }

        [Display(Name = "Danh Mục")]
        public List<int> SelectedCategories { get; set; } = new List<int>();

        public List<Category> AvailableCategories { get; set; } = new List<Category>();
    }

    public class LessonViewModel
    {
        public int LessonID { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên bài học")]
        [Display(Name = "Tên Bài Học")]
        public string TenBaiHoc { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung")]
        [Display(Name = "Nội Dung")]
        public string NoiDung { get; set; }

        [Display(Name = "Thời Lượng (phút)")]
        [Range(0, int.MaxValue, ErrorMessage = "Thời lượng không được âm")]
        public int ThoiLuong { get; set; }

        [Display(Name = "URL Video")]
        public string VideoUrl { get; set; }

        [Display(Name = "Tài Liệu")]
        public IFormFile DocumentFile { get; set; }

        public int CourseID { get; set; }
    }

    public class QuizViewModel
    {
        public int QuizID { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
        [Display(Name = "Tiêu Đề")]
        public string Title { get; set; }

        public int CourseID { get; set; }

        public List<QuestionViewModel> Questions { get; set; } = new List<QuestionViewModel>();
    }

    public class QuestionViewModel
    {
        public int QuestionID { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập câu hỏi")]
        [Display(Name = "Câu Hỏi")]
        public string QuestionText { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập đáp án A")]
        [Display(Name = "Đáp Án A")]
        public string OptionA { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập đáp án B")]
        [Display(Name = "Đáp Án B")]
        public string OptionB { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập đáp án C")]
        [Display(Name = "Đáp Án C")]
        public string OptionC { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập đáp án D")]
        [Display(Name = "Đáp Án D")]
        public string OptionD { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn đáp án đúng")]
        [Display(Name = "Đáp Án Đúng")]
        public string CorrectAnswer { get; set; }
    }

    public class EnrollmentViewModel
    {
        public int EnrollmentID { get; set; }

        [Display(Name = "Tên Người Dùng")]
        public string UserName { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Họ Tên")]
        public string HoTen { get; set; }

        [Display(Name = "Ngày Ghi Danh")]
        public DateTime EnrollDate { get; set; }

        [Display(Name = "Trạng Thái")]
        public string TrangThai { get; set; }

        [Display(Name = "Tiến Độ (%)")]
        [Range(0, 100, ErrorMessage = "Tiến độ phải từ 0 đến 100")]
        public float Progress { get; set; }
    }

    public class CourseFollowViewModel
    {
        [Display(Name = "Tên Người Dùng")]
        public string UserName { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Họ Tên")]
        public string HoTen { get; set; }

        [Display(Name = "Ngày Theo Dõi")]
        public DateTime FollowDate { get; set; }
    }
}