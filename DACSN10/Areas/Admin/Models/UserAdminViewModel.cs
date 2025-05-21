using DACSN10.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DACSN10.Areas.Admin.Models
{
    public class UserAdminViewModel
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [Display(Name = "Họ Tên")]
        public string HoTen { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Tên Đăng Nhập")]
        public string UserName { get; set; }

        [Display(Name = "Số Điện Thoại")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Ngày Đăng Ký")]
        public DateTime NgayDangKy { get; set; }

        [Display(Name = "Trạng Thái")]
        public string TrangThai { get; set; } // "Active", "Locked", "Inactive"

        [Display(Name = "Loại Người Dùng")]
        public string LoaiNguoiDung { get; set; } // RoleNames.User, RoleNames.Teacher, RoleNames.Admin

        [Display(Name = "Mật Khẩu Mới")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Xác Nhận Mật Khẩu")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; }

        // Thông tin bổ sung cho giảng viên
        [Display(Name = "Số Khóa Học")]
        public int CourseCount { get; set; }

        [Display(Name = "Số Người Theo Dõi")]
        public int FollowerCount { get; set; }

        // Thông tin bổ sung cho học viên
        [Display(Name = "Số Khóa Học Đã Đăng Ký")]
        public int EnrollmentCount { get; set; }
    }

    public class UserSearchViewModel
    {
        public string Keyword { get; set; }
        public string UserType { get; set; } // "All", "User", "Teacher", "Admin"
        public string Status { get; set; } // "All", "Active", "Locked", "Inactive"

        public List<UserAdminViewModel> SearchResults { get; set; } = new List<UserAdminViewModel>();
        public List<SelectListItem> UserTypes { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> StatusTypes { get; set; } = new List<SelectListItem>();
    }
}