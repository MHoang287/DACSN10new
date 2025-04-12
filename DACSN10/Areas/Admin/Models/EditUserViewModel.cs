using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace DACSN10.Areas.Admin.Models
{
    public class EditUserViewModel
    {
        public string Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Họ và tên")]
        public string HoTen { get; set; }

        [Required]
        [Display(Name = "Loại người dùng")]
        public string LoaiNguoiDung { get; set; } // Sử dụng enum hoặc RoleNames

        [Required]
        [Display(Name = "Trạng thái")]
        public string TrangThai { get; set; }
    }
}
