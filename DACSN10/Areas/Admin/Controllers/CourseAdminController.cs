using DACSN10.Areas.Admin.Models;
using DACSN10.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DACSN10.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = RoleNames.Admin)]
    public class CourseAdminController : Controller
    {
        private readonly AppDbContext _context;

        public CourseAdminController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/CourseAdmin
        public async Task<IActionResult> Index()
        {
            var courses = await _context.Courses
                .Include(c => c.User)
                .Include(c => c.CourseCategories)
                .ThenInclude(cc => cc.Category)
                .ToListAsync();

            var courseViewModels = courses.Select(c => new CourseAdminViewModel
            {
                CourseID = c.CourseID,
                TenKhoaHoc = c.TenKhoaHoc,
                MoTa = c.MoTa,
                Gia = c.Gia,
                TrangThai = c.TrangThai,
                NgayTao = c.NgayTao,
                TenGiangVien = c.User.HoTen,
                UserID = c.UserID
            }).ToList();

            return View(courseViewModels);
        }

        // GET: /Admin/CourseAdmin/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new CourseAdminViewModel
            {
                NgayTao = DateTime.Now,
                AvailableCategories = await _context.Categories.ToListAsync()
            };

            // Lấy danh sách giảng viên để hiển thị trong dropdown
            ViewBag.Teachers = await _context.Users
                .Where(u => u.LoaiNguoiDung == RoleNames.Teacher)
                .ToListAsync();

            return View(viewModel);
        }

        // POST: /Admin/CourseAdmin/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CourseAdminViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                // Tạo khóa học mới
                var course = new Course
                {
                    TenKhoaHoc = viewModel.TenKhoaHoc,
                    MoTa = viewModel.MoTa,
                    Gia = viewModel.Gia,
                    TrangThai = viewModel.TrangThai,
                    NgayTao = viewModel.NgayTao,
                    UserID = viewModel.UserID
                };

                _context.Courses.Add(course);
                await _context.SaveChangesAsync();

                // Thêm danh mục cho khóa học
                if (viewModel.SelectedCategories != null && viewModel.SelectedCategories.Any())
                {
                    foreach (var categoryId in viewModel.SelectedCategories)
                    {
                        _context.CourseCategories.Add(new CourseCategory
                        {
                            CourseID = course.CourseID,
                            CategoryID = categoryId
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Thêm khóa học mới thành công.";
                return RedirectToAction(nameof(Index));
            }

            // Nếu có lỗi, lấy lại danh sách các danh mục và giảng viên
            viewModel.AvailableCategories = await _context.Categories.ToListAsync();
            ViewBag.Teachers = await _context.Users
                .Where(u => u.LoaiNguoiDung == RoleNames.Teacher)
                .ToListAsync();

            return View(viewModel);
        }

        // GET: /Admin/CourseAdmin/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(c => c.CourseCategories)
                .FirstOrDefaultAsync(c => c.CourseID == id);

            if (course == null)
            {
                return NotFound();
            }

            var viewModel = new CourseAdminViewModel
            {
                CourseID = course.CourseID,
                TenKhoaHoc = course.TenKhoaHoc,
                MoTa = course.MoTa,
                Gia = course.Gia,
                TrangThai = course.TrangThai,
                NgayTao = course.NgayTao,
                UserID = course.UserID,
                SelectedCategories = course.CourseCategories.Select(cc => cc.CategoryID).ToList(),
                AvailableCategories = await _context.Categories.ToListAsync()
            };

            // Lấy danh sách giảng viên để hiển thị trong dropdown
            ViewBag.Teachers = await _context.Users
                .Where(u => u.LoaiNguoiDung == RoleNames.Teacher)
                .ToListAsync();

            return View(viewModel);
        }

        // POST: /Admin/CourseAdmin/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CourseAdminViewModel viewModel)
        {
            if (id != viewModel.CourseID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var course = await _context.Courses.FindAsync(id);
                    if (course == null)
                    {
                        return NotFound();
                    }

                    // Cập nhật thông tin khóa học
                    course.TenKhoaHoc = viewModel.TenKhoaHoc;
                    course.MoTa = viewModel.MoTa;
                    course.Gia = viewModel.Gia;
                    course.TrangThai = viewModel.TrangThai;
                    course.UserID = viewModel.UserID;

                    _context.Update(course);

                    // Cập nhật danh mục khóa học
                    var existingCategories = await _context.CourseCategories
                        .Where(cc => cc.CourseID == id)
                        .ToListAsync();

                    _context.CourseCategories.RemoveRange(existingCategories);

                    if (viewModel.SelectedCategories != null && viewModel.SelectedCategories.Any())
                    {
                        foreach (var categoryId in viewModel.SelectedCategories)
                        {
                            _context.CourseCategories.Add(new CourseCategory
                            {
                                CourseID = course.CourseID,
                                CategoryID = categoryId
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật khóa học thành công.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await CourseExists(viewModel.CourseID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            // Nếu có lỗi, lấy lại danh sách các danh mục và giảng viên
            viewModel.AvailableCategories = await _context.Categories.ToListAsync();
            ViewBag.Teachers = await _context.Users
                .Where(u => u.LoaiNguoiDung == RoleNames.Teacher)
                .ToListAsync();

            return View(viewModel);
        }

        // GET: /Admin/CourseAdmin/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(c => c.User)
                .Include(c => c.CourseCategories)
                .ThenInclude(cc => cc.Category)
                .Include(c => c.Lessons)
                .Include(c => c.Assignments)
                .Include(c => c.Quizzes)
                .FirstOrDefaultAsync(c => c.CourseID == id);

            if (course == null)
            {
                return NotFound();
            }

            // Đếm số lượng học viên đã đăng ký khóa học
            var enrollmentCount = await _context.Enrollments
                .Where(e => e.CourseID == id)
                .CountAsync();

            ViewBag.EnrollmentCount = enrollmentCount;

            return View(course);
        }

        // GET: /Admin/CourseAdmin/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(c => c.User)
                .Include(c => c.CourseCategories)
                .ThenInclude(cc => cc.Category)
                .FirstOrDefaultAsync(c => c.CourseID == id);

            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        // POST: /Admin/CourseAdmin/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course != null)
            {
                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa khóa học thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy khóa học cần xóa.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/CourseAdmin/Search
        public async Task<IActionResult> Search()
        {
            var viewModel = new CourseSearchViewModel
            {
                AvailableCategories = await _context.Categories.ToListAsync(),
                AvailableTeachers = await _context.Users
                    .Where(u => u.LoaiNguoiDung == RoleNames.Teacher)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        // POST: /Admin/CourseAdmin/Search
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Search(CourseSearchViewModel viewModel)
        {
            var query = _context.Courses
                .Include(c => c.User)
                .Include(c => c.CourseCategories)
                .ThenInclude(cc => cc.Category)
                .AsQueryable();

            // Lọc theo từ khóa
            if (!string.IsNullOrEmpty(viewModel.Keyword))
            {
                query = query.Where(c => c.TenKhoaHoc.Contains(viewModel.Keyword) ||
                                        c.MoTa.Contains(viewModel.Keyword));
            }

            // Lọc theo danh mục
            if (viewModel.CategoryFilter != null && viewModel.CategoryFilter.Any())
            {
                query = query.Where(c => c.CourseCategories
                    .Any(cc => viewModel.CategoryFilter.Contains(cc.CategoryID)));
            }

            // Lọc theo giá
            if (viewModel.MinPrice.HasValue)
            {
                query = query.Where(c => c.Gia >= viewModel.MinPrice.Value);
            }

            if (viewModel.MaxPrice.HasValue)
            {
                query = query.Where(c => c.Gia <= viewModel.MaxPrice.Value);
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(viewModel.Status))
            {
                query = query.Where(c => c.TrangThai == viewModel.Status);
            }

            // Lọc theo giảng viên
            if (!string.IsNullOrEmpty(viewModel.TeacherID))
            {
                query = query.Where(c => c.UserID == viewModel.TeacherID);
            }

            var courses = await query.ToListAsync();

            viewModel.SearchResults = courses.Select(c => new CourseAdminViewModel
            {
                CourseID = c.CourseID,
                TenKhoaHoc = c.TenKhoaHoc,
                MoTa = c.MoTa,
                Gia = c.Gia,
                TrangThai = c.TrangThai,
                NgayTao = c.NgayTao,
                TenGiangVien = c.User.HoTen,
                UserID = c.UserID
            }).ToList();

            // Load lại các danh sách lọc
            viewModel.AvailableCategories = await _context.Categories.ToListAsync();
            viewModel.AvailableTeachers = await _context.Users
                .Where(u => u.LoaiNguoiDung == RoleNames.Teacher)
                .ToListAsync();

            return View(viewModel);
        }

        // GET: /Admin/CourseAdmin/Approval
        public async Task<IActionResult> Approval()
        {
            var pendingCourses = await _context.Courses
                .Include(c => c.User)
                .Include(c => c.CourseCategories)
                .ThenInclude(cc => cc.Category)
                .Where(c => c.TrangThai == "Chờ duyệt")
                .ToListAsync();

            var viewModels = pendingCourses.Select(c => new CourseApprovalViewModel
            {
                CourseID = c.CourseID,
                TenKhoaHoc = c.TenKhoaHoc,
                MoTa = c.MoTa,
                Gia = c.Gia,
                TrangThai = c.TrangThai,
                NgayTao = c.NgayTao,
                TenGiangVien = c.User.HoTen,
                UserID = c.UserID,
                DanhMucList = c.CourseCategories.Select(cc => cc.Category.Name).ToList()
            }).ToList();

            return View(viewModels);
        }

        // POST: /Admin/CourseAdmin/ApproveCourse/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveCourse(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }

            course.TrangThai = "Đang hoạt động";
            _context.Update(course);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã duyệt khóa học thành công.";
            return RedirectToAction(nameof(Approval));
        }

        // POST: /Admin/CourseAdmin/RejectCourse/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectCourse(int id, string lyDo)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }

            course.TrangThai = "Bị từ chối";
            _context.Update(course);
            await _context.SaveChangesAsync();

            // Gửi thông báo cho giảng viên về lý do từ chối (có thể thêm logic ở đây)

            TempData["SuccessMessage"] = "Đã từ chối khóa học.";
            return RedirectToAction(nameof(Approval));
        }

        private async Task<bool> CourseExists(int id)
        {
            return await _context.Courses.AnyAsync(c => c.CourseID == id);
        }
    }
}