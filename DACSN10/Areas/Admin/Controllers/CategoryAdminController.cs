using DACSN10.Areas.Admin.Models;
using DACSN10.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DACSN10.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = RoleNames.Admin)]
    public class CategoryAdminController : Controller
    {
        private readonly AppDbContext _context;

        public CategoryAdminController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/CategoryAdmin
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .Include(c => c.CourseCategories)
                .ToListAsync();

            var viewModels = categories.Select(c => new CategoryAdminViewModel
            {
                CategoryID = c.CategoryID,
                Name = c.Name,
                Description = c.Description,
                CourseCount = c.CourseCategories.Count
            }).ToList();

            return View(viewModels);
        }

        // GET: /Admin/CategoryAdmin/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Admin/CategoryAdmin/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryAdminViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var category = new Category
                {
                    Name = viewModel.Name,
                    Description = viewModel.Description
                };

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thêm danh mục mới thành công.";
                return RedirectToAction(nameof(Index));
            }

            return View(viewModel);
        }

        // GET: /Admin/CategoryAdmin/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            var viewModel = new CategoryAdminViewModel
            {
                CategoryID = category.CategoryID,
                Name = category.Name,
                Description = category.Description
            };

            return View(viewModel);
        }

        // POST: /Admin/CategoryAdmin/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CategoryAdminViewModel viewModel)
        {
            if (id != viewModel.CategoryID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var category = await _context.Categories.FindAsync(id);
                    if (category == null)
                    {
                        return NotFound();
                    }

                    category.Name = viewModel.Name;
                    category.Description = viewModel.Description;

                    _context.Update(category);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật danh mục thành công.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await CategoryExists(viewModel.CategoryID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return View(viewModel);
        }

        // GET: /Admin/CategoryAdmin/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .Include(c => c.CourseCategories)
                .ThenInclude(cc => cc.Course)
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
            {
                return NotFound();
            }

            var viewModel = new CategoryAdminViewModel
            {
                CategoryID = category.CategoryID,
                Name = category.Name,
                Description = category.Description,
                CourseCount = category.CourseCategories.Count
            };

            // Lấy danh sách khóa học thuộc danh mục này
            ViewBag.Courses = category.CourseCategories
                .Select(cc => cc.Course)
                .ToList();

            return View(viewModel);
        }

        // GET: /Admin/CategoryAdmin/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .Include(c => c.CourseCategories)
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
            {
                return NotFound();
            }

            var viewModel = new CategoryAdminViewModel
            {
                CategoryID = category.CategoryID,
                Name = category.Name,
                Description = category.Description,
                CourseCount = category.CourseCategories.Count
            };

            return View(viewModel);
        }

        // POST: /Admin/CategoryAdmin/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                // Kiểm tra xem danh mục có khóa học không
                var hasCourses = await _context.CourseCategories
                    .AnyAsync(cc => cc.CategoryID == id);

                if (hasCourses)
                {
                    TempData["ErrorMessage"] = "Không thể xóa danh mục này vì có khóa học đang sử dụng.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa danh mục thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy danh mục cần xóa.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/CategoryAdmin/Search
        public IActionResult Search()
        {
            return View(new CategorySearchViewModel());
        }

        // POST: /Admin/CategoryAdmin/Search
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Search(CategorySearchViewModel viewModel)
        {
            var query = _context.Categories
                .Include(c => c.CourseCategories)
                .AsQueryable();

            // Lọc theo từ khóa
            if (!string.IsNullOrEmpty(viewModel.Keyword))
            {
                query = query.Where(c => c.Name.Contains(viewModel.Keyword) ||
                                       (c.Description != null && c.Description.Contains(viewModel.Keyword)));
            }

            var categories = await query.ToListAsync();

            viewModel.SearchResults = categories.Select(c => new CategoryAdminViewModel
            {
                CategoryID = c.CategoryID,
                Name = c.Name,
                Description = c.Description,
                CourseCount = c.CourseCategories.Count
            }).ToList();

            return View(viewModel);
        }

        private async Task<bool> CategoryExists(int id)
        {
            return await _context.Categories.AnyAsync(c => c.CategoryID == id);
        }
    }
}