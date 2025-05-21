using DACSN10.Models;
using System.ComponentModel.DataAnnotations;

namespace DACSN10.Areas.Admin.Models
{
    public class CourseAdminViewModel
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

        [Display(Name = "Giảm Giá (%)")]
        [Range(0, 100, ErrorMessage = "Giảm giá phải từ 0 đến 100%")]
        public decimal GiamGia { get; set; } = 0;

        [Required(ErrorMessage = "Vui lòng chọn trạng thái")]
        [Display(Name = "Trạng Thái")]
        public string TrangThai { get; set; }

        [Display(Name = "Ngày Tạo")]
        public DateTime NgayTao { get; set; }

        [Display(Name = "Giảng Viên")]
        public string TenGiangVien { get; set; }

        [Display(Name = "ID Giảng Viên")]
        public string UserID { get; set; }

        [Display(Name = "Danh Mục")]
        public List<int> SelectedCategories { get; set; } = new List<int>();

        public List<Category> AvailableCategories { get; set; } = new List<Category>();

        public decimal GiaSauGiam => Gia - (Gia * GiamGia / 100);
    }

    public class CourseApprovalViewModel
    {
        public int CourseID { get; set; }
        public string TenKhoaHoc { get; set; }
        public string MoTa { get; set; }
        public decimal Gia { get; set; }
        public string TrangThai { get; set; }
        public DateTime NgayTao { get; set; }
        public string TenGiangVien { get; set; }
        public string UserID { get; set; }
        public List<string> DanhMucList { get; set; } = new List<string>();
        public string LyDoTuChoi { get; set; } // Nếu từ chối khóa học
    }

    public class CourseSearchViewModel
    {
        public string Keyword { get; set; }
        public List<int> CategoryFilter { get; set; } = new List<int>();
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string Status { get; set; } // Trạng thái khóa học
        public string TeacherID { get; set; } // Filter theo giảng viên

        public List<CourseAdminViewModel> SearchResults { get; set; } = new List<CourseAdminViewModel>();
        public List<Category> AvailableCategories { get; set; } = new List<Category>();
        public List<User> AvailableTeachers { get; set; } = new List<User>();
    }
}