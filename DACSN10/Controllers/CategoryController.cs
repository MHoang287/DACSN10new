using Microsoft.AspNetCore.Mvc;
using DACSN10.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;

namespace DACSN10.Controllers
{
    public class CategoryController : Controller
    {
        private readonly AppDbContext _context;

        public CategoryController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .Include(c => c.CourseCategories)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(categories);
        }

        public async Task<IActionResult> Details(int id)
        {
            var category = await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
            {
                TempData["Error"] = "Không tìm thấy danh mục.";
                return NotFound();
            }

            var courses = await _context.CourseCategories
                .AsNoTracking()
                .Include(cc => cc.Course).ThenInclude(c => c.User)
                .Include(cc => cc.Course).ThenInclude(c => c.Enrollments)
                .Where(cc => cc.CategoryID == id && cc.Course.TrangThai == "Active")
                .Select(cc => cc.Course)
                .OrderByDescending(c => c.Enrollments.Count)
                .ToListAsync();

            ViewBag.Category = category;
            return View(courses);
        }

        // API endpoint để lấy danh sách categories cho dropdown
        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .Select(c => new { c.CategoryID, c.Name })
                .OrderBy(c => c.Name)
                .ToListAsync();

            return Json(categories);
        }
    }
}