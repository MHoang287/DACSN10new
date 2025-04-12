namespace DACSN10.Areas.Admin.Models
{
    public class UserViewModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string HoTen { get; set; }
        public string LoaiNguoiDung { get; set; } // Hiển thị thay cho Roles
        public string TrangThai { get; set; }
    }
}
