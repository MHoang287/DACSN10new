using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DACSN10.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json;
using OfficeOpenXml;

namespace DACSN10.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = RoleNames.Admin)]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<AdminController> _logger;

        public AdminController(AppDbContext context, UserManager<User> userManager, ILogger<AdminController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        #region Dashboard

        public async Task<IActionResult> Dashboard()
        {
            var stats = await GetDashboardStats();
            ViewBag.Stats = stats;
            return View(stats);
        }

        private async Task<dynamic> GetDashboardStats()
        {
            var totalUsers = await _userManager.Users.CountAsync();
            var totalTeachers = await _userManager.Users.CountAsync(u => u.LoaiNguoiDung == RoleNames.Teacher);
            var totalStudents = await _userManager.Users.CountAsync(u => u.LoaiNguoiDung == RoleNames.User);
            var totalCourses = await _context.Courses.CountAsync();
            var activeCourses = await _context.Courses.CountAsync(c => c.TrangThai == "Active");
            var pendingCourses = await _context.Courses.CountAsync(c => c.TrangThai == "Pending");
            var totalEnrollments = await _context.Enrollments.CountAsync();
            var totalRevenue = await _context.Payments
                .Where(p => p.Status == PaymentStatus.Success)
                .SumAsync(p => (decimal?)p.SoTien) ?? 0;
            var pendingPayments = await _context.Payments.CountAsync(p => p.Status == PaymentStatus.Pending);

            return new
            {
                TotalUsers = totalUsers,
                TotalTeachers = totalTeachers,
                TotalStudents = totalStudents,
                TotalCourses = totalCourses,
                ActiveCourses = activeCourses,
                PendingCourses = pendingCourses,
                TotalEnrollments = totalEnrollments,
                TotalRevenue = totalRevenue,
                PendingPayments = pendingPayments
            };
        }

        #endregion

        #region Statistics (Admin)

        [HttpGet]
        public async Task<IActionResult> Statistics()
        {
            var totalRevenue = await _context.Payments
                .Where(p => p.Status == PaymentStatus.Success)
                .SumAsync(p => (decimal?)p.SoTien) ?? 0;

            var totalEnrollments = await _context.Enrollments.CountAsync();
            var activeUsers = await _userManager.Users.CountAsync(u => u.TrangThai == "Active");

            var totalEnrolls = await _context.Enrollments.CountAsync();
            double completionRate = 0;
            if (totalEnrolls > 0)
            {
                var completed = await _context.Enrollments.CountAsync(e => e.Progress >= 100);
                completionRate = completed * 100.0 / totalEnrolls;
            }

            var model = new
            {
                TotalRevenue = totalRevenue,
                TotalEnrollments = totalEnrollments,
                ActiveUsers = activeUsers,
                CompletionRate = completionRate
            };

            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalEnrollments = totalEnrollments;
            ViewBag.ActiveUsers = activeUsers;
            ViewBag.CompletionRate = completionRate;

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetStatistics(string period, DateTime? startDate, DateTime? endDate)
        {
            DateTime from, to;
            var now = DateTime.Now;

            switch (period)
            {
                case "7days":
                    to = now;
                    from = now.AddDays(-7);
                    break;
                case "30days":
                    to = now;
                    from = now.AddDays(-30);
                    break;
                case "90days":
                    to = now;
                    from = now.AddDays(-90);
                    break;
                case "1year":
                    to = now;
                    from = now.AddYears(-1);
                    break;
                case "custom":
                    from = startDate ?? now.AddDays(-30);
                    to = endDate ?? now;
                    break;
                default:
                    to = now;
                    from = now.AddDays(-30);
                    break;
            }

            var payments = await _context.Payments
                .Where(p => p.Status == PaymentStatus.Success &&
                            p.NgayThanhToan >= from && p.NgayThanhToan <= to)
                .ToListAsync();

            var revenue = payments.Sum(p => p.SoTien);

            var enrollments = await _context.Enrollments
                .Where(e => e.EnrollDate >= from && e.EnrollDate <= to)
                .CountAsync();

            var activeUsers = await _userManager.Users
                .CountAsync(u => u.TrangThai == "Active" &&
                                 u.NgayDangKy <= to &&
                                 u.NgayDangKy >= from);

            var totalEnrolls = await _context.Enrollments
                .Where(e => e.EnrollDate >= from && e.EnrollDate <= to)
                .CountAsync();

            double completionRate = 0;
            if (totalEnrolls > 0)
            {
                var completed = await _context.Enrollments
                    .Where(e => e.EnrollDate >= from && e.EnrollDate <= to && e.Progress >= 100)
                    .CountAsync();
                completionRate = completed * 100.0 / totalEnrolls;
            }

            var dailyRevenue = payments
                .GroupBy(p => p.NgayThanhToan.Date)
                .Select(g => new { Date = g.Key, Revenue = g.Sum(x => x.SoTien) })
                .OrderBy(x => x.Date)
                .ToList();

            return Json(new
            {
                Revenue = revenue,
                Enrollments = enrollments,
                ActiveUsers = activeUsers,
                CompletionRate = completionRate,
                DailyRevenue = new
                {
                    labels = dailyRevenue.Select(d => d.Date.ToString("dd/MM")).ToArray(),
                    data = dailyRevenue.Select(d => d.Revenue).ToArray()
                }
            });
        }

        #endregion

        #region Course Management

        public async Task<IActionResult> Courses(string status = "", string search = "", int page = 1, int pageSize = 20)
        {
            var query = _context.Courses.Include(c => c.User).AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(c => c.TrangThai == status);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c => c.TenKhoaHoc.Contains(search) || c.User.HoTen.Contains(search));
            }

            var totalCourses = await query.CountAsync();
            var courses = await query
                .OrderByDescending(c => c.NgayTao)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Status = status;
            ViewBag.Search = search;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCourses / pageSize);

            return View(courses);
        }

        public async Task<IActionResult> CourseDetails(int id)
        {
            var course = await _context.Courses
                .Include(c => c.User)
                .Include(c => c.Lessons)
                .Include(c => c.Quizzes).ThenInclude(q => q.Questions)
                .Include(c => c.Enrollments).ThenInclude(e => e.User)
                .Include(c => c.CourseCategories).ThenInclude(cc => cc.Category)
                .FirstOrDefaultAsync(c => c.CourseID == id);

            if (course == null)
            {
                TempData["Error"] = "Không tìm thấy khóa học.";
                return NotFound();
            }

            return View(course);
        }

        public async Task<IActionResult> CreateCourse()
        {
            ViewBag.Categories = await GetCategoriesSelectList();
            ViewBag.Teachers = await GetTeachersSelectList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourse(Course course, int[] selectedCategories)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    course.NgayTao = DateTime.Now;
                    course.TrangThai = "Active";

                    _context.Courses.Add(course);
                    await _context.SaveChangesAsync();

                    if (selectedCategories != null && selectedCategories.Length > 0)
                    {
                        foreach (var categoryId in selectedCategories)
                        {
                            _context.CourseCategories.Add(new CourseCategory
                            {
                                CourseID = course.CourseID,
                                CategoryID = categoryId
                            });
                        }
                        await _context.SaveChangesAsync();
                    }

                    TempData["Success"] = "Tạo khóa học thành công!";
                    return RedirectToAction("Courses");
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Lỗi khi tạo khóa học: " + ex.Message;
                }
            }

            ViewBag.Categories = await GetCategoriesSelectList();
            ViewBag.Teachers = await GetTeachersSelectList();
            return View(course);
        }

        public async Task<IActionResult> EditCourse(int id)
        {
            var course = await _context.Courses
                .Include(c => c.CourseCategories)
                .FirstOrDefaultAsync(c => c.CourseID == id);

            if (course == null)
            {
                TempData["Error"] = "Không tìm thấy khóa học.";
                return NotFound();
            }

            ViewBag.Categories = await GetCategoriesSelectList();
            ViewBag.Teachers = await GetTeachersSelectList();
            ViewBag.SelectedCategories = course.CourseCategories.Select(cc => cc.CategoryID).ToArray();

            return View(course);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourse(Course course, int[] selectedCategories, decimal? discountPrice)
        {
            var existingCourse = await _context.Courses
                .Include(c => c.CourseCategories)
                .FirstOrDefaultAsync(c => c.CourseID == course.CourseID);

            if (existingCourse == null)
            {
                TempData["Error"] = "Không tìm thấy khóa học.";
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    existingCourse.TenKhoaHoc = course.TenKhoaHoc;
                    existingCourse.MoTa = course.MoTa;
                    existingCourse.Gia = discountPrice ?? course.Gia;
                    existingCourse.TrangThai = course.TrangThai;
                    existingCourse.UserID = course.UserID;

                    _context.CourseCategories.RemoveRange(existingCourse.CourseCategories);

                    if (selectedCategories != null && selectedCategories.Length > 0)
                    {
                        foreach (var categoryId in selectedCategories)
                        {
                            _context.CourseCategories.Add(new CourseCategory
                            {
                                CourseID = course.CourseID,
                                CategoryID = categoryId
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật khóa học thành công!";
                    return RedirectToAction("Courses");
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Lỗi khi cập nhật: " + ex.Message;
                }
            }

            ViewBag.Categories = await GetCategoriesSelectList();
            ViewBag.Teachers = await GetTeachersSelectList();
            ViewBag.SelectedCategories = selectedCategories;
            return View(course);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.CourseID == id);

            if (course == null)
            {
                return Json(new { success = false, message = "Không tìm thấy khóa học." });
            }

            try
            {
                var courseCategories = await _context.CourseCategories.Where(cc => cc.CourseID == id).ToListAsync();
                _context.CourseCategories.RemoveRange(courseCategories);

                var enrollments = await _context.Enrollments.Where(e => e.CourseID == id).ToListAsync();
                _context.Enrollments.RemoveRange(enrollments);

                var lessons = await _context.Lessons.Where(l => l.CourseID == id).ToListAsync();
                _context.Lessons.RemoveRange(lessons);

                var quizzes = await _context.Quizzes.Include(q => q.Questions).Where(q => q.CourseID == id).ToListAsync();
                foreach (var quiz in quizzes)
                {
                    _context.Questions.RemoveRange(quiz.Questions);
                }
                _context.Quizzes.RemoveRange(quizzes);

                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa khóa học thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi xóa: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveCourse(int id)
        {
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.CourseID == id);

            if (course == null)
            {
                return Json(new { success = false, message = "Không tìm thấy khóa học." });
            }

            try
            {
                course.TrangThai = "Active";
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Duyệt khóa học thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi duyệt: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectCourse(int id, string reason)
        {
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.CourseID == id);

            if (course == null)
            {
                return Json(new { success = false, message = "Không tìm thấy khóa học." });
            }

            try
            {
                course.TrangThai = "Rejected";
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Từ chối khóa học thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi từ chối: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkApproveCourses(int[] courseIds)
        {
            try
            {
                var courses = await _context.Courses.Where(c => courseIds.Contains(c.CourseID)).ToListAsync();
                foreach (var course in courses)
                {
                    course.TrangThai = "Active";
                }
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = $"Đã duyệt {courses.Count} khóa học thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi duyệt hàng loạt: " + ex.Message });
            }
        }

        #endregion

        #region Category Management

        public async Task<IActionResult> Categories(string search = "", int page = 1, int pageSize = 20)
        {
            var query = _context.Categories.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c => c.Name.Contains(search) || c.Description.Contains(search));
            }

            var totalCategories = await query.CountAsync();
            var categories = await query
                .Include(c => c.CourseCategories)
                .OrderBy(c => c.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCategories / pageSize);

            return View(categories);
        }

        public IActionResult CreateCategory()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(Category category)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Categories.Add(category);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Tạo danh mục thành công!";
                    return RedirectToAction("Categories");
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Lỗi khi tạo danh mục: " + ex.Message;
                }
            }

            return View(category);
        }

        public async Task<IActionResult> EditCategory(int id)
        {
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
            {
                TempData["Error"] = "Không tìm thấy danh mục.";
                return NotFound();
            }

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(Category category)
        {
            var existingCategory = await _context.Categories.FirstOrDefaultAsync(c => c.CategoryID == category.CategoryID);

            if (existingCategory == null)
            {
                TempData["Error"] = "Không tìm thấy danh mục.";
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    existingCategory.Name = category.Name;
                    existingCategory.Description = category.Description;

                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật danh mục thành công!";
                    return RedirectToAction("Categories");
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Lỗi khi cập nhật: " + ex.Message;
                }
            }

            return View(category);
        }

        public async Task<IActionResult> CategoryDetails(int id)
        {
            var category = await _context.Categories
                .Include(c => c.CourseCategories).ThenInclude(cc => cc.Course).ThenInclude(c => c.User)
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
            {
                TempData["Error"] = "Không tìm thấy danh mục.";
                return NotFound();
            }

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories
                .Include(c => c.CourseCategories)
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
            {
                return Json(new { success = false, message = "Không tìm thấy danh mục." });
            }

            if (category.CourseCategories.Any())
            {
                return Json(new { success = false, message = "Không thể xóa danh mục đang có khóa học." });
            }

            try
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Xóa danh mục thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi xóa: " + ex.Message });
            }
        }

        #endregion

        #region Student Management

        public async Task<IActionResult> Students(string search = "", string status = "", int page = 1, int pageSize = 20)
        {
            var query = _userManager.Users.Where(u => u.LoaiNguoiDung == RoleNames.User);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u => u.HoTen.Contains(search) || u.Email.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(u => u.TrangThai == status);
            }

            var totalStudents = await query.CountAsync();
            var students = await query
                .OrderByDescending(u => u.NgayDangKy)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalStudents / pageSize);

            return View(students);
        }

        public async Task<IActionResult> StudentDetails(string id)
        {
            var student = await _userManager.Users
                .Include(u => u.Enrollments).ThenInclude(e => e.Course)
                .Include(u => u.Payments)
                .Include(u => u.QuizResults).ThenInclude(qr => qr.Quiz)
                .FirstOrDefaultAsync(u => u.Id == id && u.LoaiNguoiDung == RoleNames.User);

            if (student == null)
            {
                TempData["Error"] = "Không tìm thấy học viên.";
                return NotFound();
            }

            return View(student);
        }

        public IActionResult CreateStudent()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStudent(User user, string password)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    user.UserName = user.Email;
                    user.NgayDangKy = DateTime.Now;
                    user.TrangThai = "Active";
                    user.LoaiNguoiDung = RoleNames.User;

                    var result = await _userManager.CreateAsync(user, password);
                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(user, RoleNames.User);
                        TempData["Success"] = "Tạo tài khoản học viên thành công!";
                        return RedirectToAction("Students");
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }
                    }
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Lỗi khi tạo tài khoản: " + ex.Message;
                }
            }

            return View(user);
        }

        public async Task<IActionResult> EditStudent(string id)
        {
            var student = await _userManager.FindByIdAsync(id);

            if (student == null || student.LoaiNguoiDung != RoleNames.User)
            {
                TempData["Error"] = "Không tìm thấy học viên.";
                return NotFound();
            }

            return View(student);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStudent(User user)
        {
            var existingUser = await _userManager.FindByIdAsync(user.Id);

            if (existingUser == null || existingUser.LoaiNguoiDung != RoleNames.User)
            {
                TempData["Error"] = "Không tìm thấy học viên.";
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    existingUser.HoTen = user.HoTen;
                    existingUser.Email = user.Email;
                    existingUser.UserName = user.Email;
                    existingUser.PhoneNumber = user.PhoneNumber;
                    existingUser.TrangThai = user.TrangThai;

                    var result = await _userManager.UpdateAsync(existingUser);
                    if (result.Succeeded)
                    {
                        TempData["Success"] = "Cập nhật thông tin học viên thành công!";
                        return RedirectToAction("Students");
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }
                    }
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Lỗi khi cập nhật: " + ex.Message;
                }
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStudent(string id)
        {
            var student = await _userManager.FindByIdAsync(id);

            if (student == null || student.LoaiNguoiDung != RoleNames.User)
            {
                return Json(new { success = false, message = "Không tìm thấy học viên." });
            }

            try
            {
                var hasEnrollments = await _context.Enrollments.AnyAsync(e => e.UserID == id);
                if (hasEnrollments)
                {
                    return Json(new { success = false, message = "Không thể xóa học viên đã có đăng ký khóa học." });
                }

                var result = await _userManager.DeleteAsync(student);
                if (result.Succeeded)
                {
                    return Json(new { success = true, message = "Xóa học viên thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Lỗi khi xóa học viên." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi xóa: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockStudent(string id)
        {
            var student = await _userManager.FindByIdAsync(id);

            if (student == null || student.LoaiNguoiDung != RoleNames.User)
            {
                return Json(new { success = false, message = "Không tìm thấy học viên." });
            }

            try
            {
                student.TrangThai = student.TrangThai == "Active" ? "Locked" : "Active";
                var result = await _userManager.UpdateAsync(student);

                if (result.Succeeded)
                {
                    var message = student.TrangThai == "Locked" ? "Khóa tài khoản thành công!" : "Mở khóa tài khoản thành công!";
                    return Json(new { success = true, message = message });
                }
                else
                {
                    return Json(new { success = false, message = "Lỗi khi cập nhật trạng thái." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PromoteToTeacher(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null || user.LoaiNguoiDung != RoleNames.User)
            {
                return Json(new { success = false, message = "Không tìm thấy học viên." });
            }

            try
            {
                await _userManager.RemoveFromRoleAsync(user, RoleNames.User);
                await _userManager.AddToRoleAsync(user, RoleNames.Teacher);

                user.LoaiNguoiDung = RoleNames.Teacher;
                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    return Json(new { success = true, message = "Cấp quyền giảng viên thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Lỗi khi cấp quyền." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        #endregion

        #region Teacher Management

        public async Task<IActionResult> Teachers(string search = "", string status = "", int page = 1, int pageSize = 20)
        {
            var query = _userManager.Users.Where(u => u.LoaiNguoiDung == RoleNames.Teacher);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u => u.HoTen.Contains(search) || u.Email.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(u => u.TrangThai == status);
            }

            var totalTeachers = await query.CountAsync();
            var teachers = await query
                .Include(u => u.Courses).ThenInclude(c => c.Enrollments)
                .Include(u => u.Followers)
                .OrderByDescending(u => u.NgayDangKy)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var revenues = await _context.Payments
                .Where(p => p.Status == PaymentStatus.Success && p.Course.UserID != null)
                .GroupBy(p => p.Course.UserID)
                .ToDictionaryAsync(g => g.Key!, g => g.Sum(x => x.SoTien));

            ViewBag.TeacherRevenue = revenues;

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalTeachers / pageSize);

            return View(teachers);
        }

        public async Task<IActionResult> TeacherDetails(string id)
        {
            var teacher = await _userManager.Users
                .Include(u => u.Courses).ThenInclude(c => c.Enrollments)
                .Include(u => u.Followers)
                .FirstOrDefaultAsync(u => u.Id == id && u.LoaiNguoiDung == RoleNames.Teacher);

            if (teacher == null)
            {
                TempData["Error"] = "Không tìm thấy giảng viên.";
                return NotFound();
            }

            var totalRevenue = await _context.Payments
                .Include(p => p.Course)
                .Where(p => p.Course.UserID == id && p.Status == PaymentStatus.Success)
                .SumAsync(p => (decimal?)p.SoTien) ?? 0;

            ViewBag.TotalRevenue = totalRevenue;

            return View(teacher);
        }

        public IActionResult CreateTeacher()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTeacher(User user, string password)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    user.UserName = user.Email;
                    user.NgayDangKy = DateTime.Now;
                    user.TrangThai = "Active";
                    user.LoaiNguoiDung = RoleNames.Teacher;

                    var result = await _userManager.CreateAsync(user, password);
                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(user, RoleNames.Teacher);
                        TempData["Success"] = "Tạo tài khoản giảng viên thành công!";
                        return RedirectToAction("Teachers");
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }
                    }
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Lỗi khi tạo tài khoản: " + ex.Message;
                }
            }

            return View(user);
        }

        public async Task<IActionResult> EditTeacher(string id)
        {
            var teacher = await _userManager.FindByIdAsync(id);

            if (teacher == null || teacher.LoaiNguoiDung != RoleNames.Teacher)
            {
                TempData["Error"] = "Không tìm thấy giảng viên.";
                return NotFound();
            }

            return View(teacher);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTeacher(User user)
        {
            var existingUser = await _userManager.FindByIdAsync(user.Id);

            if (existingUser == null || existingUser.LoaiNguoiDung != RoleNames.Teacher)
            {
                TempData["Error"] = "Không tìm thấy giảng viên.";
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    existingUser.HoTen = user.HoTen;
                    existingUser.Email = user.Email;
                    existingUser.UserName = user.Email;
                    existingUser.PhoneNumber = user.PhoneNumber;
                    existingUser.TrangThai = user.TrangThai;

                    var result = await _userManager.UpdateAsync(existingUser);
                    if (result.Succeeded)
                    {
                        TempData["Success"] = "Cập nhật thông tin giảng viên thành công!";
                        return RedirectToAction("Teachers");
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }
                    }
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Lỗi khi cập nhật: " + ex.Message;
                }
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTeacher(string id)
        {
            var teacher = await _userManager.FindByIdAsync(id);

            if (teacher == null || teacher.LoaiNguoiDung != RoleNames.Teacher)
            {
                return Json(new { success = false, message = "Không tìm thấy giảng viên." });
            }

            try
            {
                var hasCourses = await _context.Courses.AnyAsync(c => c.UserID == id);
                if (hasCourses)
                {
                    return Json(new { success = false, message = "Không thể xóa giảng viên đã có khóa học." });
                }

                var result = await _userManager.DeleteAsync(teacher);
                if (result.Succeeded)
                {
                    return Json(new { success = true, message = "Xóa giảng viên thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Lỗi khi xóa giảng viên." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi xóa: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockTeacher(string id)
        {
            var teacher = await _userManager.FindByIdAsync(id);

            if (teacher == null || teacher.LoaiNguoiDung != RoleNames.Teacher)
            {
                return Json(new { success = false, message = "Không tìm thấy giảng viên." });
            }

            try
            {
                teacher.TrangThai = teacher.TrangThai == "Active" ? "Locked" : "Active";
                var result = await _userManager.UpdateAsync(teacher);

                if (result.Succeeded)
                {
                    var message = teacher.TrangThai == "Locked" ? "Khóa tài khoản thành công!" : "Mở khóa tài khoản thành công!";
                    return Json(new { success = true, message = message });
                }
                else
                {
                    return Json(new { success = false, message = "Lỗi khi cập nhật trạng thái." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        #endregion

        #region Payment Management

        public async Task<IActionResult> Payments(string status = "", string search = "", DateTime? fromDate = null, DateTime? toDate = null, int page = 1, int pageSize = 20)
        {
            var query = _context.Payments
                .Include(p => p.User)
                .Include(p => p.Course)
                    .ThenInclude(c => c.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PaymentStatus>(status, out var paymentStatus))
            {
                query = query.Where(p => p.Status == paymentStatus);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => p.User.HoTen.Contains(search) ||
                                         p.Course.TenKhoaHoc.Contains(search) ||
                                         p.User.Email.Contains(search));
            }

            if (fromDate.HasValue)
            {
                query = query.Where(p => p.NgayThanhToan >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(p => p.NgayThanhToan <= toDate.Value);
            }

            var totalPayments = await query.CountAsync();
            var payments = await query
                .OrderByDescending(p => p.NgayThanhToan)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Status = status;
            ViewBag.Search = search;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalPayments / pageSize);

            return View(payments);
        }

        [HttpGet]
        public async Task<IActionResult> GetPaymentSummary(string period)
        {
            DateTime from, to;
            var now = DateTime.Now;

            switch (period)
            {
                case "today":
                    from = now.Date;
                    to = now;
                    break;
                case "week":
                    int diff = (7 + (int)now.DayOfWeek - (int)DayOfWeek.Monday) % 7;
                    from = now.AddDays(-1 * diff).Date;
                    to = now;
                    break;
                case "month":
                default:
                    from = new DateTime(now.Year, now.Month, 1);
                    to = now;
                    break;
            }

            var payments = await _context.Payments
                .Where(p => p.NgayThanhToan >= from && p.NgayThanhToan <= to)
                .ToListAsync();

            var success = payments.Where(p => p.Status == PaymentStatus.Success).ToList();
            var pending = payments.Where(p => p.Status == PaymentStatus.Pending || p.Status == PaymentStatus.WaitingConfirm).ToList();

            var totalRevenue = success.Sum(p => p.SoTien);
            var totalTransactions = success.Count;
            var pendingAmount = pending.Sum(p => p.SoTien);
            var average = totalTransactions > 0 ? totalRevenue / totalTransactions : 0;

            return Json(new
            {
                TotalRevenue = totalRevenue,
                TotalTransactions = totalTransactions,
                PendingAmount = pendingAmount,
                AverageTransaction = average
            });
        }

        public async Task<IActionResult> PaymentDetails(int id)
        {
            var payment = await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Course)
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(p => p.PaymentID == id);

            if (payment == null)
            {
                TempData["Error"] = "Không tìm thấy giao dịch.";
                return NotFound();
            }

            return View(payment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApprovePayment(int id)
        {
            try
            {
                var payment = await _context.Payments
                    .Include(p => p.Course)
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.PaymentID == id);

                if (payment == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy giao dịch." });
                }

                if (payment.Status == PaymentStatus.Success)
                {
                    return Json(new { success = false, message = "Giao dịch đã được duyệt trước đó." });
                }

                if (payment.Status == PaymentStatus.Rejected)
                {
                    return Json(new { success = false, message = "Không thể duyệt giao dịch đã bị từ chối." });
                }

                payment.Status = PaymentStatus.Success;

                var existingEnrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e => e.UserID == payment.UserID && e.CourseID == payment.CourseID);

                if (existingEnrollment == null)
                {
                    var enrollment = new Enrollment
                    {
                        UserID = payment.UserID,
                        CourseID = payment.CourseID,
                        EnrollDate = DateTime.Now,
                        TrangThai = "Active",
                        Progress = 0
                    };

                    _context.Enrollments.Add(enrollment);
                }
                else if (existingEnrollment.TrangThai != "Active")
                {
                    existingEnrollment.TrangThai = "Active";
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Duyệt thanh toán thành công!",
                    paymentId = id,
                    newStatus = "Success"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Lỗi khi duyệt thanh toán: " + ex.Message
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectPayment(int id, string reason)
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.PaymentID == id);

            if (payment == null)
            {
                return Json(new { success = false, message = "Không tìm thấy giao dịch." });
            }

            try
            {
                payment.Status = PaymentStatus.Rejected;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Từ chối thanh toán thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi từ chối: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkApprovePayments(int[] paymentIds)
        {
            try
            {
                var payments = await _context.Payments
                    .Include(p => p.Course)
                        .ThenInclude(c => c.User)
                    .Where(p => paymentIds.Contains(p.PaymentID))
                    .ToListAsync();

                foreach (var payment in payments)
                {
                    payment.Status = PaymentStatus.Success;

                    var existingEnrollment = await _context.Enrollments
                        .FirstOrDefaultAsync(e => e.UserID == payment.UserID && e.CourseID == payment.CourseID);

                    if (existingEnrollment == null)
                    {
                        _context.Enrollments.Add(new Enrollment
                        {
                            UserID = payment.UserID,
                            CourseID = payment.CourseID,
                            EnrollDate = DateTime.Now,
                            TrangThai = "Active",
                            Progress = 0
                        });
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = $"Đã duyệt {payments.Count} giao dịch thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi duyệt hàng loạt: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkRejectPayments(int[] paymentIds)
        {
            try
            {
                var payments = await _context.Payments
                    .Where(p => paymentIds.Contains(p.PaymentID))
                    .ToListAsync();

                foreach (var payment in payments)
                {
                    payment.Status = PaymentStatus.Rejected;
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = $"Đã từ chối {payments.Count} giao dịch!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi từ chối hàng loạt: " + ex.Message });
            }
        }

        #endregion

        #region Statistics & Reports

        public async Task<IActionResult> RevenueReport(DateTime? fromDate = null, DateTime? toDate = null)
        {
            fromDate ??= DateTime.Now.AddMonths(-1);
            toDate ??= DateTime.Now;

            var query = _context.Payments
                .Include(p => p.Course).ThenInclude(c => c.User)
                .Include(p => p.User)
                .Where(p => p.Status == PaymentStatus.Success &&
                           p.NgayThanhToan >= fromDate && p.NgayThanhToan <= toDate);

            var payments = await query.ToListAsync();

            var report = new
            {
                FromDate = fromDate,
                ToDate = toDate,
                TotalRevenue = payments.Sum(p => p.SoTien),
                TotalTransactions = payments.Count,
                AverageTransactionValue = payments.Any() ? payments.Average(p => p.SoTien) : 0,
                RevenueByTeacher = payments
                    .GroupBy(p => p.Course.User?.HoTen ?? "Không xác định")
                    .Select(g => new { TeacherName = g.Key, Revenue = g.Sum(p => p.SoTien), Count = g.Count() })
                    .OrderByDescending(x => x.Revenue)
                    .ToList(),
                RevenueByCategory = payments
                    .GroupBy(p => p.Course.TenKhoaHoc)
                    .Select(g => new { CourseName = g.Key, Revenue = g.Sum(p => p.SoTien), Count = g.Count() })
                    .OrderByDescending(x => x.Revenue)
                    .Take(10)
                    .ToList(),
                DailyRevenue = payments
                    .GroupBy(p => p.NgayThanhToan.Date)
                    .Select(g => new { Date = g.Key, Revenue = g.Sum(p => p.SoTien), Count = g.Count() })
                    .OrderBy(x => x.Date)
                    .ToList()
            };

            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;

            return View(report);
        }

        public async Task<IActionResult> ExportRevenueReport(DateTime? fromDate = null, DateTime? toDate = null)
        {
            fromDate ??= DateTime.Now.AddMonths(-1);
            toDate ??= DateTime.Now;

            var payments = await _context.Payments
                .Include(p => p.Course).ThenInclude(c => c.User)
                .Include(p => p.User)
                .Where(p => p.Status == PaymentStatus.Success &&
                           p.NgayThanhToan >= fromDate && p.NgayThanhToan <= toDate)
                .ToListAsync();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Revenue Report");

            worksheet.Cells[1, 1].Value = "Ngày thanh toán";
            worksheet.Cells[1, 2].Value = "Học viên";
            worksheet.Cells[1, 3].Value = "Email";
            worksheet.Cells[1, 4].Value = "Khóa học";
            worksheet.Cells[1, 5].Value = "Giảng viên";
            worksheet.Cells[1, 6].Value = "Số tiền";
            worksheet.Cells[1, 7].Value = "Phương thức";

            for (int i = 0; i < payments.Count; i++)
            {
                var payment = payments[i];
                worksheet.Cells[i + 2, 1].Value = payment.NgayThanhToan.ToString("dd/MM/yyyy");
                worksheet.Cells[i + 2, 2].Value = payment.User?.HoTen ?? "Không xác định";
                worksheet.Cells[i + 2, 3].Value = payment.User?.Email ?? "Không xác định";
                worksheet.Cells[i + 2, 4].Value = payment.Course?.TenKhoaHoc ?? "Không xác định";
                worksheet.Cells[i + 2, 5].Value = payment.Course?.User?.HoTen ?? "Không xác định";
                worksheet.Cells[i + 2, 6].Value = payment.SoTien;
                worksheet.Cells[i + 2, 7].Value = payment.PhuongThucThanhToan ?? "Không xác định";
            }

            worksheet.Cells.AutoFitColumns();

            var fileName = $"RevenueReport_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.xlsx";
            var content = package.GetAsByteArray();

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        #endregion

        #region System Health & Monitoring

        public async Task<IActionResult> SystemHealth()
        {
            var health = await GetSystemHealthData();
            return View(health);
        }

        private async Task<dynamic> GetSystemHealthData()
        {
            var totalUsers = await _userManager.Users.CountAsync();
            var activeUsers = await _userManager.Users.CountAsync(u => u.TrangThai == "Active");
            var lockedUsers = await _userManager.Users.CountAsync(u => u.TrangThai == "Locked");

            var totalCourses = await _context.Courses.CountAsync();
            var activeCourses = await _context.Courses.CountAsync(c => c.TrangThai == "Active");
            var pendingCourses = await _context.Courses.CountAsync(c => c.TrangThai == "Pending");

            var totalPayments = await _context.Payments.CountAsync();
            var pendingPayments = await _context.Payments.CountAsync(p => p.Status == PaymentStatus.Pending);
            var failedPayments = await _context.Payments.CountAsync(p => p.Status == PaymentStatus.Failed);

            var dbSize = await EstimateDatabaseSize();

            var lastBackup = await _context.BackupRecords
                .OrderByDescending(b => b.CreatedAt)
                .FirstOrDefaultAsync();

            return new
            {
                UserStats = new
                {
                    Total = totalUsers,
                    Active = activeUsers,
                    Locked = lockedUsers,
                    ActivePercentage = totalUsers > 0 ? (double)activeUsers / totalUsers * 100 : 0
                },
                CourseStats = new
                {
                    Total = totalCourses,
                    Active = activeCourses,
                    Pending = pendingCourses,
                    ActivePercentage = totalCourses > 0 ? (double)activeCourses / totalCourses * 100 : 0
                },
                PaymentStats = new
                {
                    Total = totalPayments,
                    Pending = pendingPayments,
                    Failed = failedPayments,
                    SuccessPercentage = totalPayments > 0 ? (double)(totalPayments - pendingPayments - failedPayments) / totalPayments * 100 : 0
                },
                SystemInfo = new
                {
                    DatabaseSize = dbSize,
                    LastBackup = lastBackup?.CreatedAt.ToString("dd/MM/yyyy HH:mm") ?? "Chưa có",
                    ServerUptimeSeconds = Environment.TickCount64 / 1000
                }
            };
        }

        private async Task<string> EstimateDatabaseSize()
        {
            try
            {
                var userCount = await _userManager.Users.CountAsync();
                var courseCount = await _context.Courses.CountAsync();
                var enrollmentCount = await _context.Enrollments.CountAsync();
                var paymentCount = await _context.Payments.CountAsync();

                var estimatedSizeKB = (userCount * 2) + (courseCount * 5) + (enrollmentCount * 1) + (paymentCount * 3);

                if (estimatedSizeKB < 1024)
                    return $"{estimatedSizeKB} KB";
                else if (estimatedSizeKB < 1024 * 1024)
                    return $"{estimatedSizeKB / 1024:F1} MB";
                else
                    return $"{estimatedSizeKB / (1024 * 1024):F1} GB";
            }
            catch
            {
                return "Unknown";
            }
        }

        #endregion

        #region Helper Methods

        private async Task<SelectList> GetCategoriesSelectList()
        {
            var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            return new SelectList(categories, "CategoryID", "Name");
        }

        private async Task<SelectList> GetTeachersSelectList()
        {
            var teachers = await _userManager.Users
                .Where(u => u.LoaiNguoiDung == RoleNames.Teacher && u.TrangThai == "Active")
                .OrderBy(u => u.HoTen)
                .ToListAsync();
            return new SelectList(teachers, "Id", "HoTen");
        }

        #endregion

        #region System Settings

        public async Task<IActionResult> Settings()
        {
            var settings = await _context.SystemSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new SystemSettings();
                _context.SystemSettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(SystemSettings model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Dữ liệu không hợp lệ.";
                return View(model);
            }

            try
            {
                var settings = await _context.SystemSettings.FirstOrDefaultAsync();
                if (settings == null)
                {
                    settings = new SystemSettings();
                    _context.SystemSettings.Add(settings);
                }

                settings.SiteName = model.SiteName;
                settings.SiteDescription = model.SiteDescription;
                settings.ContactEmail = model.ContactEmail;
                settings.ContactPhone = model.ContactPhone;
                settings.ContactAddress = model.ContactAddress;
                settings.TimeZone = model.TimeZone;
                settings.DefaultLanguage = model.DefaultLanguage;
                settings.MaintenanceMode = model.MaintenanceMode;
                settings.AllowNewRegistration = model.AllowNewRegistration;

                settings.Theme = model.Theme;
                settings.PrimaryColor = model.PrimaryColor;
                settings.FontFamily = model.FontFamily;
                settings.EnableDarkMode = model.EnableDarkMode;
                settings.EnableAnimations = model.EnableAnimations;

                settings.Require2FAForAdmins = model.Require2FAForAdmins;
                settings.EncryptSession = model.EncryptSession;
                settings.SessionTimeoutMinutes = model.SessionTimeoutMinutes;
                settings.PasswordComplexityLevel = model.PasswordComplexityLevel;
                settings.MaxLoginFailures = model.MaxLoginFailures;
                settings.AccountLockMinutes = model.AccountLockMinutes;
                settings.ForceHttps = model.ForceHttps;

                settings.NotifyNewRegistration = model.NotifyNewRegistration;
                settings.NotifyNewPayment = model.NotifyNewPayment;
                settings.NotifyNewCourse = model.NotifyNewCourse;
                settings.NotifySystemError = model.NotifySystemError;
                settings.BrowserNotifications = model.BrowserNotifications;
                settings.NotificationSound = model.NotificationSound;
                settings.SmtpServer = model.SmtpServer;
                settings.SmtpPort = model.SmtpPort;
                settings.NotificationFromEmail = model.NotificationFromEmail;

                settings.EnableMomo = model.EnableMomo;
                settings.EnableVnPay = model.EnableVnPay;
                settings.EnableZaloPay = model.EnableZaloPay;

                settings.GoogleAnalyticsId = model.GoogleAnalyticsId;
                settings.FacebookPixelId = model.FacebookPixelId;
                settings.HotjarSiteId = model.HotjarSiteId;

                settings.EnableS3 = model.EnableS3;
                settings.EnableGcs = model.EnableGcs;
                settings.EnableSlack = model.EnableSlack;
                settings.EnableDiscord = model.EnableDiscord;

                settings.MainApiKeyMasked = model.MainApiKeyMasked;
                settings.WebhookApiKeyMasked = model.WebhookApiKeyMasked;

                settings.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Cập nhật cài đặt thành công!";
                return RedirectToAction(nameof(Settings));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating system settings");
                TempData["Error"] = "Lỗi khi cập nhật cài đặt: " + ex.Message;
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleMaintenanceMode(bool enable)
        {
            try
            {
                var settings = await _context.SystemSettings.FirstOrDefaultAsync();
                if (settings == null)
                {
                    settings = new SystemSettings();
                    _context.SystemSettings.Add(settings);
                }

                settings.MaintenanceMode = enable;
                settings.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var message = enable ? "Đã kích hoạt chế độ bảo trì." : "Đã tắt chế độ bảo trì.";
                return Json(new { success = true, message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling maintenance mode");
                return Json(new { success = false, message = "Lỗi khi cập nhật chế độ bảo trì: " + ex.Message });
            }
        }

        #endregion

        #region Advanced Search & Filters

        private async Task<(List<Payment>, int)> SearchPayments(string keyword, string status, DateTime? fromDate, DateTime? toDate, int page, int pageSize)
        {
            var query = _context.Payments
                .Include(p => p.User)
                .Include(p => p.Course)
                    .ThenInclude(c => c.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(p => p.User.HoTen.Contains(keyword) ||
                                        p.User.Email.Contains(keyword) ||
                                        p.Course.TenKhoaHoc.Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PaymentStatus>(status, out var paymentStatus))
            {
                query = query.Where(p => p.Status == paymentStatus);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(p => p.NgayThanhToan >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(p => p.NgayThanhToan <= toDate.Value);
            }

            var totalCount = await query.CountAsync();
            var results = await query
                .OrderByDescending(p => p.NgayThanhToan)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (results, totalCount);
        }

        #endregion

        #region Bulk Operations

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDeleteCourses(int[] courseIds)
        {
            try
            {
                var courses = await _context.Courses.Where(c => courseIds.Contains(c.CourseID)).ToListAsync();

                foreach (var course in courses)
                {
                    var courseCategories = await _context.CourseCategories.Where(cc => cc.CourseID == course.CourseID).ToListAsync();
                    _context.CourseCategories.RemoveRange(courseCategories);

                    var enrollments = await _context.Enrollments.Where(e => e.CourseID == course.CourseID).ToListAsync();
                    _context.Enrollments.RemoveRange(enrollments);

                    var lessons = await _context.Lessons.Where(l => l.CourseID == course.CourseID).ToListAsync();
                    _context.Lessons.RemoveRange(lessons);

                    var quizzes = await _context.Quizzes.Include(q => q.Questions).Where(q => q.CourseID == course.CourseID).ToListAsync();
                    foreach (var quiz in quizzes)
                    {
                        _context.Questions.RemoveRange(quiz.Questions);
                    }
                    _context.Quizzes.RemoveRange(quizzes);
                }

                _context.Courses.RemoveRange(courses);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Đã xóa {courses.Count} khóa học thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi xóa hàng loạt: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkLockUsers(string[] userIds, bool lockStatus)
        {
            try
            {
                var users = await _userManager.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
                var action = lockStatus ? "Locked" : "Active";

                foreach (var user in users)
                {
                    user.TrangThai = action;
                    await _userManager.UpdateAsync(user);
                }

                var message = lockStatus ? $"Đã khóa {users.Count} tài khoản!" : $"Đã mở khóa {users.Count} tài khoản!";
                return Json(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi cập nhật hàng loạt: " + ex.Message });
            }
        }

        #endregion

        #region Export Functions

        public async Task<IActionResult> ExportPayments(string status = "", DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.Payments
                .Include(p => p.User)
                .Include(p => p.Course)
                    .ThenInclude(c => c.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PaymentStatus>(status, out var paymentStatus))
            {
                query = query.Where(p => p.Status == paymentStatus);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(p => p.NgayThanhToan >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(p => p.NgayThanhToan <= toDate.Value);
            }

            var payments = await query.OrderByDescending(p => p.NgayThanhToan).ToListAsync();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Payments");

            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "Ngày thanh toán";
            worksheet.Cells[1, 3].Value = "Học viên";
            worksheet.Cells[1, 4].Value = "Email";
            worksheet.Cells[1, 5].Value = "Khóa học";
            worksheet.Cells[1, 6].Value = "Giảng viên";
            worksheet.Cells[1, 7].Value = "Số tiền";
            worksheet.Cells[1, 8].Value = "Phương thức";
            worksheet.Cells[1, 9].Value = "Trạng thái";

            for (int i = 0; i < payments.Count; i++)
            {
                var payment = payments[i];
                worksheet.Cells[i + 2, 1].Value = payment.PaymentID;
                worksheet.Cells[i + 2, 2].Value = payment.NgayThanhToan.ToString("dd/MM/yyyy HH:mm");
                worksheet.Cells[i + 2, 3].Value = payment.User?.HoTen ?? "Không xác định";
                worksheet.Cells[i + 2, 4].Value = payment.User?.Email ?? "Không xác định";
                worksheet.Cells[i + 2, 5].Value = payment.Course?.TenKhoaHoc ?? "Không xác định";
                worksheet.Cells[i + 2, 6].Value = payment.Course?.User?.HoTen ?? "Không xác định";
                worksheet.Cells[i + 2, 7].Value = payment.SoTien;
                worksheet.Cells[i + 2, 8].Value = payment.PhuongThucThanhToan ?? "Không xác định";
                worksheet.Cells[i + 2, 9].Value = payment.Status.ToString();
            }

            worksheet.Cells.AutoFitColumns();

            var fileName = $"Payments_{status}_{DateTime.Now:yyyyMMdd}.xlsx";
            var content = package.GetAsByteArray();

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        #endregion

        #region Analytics & Advanced Reports

        public async Task<IActionResult> Analytics()
        {
            var analytics = await GetAdvancedAnalytics();
            return View(analytics);
        }

        private async Task<dynamic> GetAdvancedAnalytics()
        {
            var userGrowth = await AnalyzeUserGrowth();
            var coursePerformance = await AnalyzeCoursePerformance();
            var revenueTrends = await AnalyzeRevenueTrends();
            var teacherPerformance = await AnalyzeTeacherPerformance();

            return new
            {
                UserGrowth = userGrowth,
                CoursePerformance = coursePerformance,
                RevenueTrends = revenueTrends,
                TeacherPerformance = teacherPerformance
            };
        }

        private async Task<object> AnalyzeUserGrowth()
        {
            var last12Months = Enumerable.Range(0, 12)
                .Select(i => DateTime.Now.AddMonths(-i))
                .Reverse()
                .ToList();

            var growthData = new List<object>();

            foreach (var month in last12Months)
            {
                var monthStart = new DateTime(month.Year, month.Month, 1);
                var monthEnd = monthStart.AddMonths(1);

                var newUsers = await _userManager.Users
                    .CountAsync(u => u.NgayDangKy >= monthStart && u.NgayDangKy < monthEnd);

                var totalUsers = await _userManager.Users
                    .CountAsync(u => u.NgayDangKy < monthEnd);

                growthData.Add(new
                {
                    Month = month.ToString("MM/yyyy"),
                    NewUsers = newUsers,
                    TotalUsers = totalUsers
                });
            }

            return growthData;
        }

        private async Task<object> AnalyzeCoursePerformance()
        {
            var courses = await _context.Courses
                .Include(c => c.Enrollments)
                .Include(c => c.Payments.Where(p => p.Status == PaymentStatus.Success))
                .Select(c => new
                {
                    c.TenKhoaHoc,
                    c.Gia,
                    EnrollmentCount = c.Enrollments.Count,
                    Revenue = c.Payments.Sum(p => p.SoTien),
                    CompletionRate = c.Enrollments.Any()
                        ? c.Enrollments.Count(e => e.Progress >= 100) * 100.0 / c.Enrollments.Count
                        : 0
                })
                .OrderByDescending(c => c.Revenue)
                .Take(20)
                .ToListAsync();

            return courses;
        }

        private async Task<object> AnalyzeRevenueTrends()
        {
            var last6Months = Enumerable.Range(0, 6)
                .Select(i => DateTime.Now.AddMonths(-i))
                .Reverse()
                .ToList();

            var trends = new List<object>();

            foreach (var month in last6Months)
            {
                var monthStart = new DateTime(month.Year, month.Month, 1);
                var monthEnd = monthStart.AddMonths(1);

                var monthlyRevenue = await _context.Payments
                    .Where(p => p.Status == PaymentStatus.Success &&
                               p.NgayThanhToan >= monthStart && p.NgayThanhToan < monthEnd)
                    .SumAsync(p => (decimal?)p.SoTien) ?? 0;

                var transactionCount = await _context.Payments
                    .CountAsync(p => p.Status == PaymentStatus.Success &&
                                    p.NgayThanhToan >= monthStart && p.NgayThanhToan < monthEnd);

                trends.Add(new
                {
                    Month = month.ToString("MM/yyyy"),
                    Revenue = monthlyRevenue,
                    TransactionCount = transactionCount,
                    AverageTransaction = transactionCount > 0 ? monthlyRevenue / transactionCount : 0
                });
            }

            return trends;
        }

        private async Task<object> AnalyzeTeacherPerformance()
        {
            var teachers = await _userManager.Users
                .Where(u => u.LoaiNguoiDung == RoleNames.Teacher)
                .Select(u => new
                {
                    u.HoTen,
                    u.Email,
                    CourseCount = _context.Courses.Count(c => c.UserID == u.Id),
                    TotalStudents = _context.Enrollments.Count(e => e.Course.UserID == u.Id),
                    TotalRevenue = _context.Payments
                        .Where(p => p.Course.UserID == u.Id && p.Status == PaymentStatus.Success)
                        .Sum(p => (decimal?)p.SoTien) ?? 0,
                    FollowerCount = _context.Follows.Count(f => f.FollowedTeacherID == u.Id)
                })
                .OrderByDescending(t => t.TotalRevenue)
                .Take(20)
                .ToListAsync();

            return teachers;
        }

        [HttpGet]
        public async Task<IActionResult> GetDetailedChartData(string type, string period = "month")
        {
            switch (type.ToLower())
            {
                case "user-growth":
                    return Json(await GetUserGrowthChartData(period));
                case "course-popularity":
                    return Json(await GetCoursePopularityChartData());
                case "revenue-by-category":
                    return Json(await GetRevenueByCategoryChartData());
                case "teacher-performance":
                    return Json(await GetTeacherPerformanceChartData());
                default:
                    return BadRequest("Invalid chart type");
            }
        }

        private async Task<object> GetUserGrowthChartData(string period)
        {
            var periods = period == "week"
                ? Enumerable.Range(0, 8).Select(i => DateTime.Now.AddDays(-i * 7)).Reverse()
                : Enumerable.Range(0, 12).Select(i => DateTime.Now.AddMonths(-i)).Reverse();

            var data = new List<object>();

            foreach (var date in periods)
            {
                var start = period == "week" ? date.Date : new DateTime(date.Year, date.Month, 1);
                var end = period == "week" ? start.AddDays(7) : start.AddMonths(1);

                var students = await _userManager.Users
                    .CountAsync(u => u.LoaiNguoiDung == RoleNames.User &&
                                    u.NgayDangKy >= start && u.NgayDangKy < end);

                var teachers = await _userManager.Users
                    .CountAsync(u => u.LoaiNguoiDung == RoleNames.Teacher &&
                                    u.NgayDangKy >= start && u.NgayDangKy < end);

                data.Add(new
                {
                    Period = period == "week" ? start.ToString("dd/MM") : start.ToString("MM/yyyy"),
                    Students = students,
                    Teachers = teachers
                });
            }

            return new
            {
                labels = data.Select(d => ((dynamic)d).Period).ToArray(),
                datasets = new[]
                {
                    new
                    {
                        label = "Học viên",
                        data = data.Select(d => ((dynamic)d).Students).ToArray(),
                        borderColor = "rgb(75, 192, 192)",
                        backgroundColor = "rgba(75, 192, 192, 0.2)"
                    },
                    new
                    {
                        label = "Giảng viên",
                        data = data.Select(d => ((dynamic)d).Teachers).ToArray(),
                        borderColor = "rgb(255, 99, 132)",
                        backgroundColor = "rgba(255, 99, 132, 0.2)"
                    }
                }
            };
        }

        private async Task<object> GetCoursePopularityChartData()
        {
            var courses = await _context.Courses
                .Include(c => c.Enrollments)
                .OrderByDescending(c => c.Enrollments.Count)
                .Take(10)
                .Select(c => new
                {
                    Name = c.TenKhoaHoc.Length > 30 ? c.TenKhoaHoc.Substring(0, 30) + "..." : c.TenKhoaHoc,
                    Enrollments = c.Enrollments.Count
                })
                .ToListAsync();

            return new
            {
                labels = courses.Select(c => c.Name).ToArray(),
                datasets = new[]
                {
                    new
                    {
                        label = "Số lượng đăng ký",
                        data = courses.Select(c => c.Enrollments).ToArray(),
                        backgroundColor = new[]
                        {
                            "rgba(255, 99, 132, 0.8)",
                            "rgba(54, 162, 235, 0.8)",
                            "rgba(255, 205, 86, 0.8)",
                            "rgba(75, 192, 192, 0.8)",
                            "rgba(153, 102, 255, 0.8)",
                            "rgba(255, 159, 64, 0.8)",
                            "rgba(199, 199, 199, 0.8)",
                            "rgba(83, 102, 255, 0.8)",
                            "rgba(255, 99, 255, 0.8)",
                            "rgba(99, 255, 132, 0.8)"
                        }
                    }
                }
            };
        }

        private async Task<object> GetRevenueByCategoryChartData()
        {
            var categoryRevenue = await _context.Categories
                .Select(cat => new
                {
                    CategoryName = cat.Name,
                    Revenue = cat.CourseCategories
                        .SelectMany(cc => cc.Course.Payments)
                        .Where(p => p.Status == PaymentStatus.Success)
                        .Sum(p => (decimal?)p.SoTien) ?? 0
                })
                .Where(x => x.Revenue > 0)
                .OrderByDescending(x => x.Revenue)
                .Take(8)
                .ToListAsync();

            return new
            {
                labels = categoryRevenue.Select(c => c.CategoryName).ToArray(),
                datasets = new[]
                {
                    new
                    {
                        label = "Doanh thu (VNĐ)",
                        data = categoryRevenue.Select(c => c.Revenue).ToArray(),
                        backgroundColor = new[]
                        {
                            "rgba(255, 99, 132, 0.8)",
                            "rgba(54, 162, 235, 0.8)",
                            "rgba(255, 205, 86, 0.8)",
                            "rgba(75, 192, 192, 0.8)",
                            "rgba(153, 102, 255, 0.8)",
                            "rgba(255, 159, 64, 0.8)",
                            "rgba(199, 199, 199, 0.8)",
                            "rgba(83, 102, 255, 0.8)"
                        }
                    }
                }
            };
        }

        private async Task<object> GetTeacherPerformanceChartData()
        {
            var teachers = await _userManager.Users
                .Where(u => u.LoaiNguoiDung == RoleNames.Teacher)
                .Select(u => new
                {
                    Name = u.HoTen.Length > 20 ? u.HoTen.Substring(0, 20) + "..." : u.HoTen,
                    Revenue = _context.Payments
                        .Where(p => p.Course.UserID == u.Id && p.Status == PaymentStatus.Success)
                        .Sum(p => (decimal?)p.SoTien) ?? 0,
                    Students = _context.Enrollments.Count(e => e.Course.UserID == u.Id)
                })
                .Where(t => t.Revenue > 0)
                .OrderByDescending(t => t.Revenue)
                .Take(10)
                .ToListAsync();

            return new
            {
                labels = teachers.Select(t => t.Name).ToArray(),
                datasets = new List<object>
                {
                    new
                    {
                        label = "Doanh thu (VNĐ)",
                        data = teachers.Select(t => t.Revenue).ToArray(),
                        backgroundColor = "rgba(54, 162, 235, 0.8)",
                        yAxisID = "y"
                    },
                    new
                    {
                        label = "Số học viên",
                        data = teachers.Select(t => t.Students).ToArray(),
                        backgroundColor = "rgba(255, 99, 132, 0.8)",
                        yAxisID = "y1"
                    }
                }
            };
        }

        [HttpGet]
        public async Task<IActionResult> GetTopCourses()
        {
            var courses = await AnalyzeCoursePerformance() as IEnumerable<dynamic>;
            if (courses == null) return Json(Array.Empty<object>());

            var top = courses
                .OrderByDescending(c => (decimal)c.GetType().GetProperty("Revenue")!.GetValue(c)!)
                .Take(10)
                .Select(c => new
                {
                    Name = c.GetType().GetProperty("TenKhoaHoc")?.GetValue(c)?.ToString() ?? "",
                    Students = (int)(c.GetType().GetProperty("EnrollmentCount")?.GetValue(c) ?? 0),
                    Revenue = (decimal)(c.GetType().GetProperty("Revenue")?.GetValue(c) ?? 0m)
                });

            return Json(top);
        }

        [HttpGet]
        public async Task<IActionResult> GetTopTeachers()
        {
            var teachers = await AnalyzeTeacherPerformance() as IEnumerable<dynamic>;
            if (teachers == null) return Json(Array.Empty<object>());

            var top = teachers
                .OrderByDescending(t => (decimal)t.GetType().GetProperty("TotalRevenue")!.GetValue(t)!)
                .Take(10)
                .Select(t => new
                {
                    Name = t.GetType().GetProperty("HoTen")?.GetValue(t)?.ToString() ?? "",
                    Students = (int)(t.GetType().GetProperty("TotalStudents")?.GetValue(t) ?? 0),
                    Revenue = (decimal)(t.GetType().GetProperty("TotalRevenue")?.GetValue(t) ?? 0m)
                });

            return Json(top);
        }

        #endregion

        #region Backup & Maintenance

        public IActionResult Maintenance()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CleanupDatabase()
        {
            try
            {
                var cleanupCount = 0;

                var oldFailedPayments = await _context.Payments
                    .Where(p => p.Status == PaymentStatus.Failed &&
                               p.NgayThanhToan < DateTime.Now.AddDays(-30))
                    .ToListAsync();

                _context.Payments.RemoveRange(oldFailedPayments);
                cleanupCount += oldFailedPayments.Count;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Đã dọn dẹp {cleanupCount} bản ghi!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi dọn dẹp: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OptimizeDatabase()
        {
            try
            {
                await Task.Delay(1000);
                return Json(new { success = true, message = "Tối ưu hóa cơ sở dữ liệu thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi tối ưu hóa: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BackupDatabase()
        {
            try
            {
                var now = DateTime.Now;
                var record = new BackupRecord
                {
                    CreatedAt = now,
                    FileName = $"backup_{now:yyyyMMdd_HHmmss}.bak",
                    Location = "Local/Cloud (demo)",
                    Status = "Success",
                    Note = "Backup được tạo từ trang bảo trì (demo)."
                };

                _context.BackupRecords.Add(record);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã tạo bản sao lưu dữ liệu (demo)!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi sao lưu: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ClearCache()
        {
            try
            {
                _logger.LogInformation("ClearCache called from admin.");
                return Json(new { success = true, message = "Cache đã được xóa thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi xóa cache: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SecurityScan()
        {
            try
            {
                _logger.LogInformation("SecurityScan executed (demo).");
                return Json(new { success = true, message = "Quét bảo mật hoàn tất. Hệ thống an toàn (demo)." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi quét bảo mật: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RestartServices()
        {
            try
            {
                _logger.LogWarning("RestartServices requested by admin.");
                return Json(new { success = true, message = "Yêu cầu khởi động lại dịch vụ đã được ghi nhận (demo)." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi khởi động lại dịch vụ: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RestartAllServices()
        {
            try
            {
                _logger.LogWarning("RestartAllServices requested by admin.");
                return Json(new { success = true, message = "Yêu cầu khởi động lại tất cả dịch vụ đã được ghi nhận (demo)." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi khởi động lại tất cả dịch vụ: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RunDiagnostics()
        {
            try
            {
                _logger.LogInformation("RunDiagnostics executed (demo).");
                return Json(new { success = true, message = "Đã chạy chẩn đoán hệ thống (demo)." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi chẩn đoán hệ thống: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GenerateSystemReport()
        {
            try
            {
                _logger.LogInformation("GenerateSystemReport executed (demo).");
                return Json(new { success = true, message = "Đã tạo báo cáo tình trạng hệ thống (demo)." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi tạo báo cáo: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ClearSystemLogs()
        {
            try
            {
                _logger.LogWarning("ClearSystemLogs requested (demo).");
                return Json(new { success = true, message = "Đã xóa nhật ký hệ thống (demo)." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi xóa nhật ký: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DownloadSystemLogs()
        {
            try
            {
                _logger.LogInformation("DownloadSystemLogs requested (demo).");
                return Json(new { success = true, message = "Đã tải xuống file nhật ký hệ thống (demo)." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi tải nhật ký: " + ex.Message });
            }
        }

        #endregion
    }
}