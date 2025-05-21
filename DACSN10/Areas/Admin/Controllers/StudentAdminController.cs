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
    public class StudentAdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;

        public StudentAdminController(AppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Admin/StudentAdmin
        public async Task<IActionResult> Index()
        {
            var students = await _userManager.GetUsersInRoleAsync(RoleNames.User);

            var viewModels = new List<UserAdminViewModel>();
            foreach (var student in students)
            {
                // Đếm số khóa học đã đăng ký
                var enrollmentCount = await _context.Enrollments
                    .CountAsync(e => e.UserID == student.Id);

                viewModels.Add(new UserAdminViewModel
                {
                    Id = student.Id,
                    HoTen = student.HoTen,
                    Email = student.Email,
                    UserName = student.UserName,
                    PhoneNumber = student.PhoneNumber,
                    NgayDangKy = student.NgayDangKy,
                    TrangThai = student.TrangThai,
                    LoaiNguoiDung = student.LoaiNguoiDung,
                    EnrollmentCount = enrollmentCount
                });
            }

            return View(viewModels);
        }

        // GET: /Admin/StudentAdmin/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Admin/StudentAdmin/Create
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

                var user = new User
                {
                    UserName = viewModel.Email,
                    Email = viewModel.Email,
                    HoTen = viewModel.HoTen,
                    PhoneNumber = viewModel.PhoneNumber,
                    NgayDangKy = DateTime.Now,
                    TrangThai = "Active",
                    LoaiNguoiDung = RoleNames.User
                };

                var result = await _userManager.CreateAsync(user, viewModel.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, RoleNames.User);
                    TempData["SuccessMessage"] = "Thêm học viên mới thành công.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            return View(viewModel);
        }

        // GET: /Admin/StudentAdmin/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _userManager.FindByIdAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            // Đếm số khóa học đã đăng ký
            var enrollmentCount = await _context.Enrollments
                .CountAsync(e => e.UserID == student.Id);

            var viewModel = new UserAdminViewModel
            {
                Id = student.Id,
                HoTen = student.HoTen,
                Email = student.Email,
                UserName = student.UserName,
                PhoneNumber = student.PhoneNumber,
                NgayDangKy = student.NgayDangKy,
                TrangThai = student.TrangThai,
                LoaiNguoiDung = student.LoaiNguoiDung,
                EnrollmentCount = enrollmentCount
            };

            return View(viewModel);
        }

        // POST: /Admin/StudentAdmin/Edit/5
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
                var student = await _userManager.FindByIdAsync(id);
                if (student == null)
                {
                    return NotFound();
                }

                // Kiểm tra email có trùng với người dùng khác không
                if (student.Email != viewModel.Email)
                {
                    var existingUser = await _userManager.FindByEmailAsync(viewModel.Email);
                    if (existingUser != null && existingUser.Id != id)
                    {
                        ModelState.AddModelError("Email", "Email này đã được sử dụng.");
                        return View(viewModel);
                    }

                    student.Email = viewModel.Email;
                    student.UserName = viewModel.Email; // Đồng bộ username với email
                }

                student.HoTen = viewModel.HoTen;
                student.PhoneNumber = viewModel.PhoneNumber;
                student.TrangThai = viewModel.TrangThai;

                var result = await _userManager.UpdateAsync(student);

                if (result.Succeeded)
                {
                    // Cập nhật mật khẩu nếu có
                    if (!string.IsNullOrEmpty(viewModel.Password))
                    {
                        var token = await _userManager.GeneratePasswordResetTokenAsync(student);
                        var passwordResult = await _userManager.ResetPasswordAsync(student, token, viewModel.Password);

                        if (!passwordResult.Succeeded)
                        {
                            foreach (var error in passwordResult.Errors)
                            {
                                ModelState.AddModelError("", error.Description);
                            }
                            return View(viewModel);
                        }
                    }

                    TempData["SuccessMessage"] = "Cập nhật thông tin học viên thành công.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            return View(viewModel);
        }

        // GET: /Admin/StudentAdmin/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _userManager.FindByIdAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            // Lấy danh sách khóa học đã đăng ký
            var enrollments = await _context.Enrollments
                .Include(e => e.Course)
                .Where(e => e.UserID == id)
                .ToListAsync();

            // Lấy danh sách thanh toán
            var payments = await _context.Payments
                .Include(p => p.Course)
                .Where(p => p.UserID == id)
                .ToListAsync();

            // Lấy danh sách khóa học yêu thích
            var favoriteCourses = await _context.FavoriteCourses
                .Include(fc => fc.Course)
                .Where(fc => fc.UserID == id)
                .ToListAsync();

            // Lấy danh sách giảng viên đang theo dõi
            var followings = await _context.Follows
                .Include(f => f.FollowedTeacher)
                .Where(f => f.FollowerID == id)
                .ToListAsync();

            var viewModel = new UserAdminViewModel
            {
                Id = student.Id,
                HoTen = student.HoTen,
                Email = student.Email,
                UserName = student.UserName,
                PhoneNumber = student.PhoneNumber,
                NgayDangKy = student.NgayDangKy,
                TrangThai = student.TrangThai,
                LoaiNguoiDung = student.LoaiNguoiDung,
                EnrollmentCount = enrollments.Count
            };

            ViewBag.Enrollments = enrollments;
            ViewBag.Payments = payments;
            ViewBag.FavoriteCourses = favoriteCourses;
            ViewBag.Followings = followings;

            return View(viewModel);
        }

        // GET: /Admin/StudentAdmin/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _userManager.FindByIdAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            // Đếm số khóa học đã đăng ký
            var enrollmentCount = await _context.Enrollments
                .CountAsync(e => e.UserID == student.Id);

            var viewModel = new UserAdminViewModel
            {
                Id = student.Id,
                HoTen = student.HoTen,
                Email = student.Email,
                UserName = student.UserName,
                PhoneNumber = student.PhoneNumber,
                NgayDangKy = student.NgayDangKy,
                TrangThai = student.TrangThai,
                LoaiNguoiDung = student.LoaiNguoiDung,
                EnrollmentCount = enrollmentCount
            };

            return View(viewModel);
        }

        // POST: /Admin/StudentAdmin/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var student = await _userManager.FindByIdAsync(id);
            if (student != null)
            {
                // Kiểm tra xem có dữ liệu liên quan không
                var hasEnrollments = await _context.Enrollments.AnyAsync(e => e.UserID == id);
                var hasPayments = await _context.Payments.AnyAsync(p => p.UserID == id);

                if (hasEnrollments || hasPayments)
                {
                    TempData["ErrorMessage"] = "Không thể xóa học viên này vì còn dữ liệu liên quan (khóa học đã đăng ký, thanh toán).";
                    return RedirectToAction(nameof(Index));
                }

                var result = await _userManager.DeleteAsync(student);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Xóa học viên thành công.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể xóa học viên. Vui lòng thử lại.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy học viên cần xóa.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/StudentAdmin/LockAccount/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockAccount(string id)
        {
            var student = await _userManager.FindByIdAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            // Khóa tài khoản
            student.TrangThai = "Locked";
            var result = await _userManager.UpdateAsync(student);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Khóa tài khoản học viên thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể khóa tài khoản. Vui lòng thử lại.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/StudentAdmin/UnlockAccount/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockAccount(string id)
        {
            var student = await _userManager.FindByIdAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            // Mở khóa tài khoản
            student.TrangThai = "Active";
            var result = await _userManager.UpdateAsync(student);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Mở khóa tài khoản học viên thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể mở khóa tài khoản. Vui lòng thử lại.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/StudentAdmin/PromoteToTeacher/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PromoteToTeacher(string id)
        {
            var student = await _userManager.FindByIdAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            // Cấp quyền giảng viên
            student.LoaiNguoiDung = RoleNames.Teacher;
            await _userManager.UpdateAsync(student);

            // Cập nhật role
            await _userManager.RemoveFromRoleAsync(student, RoleNames.User);
            await _userManager.AddToRoleAsync(student, RoleNames.Teacher);

            TempData["SuccessMessage"] = "Cấp quyền giảng viên thành công.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/StudentAdmin/Search
        public IActionResult Search()
        {
            var viewModel = new UserSearchViewModel
            {
                UserType = "User",
                UserTypes = new List<SelectListItem>
                {
                    new SelectListItem { Value = "User", Text = "Học Viên" }
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

        // POST: /Admin/StudentAdmin/Search
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Search(UserSearchViewModel viewModel)
        {
            // Lấy tất cả người dùng có vai trò học viên
            var students = await _userManager.GetUsersInRoleAsync(RoleNames.User);
            var filteredStudents = students.AsQueryable();

            // Lọc theo từ khóa
            if (!string.IsNullOrEmpty(viewModel.Keyword))
            {
                filteredStudents = filteredStudents.Where(s =>
                    s.HoTen.Contains(viewModel.Keyword) ||
                    s.Email.Contains(viewModel.Keyword) ||
                    s.UserName.Contains(viewModel.Keyword) ||
                    (s.PhoneNumber != null && s.PhoneNumber.Contains(viewModel.Keyword))
                );
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(viewModel.Status))
            {
                filteredStudents = filteredStudents.Where(s => s.TrangThai == viewModel.Status);
            }

            // Tạo danh sách học viên với thông tin chi tiết
            var studentList = new List<UserAdminViewModel>();
            foreach (var student in filteredStudents)
            {
                // Đếm số khóa học đã đăng ký
                var enrollmentCount = await _context.Enrollments
                    .CountAsync(e => e.UserID == student.Id);

                studentList.Add(new UserAdminViewModel
                {
                    Id = student.Id,
                    HoTen = student.HoTen,
                    Email = student.Email,
                    UserName = student.UserName,
                    PhoneNumber = student.PhoneNumber,
                    NgayDangKy = student.NgayDangKy,
                    TrangThai = student.TrangThai,
                    LoaiNguoiDung = student.LoaiNguoiDung,
                    EnrollmentCount = enrollmentCount
                });
            }

            viewModel.SearchResults = studentList;
            viewModel.UserTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = "User", Text = "Học Viên" }
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