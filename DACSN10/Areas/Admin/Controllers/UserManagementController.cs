using DACSN10.Areas.Admin.Models;
using DACSN10.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace DACSN10.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UserManagementController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;

        public UserManagementController(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            AppDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Sử dụng Join để lấy dữ liệu user và role trong 1 truy vấn
            var usersWithRoles = await (
                from user in _userManager.Users
                select new UserViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    HoTen = user.HoTen,
                    LoaiNguoiDung = user.LoaiNguoiDung,
                    TrangThai = user.TrangThai
                }).ToListAsync();

            return View(usersWithRoles);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Loại bỏ AllRoles, chỉ sử dụng RoleNames
            var model = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                HoTen = user.HoTen,
                LoaiNguoiDung = user.LoaiNguoiDung, // Sử dụng trường này thay cho roles
                TrangThai = user.TrangThai
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            // Cập nhật thông tin cơ bản
            user.HoTen = model.HoTen;
            user.LoaiNguoiDung = model.LoaiNguoiDung;
            user.TrangThai = model.TrangThai;

            // Xử lý role: Đồng bộ LoaiNguoiDung với Role
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, model.LoaiNguoiDung); // Thêm role tương ứng

            await _userManager.UpdateAsync(user);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = "Xóa người dùng thất bại";
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = "Xóa người dùng thành công";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ManageRoles(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var model = new ManageRolesViewModel
            {
                UserId = user.Id,
                Email = user.Email,
                CurrentRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? RoleNames.User
            };

            ViewBag.AllRoles = _roleManager.Roles.Select(r => r.Name).ToList();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ManageRoles(ManageRolesViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            // Cập nhật LoaiNguoiDung và Role
            user.LoaiNguoiDung = model.SelectedRole;
            await _userManager.UpdateAsync(user);

            // Xóa tất cả roles hiện tại
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            // Thêm role mới
            await _userManager.AddToRoleAsync(user, model.SelectedRole);

            TempData["SuccessMessage"] = "Đã cập nhật vai trò thành công";
            return RedirectToAction(nameof(Index));
        }
    }
}