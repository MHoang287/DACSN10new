using DACSN10.Areas.Admin.Models;
using DACSN10.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DACSN10.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = RoleNames.Admin)]
    public class TeacherAdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;

        public TeacherAdminController(AppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Admin/TeacherAdmin
        public async Task<IActionResult> Index()
        {
            var teachers = await _userManager.GetUsersInRoleAsync(RoleNames.Teacher);

            var viewModels = new List<UserAdminViewModel>();
            foreach (var teacher in teachers)
            {
                // Đếm số khóa học
                var courseCount = await _context.Courses
                    .CountAsync(c => c.UserID == teacher.Id);

                // Đếm số người theo dõi
                var followerCount = await _context.Follows
                    .CountAsync(f => f.FollowedTeacherID == teacher.Id);

                viewModels.Add(new UserAdminViewModel
                {
                    Id = teacher.Id,
                    HoTen = teacher.HoTen,
                    Email = teacher.Email,
                    UserName = teacher.UserName,
                    PhoneNumber = teacher.PhoneNumber,
                    NgayDangKy = teacher.NgayDangKy,
                    TrangThai = teacher.TrangThai,
                    LoaiNguoiDung = teacher.LoaiNguoiDung,
                    CourseCount = courseCount,
                    FollowerCount = followerCount
                });
            }

            return View(viewModels);
        }

        // GET: /Admin/TeacherAdmin/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Admin/TeacherAdmin/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserAdminViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra email đã tồn tại chưa
                var existingUser = await _userManager.FindByEmailAsync(viewModel.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng.");
                    return View(viewModel);
                }

                var teacher = new User
                {
                    UserName = viewModel.Email,
                    Email = viewModel.Email,
                    HoTen = viewModel.HoTen,
                    PhoneNumber = viewModel.PhoneNumber,
                    NgayDangKy = DateTime.Now,
                    TrangThai = "Active",
                    LoaiNguoiDung = RoleNames.Teacher
                };

                var result = await _userManager.CreateAsync(teacher, viewModel.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(teacher, RoleNames.Teacher);
                    TempData["SuccessMessage"] = "Thêm giảng viên mới thành công.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            return View(viewModel);
        }

        // GET: /Admin/TeacherAdmin/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teacher = await _userManager.FindByIdAsync(id);
            if (teacher == null)
            {
                return NotFound();
            }

            // Đếm số khóa học
            var courseCount = await _context.Courses
                .CountAsync(c => c.UserID == teacher.Id);

            // Đếm số người theo dõi
            var followerCount = await _context.Follows
                .CountAsync(f => f.FollowedTeacherID == teacher.Id);

            var viewModel = new UserAdminViewModel
            {
                Id = teacher.Id,
                HoTen = teacher.HoTen,
                Email = teacher.Email,
                UserName = teacher.UserName,
                PhoneNumber = teacher.PhoneNumber,
                NgayDangKy = teacher.NgayDangKy,
                TrangThai = teacher.TrangThai,
                LoaiNguoiDung = teacher.LoaiNguoiDung,
                CourseCount = courseCount,
                FollowerCount = followerCount
            };

            return View(viewModel);
        }

        // POST: /Admin/TeacherAdmin/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UserAdminViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var teacher = await _userManager.FindByIdAsync(id);
                if (teacher == null)
                {
                    return NotFound();
                }

                // Kiểm tra email có trùng với người dùng khác không
                if (teacher.Email != viewModel.Email)
                {
                    var existingUser = await _userManager.FindByEmailAsync(viewModel.Email);
                    if (existingUser != null && existingUser.Id != id)
                    {
                        ModelState.AddModelError("Email", "Email này đã được sử dụng.");
                        return View(viewModel);
                    }

                    teacher.Email = viewModel.Email;
                    teacher.UserName = viewModel.Email; // Đồng bộ username với email
                }

                teacher.HoTen = viewModel.HoTen;
                teacher.PhoneNumber = viewModel.PhoneNumber;
                teacher.TrangThai = viewModel.TrangThai;

                var result = await _userManager.UpdateAsync(teacher);

                if (result.Succeeded)
                {
                    // Cập nhật mật khẩu nếu có
                    if (!string.IsNullOrEmpty(viewModel.Password))
                    {
                        var token = await _userManager.GeneratePasswordResetTokenAsync(teacher);
                        var passwordResult = await _userManager.ResetPasswordAsync(teacher, token, viewModel.Password);

                        if (!passwordResult.Succeeded)
                        {
                            foreach (var error in passwordResult.Errors)
                            {
                                ModelState.AddModelError("", error.Description);
                            }
                            return View(viewModel);
                        }
                    }

                    TempData["SuccessMessage"] = "Cập nhật thông tin giảng viên thành công.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            return View(viewModel);
        }

        // GET: /Admin/TeacherAdmin/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teacher = await _userManager.FindByIdAsync(id);
            if (teacher == null)
            {
                return NotFound();
            }

            // Lấy danh sách khóa học của giảng viên
            var courses = await _context.Courses
                .Where(c => c.UserID == id)
                .ToListAsync();

            // Đếm số người học đăng ký khóa học của giảng viên
            var studentCount = await _context.Enrollments
                .Where(e => courses.Select(c => c.CourseID).Contains(e.CourseID))
                .Select(e => e.UserID)
                .Distinct()
                .CountAsync();

            // Lấy danh sách người theo dõi
            var followers = await _context.Follows
                .Include(f => f.Follower)
                .Where(f => f.FollowedTeacherID == id)
                .ToListAsync();

            var viewModel = new UserAdminViewModel
            {
                Id = teacher.Id,
                HoTen = teacher.HoTen,
                Email = teacher.Email,
                UserName = teacher.UserName,
                PhoneNumber = teacher.PhoneNumber,
                NgayDangKy = teacher.NgayDangKy,
                TrangThai = teacher.TrangThai,
                LoaiNguoiDung = teacher.LoaiNguoiDung,
                CourseCount = courses.Count,
                FollowerCount = followers.Count
            };

            ViewBag.Courses = courses;
            ViewBag.StudentCount = studentCount;
            ViewBag.Followers = followers;

            return View(viewModel);
        }

        // GET: /Admin/TeacherAdmin/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teacher = await _userManager.FindByIdAsync(id);
            if (teacher == null)
            {
                return NotFound();
            }

            // Đếm số khóa học
            var courseCount = await _context.Courses
                .CountAsync(c => c.UserID == teacher.Id);

            // Đếm số người theo dõi
            var followerCount = await _context.Follows
                .CountAsync(f => f.FollowedTeacherID == teacher.Id);

            var viewModel = new UserAdminViewModel
            {
                Id = teacher.Id,
                HoTen = teacher.HoTen,
                Email = teacher.Email,
                UserName = teacher.UserName,
                PhoneNumber = teacher.PhoneNumber,
                NgayDangKy = teacher.NgayDangKy,
                TrangThai = teacher.TrangThai,
                LoaiNguoiDung = teacher.LoaiNguoiDung,
                CourseCount = courseCount,
                FollowerCount = followerCount
            };

            return View(viewModel);
        }

        // POST: /Admin/TeacherAdmin/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var teacher = await _userManager.FindByIdAsync(id);
            if (teacher != null)
            {
                // Kiểm tra xem có khóa học không
                var hasCourses = await _context.Courses.AnyAsync(c => c.UserID == id);

                if (hasCourses)
                {
                    TempData["ErrorMessage"] = "Không thể xóa giảng viên này vì có khóa học liên quan.";
                    return RedirectToAction(nameof(Index));
                }

                var result = await _userManager.DeleteAsync(teacher);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Xóa giảng viên thành công.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể xóa giảng viên. Vui lòng thử lại.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy giảng viên cần xóa.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/TeacherAdmin/LockAccount/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockAccount(string id)
        {
            var teacher = await _userManager.FindByIdAsync(id);
            if (teacher == null)
            {
                return NotFound();
            }

            // Khóa tài khoản
            teacher.TrangThai = "Locked";
            var result = await _userManager.UpdateAsync(teacher);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Khóa tài khoản giảng viên thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể khóa tài khoản. Vui lòng thử lại.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/TeacherAdmin/UnlockAccount/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockAccount(string id)
        {
            var teacher = await _userManager.FindByIdAsync(id);
            if (teacher == null)
            {
                return NotFound();
            }

            // Mở khóa tài khoản
            teacher.TrangThai = "Active";
            var result = await _userManager.UpdateAsync(teacher);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Mở khóa tài khoản giảng viên thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể mở khóa tài khoản. Vui lòng thử lại.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/TeacherAdmin/Search
        public IActionResult Search()
        {
            var viewModel = new UserSearchViewModel
            {
                UserType = "Teacher",
                UserTypes = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Teacher", Text = "Giảng Viên" }
                },
                StatusTypes = new List<SelectListItem>
                {
                    new SelectListItem { Value = "", Text = "Tất Cả" },
                    new SelectListItem { Value = "Active", Text = "Đang Hoạt Động" },
                    new SelectListItem { Value = "Locked", Text = "Đã Khóa" },
                    new SelectListItem { Value = "Inactive", Text = "Không Hoạt Động" }
                }
            };

            return View(viewModel);
        }

        // POST: /Admin/TeacherAdmin/Search
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Search(UserSearchViewModel viewModel)
        {
            // Lấy tất cả người dùng có vai trò giảng viên
            var teachers = await _userManager.GetUsersInRoleAsync(RoleNames.Teacher);
            var filteredTeachers = teachers.AsQueryable();

            // Lọc theo từ khóa
            if (!string.IsNullOrEmpty(viewModel.Keyword))
            {
                filteredTeachers = filteredTeachers.Where(t =>
                    t.HoTen.Contains(viewModel.Keyword) ||
                    t.Email.Contains(viewModel.Keyword) ||
                    t.UserName.Contains(viewModel.Keyword) ||
                    (t.PhoneNumber != null && t.PhoneNumber.Contains(viewModel.Keyword))
                );
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(viewModel.Status))
            {
                filteredTeachers = filteredTeachers.Where(t => t.TrangThai == viewModel.Status);
            }

            // Tạo danh sách giảng viên với thông tin chi tiết
            var teacherList = new List<UserAdminViewModel>();
            foreach (var teacher in filteredTeachers)
            {
                // Đếm số khóa học
                var courseCount = await _context.Courses
                    .CountAsync(c => c.UserID == teacher.Id);

                // Đếm số người theo dõi
                var followerCount = await _context.Follows
                    .CountAsync(f => f.FollowedTeacherID == teacher.Id);

                teacherList.Add(new UserAdminViewModel
                {
                    Id = teacher.Id,
                    HoTen = teacher.HoTen,
                    Email = teacher.Email,
                    UserName = teacher.UserName,
                    PhoneNumber = teacher.PhoneNumber,
                    NgayDangKy = teacher.NgayDangKy,
                    TrangThai = teacher.TrangThai,
                    LoaiNguoiDung = teacher.LoaiNguoiDung,
                    CourseCount = courseCount,
                    FollowerCount = followerCount
                });
            }

            viewModel.SearchResults = teacherList;
            viewModel.UserTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = "Teacher", Text = "Giảng Viên" }
            };
            viewModel.StatusTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Tất Cả" },
                new SelectListItem { Value = "Active", Text = "Đang Hoạt Động" },
                new SelectListItem { Value = "Locked", Text = "Đã Khóa" },
                new SelectListItem { Value = "Inactive", Text = "Không Hoạt Động" }
            };

            return View(viewModel);
        }
    }
}