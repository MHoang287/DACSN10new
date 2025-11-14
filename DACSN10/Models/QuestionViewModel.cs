using System.ComponentModel.DataAnnotations;

namespace DACSN10.Models
{
    /// <summary>
    /// ViewModel để nhận dữ liệu câu hỏi từ form khi tạo/chỉnh sửa quiz
    /// </summary>
    /// <summary>
    /// ViewModel để nhận dữ liệu câu hỏi từ form khi tạo/chỉnh sửa quiz
    /// </summary>
    public class QuestionViewModel
    {
        [Required(ErrorMessage = "Nội dung câu hỏi là bắt buộc")]
        public string QuestionText { get; set; }

        [Required(ErrorMessage = "Đáp án A là bắt buộc")]
        public string OptionA { get; set; }

        [Required(ErrorMessage = "Đáp án B là bắt buộc")]
        public string OptionB { get; set; }

        public string OptionC { get; set; }

        public string OptionD { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn đáp án đúng")]
        public string CorrectAnswer { get; set; }
    }
}