using Microsoft.AspNetCore.Mvc;
using DACSN10.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace DACSN10.Controllers
{
    public class UserController : Controller
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        // Thay đổi thông tin cá nhân (GET)
        public IActionResult EditProfile()
        {
            var userId = User.Identity.Name;
            var user = _context.Users.FirstOrDefault(u => u.UserName == userId);
            if (user == null) return NotFound();
            return View(user);
        }

        // Thay đổi thông tin cá nhân (POST)
        [HttpPost]
        public IActionResult EditProfile(User model)
        {
            var userId = User.Identity.Name;
            var user = _context.Users.FirstOrDefault(u => u.UserName == userId);
            if (user == null) return NotFound();

            user.HoTen = model.HoTen;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            // Giả sử bạn có thêm field Bio thì nên thêm trong model trước
            // user.Bio = model.Bio;

            _context.SaveChanges();
            return RedirectToAction("EditProfile");
        }

        // Follow giảng viên
        [HttpPost]
        public IActionResult FollowTeacher(string teacherId)
        {
            var userId = User.Identity.Name;

            var exists = _context.Follows.Any(f => f.FollowerID == userId && f.FollowedTeacherID == teacherId);
            if (!exists)
            {
                _context.Follows.Add(new Follow
                {
                    FollowerID = userId,
                    FollowedTeacherID = teacherId
                });
                _context.SaveChanges();
            }

            return RedirectToAction("TeacherProfile", new { id = teacherId });
        }

        // Tìm kiếm giảng viên + đề xuất khóa học của giảng viên đó
        public IActionResult SearchTeacher(string keyword)
        {
            var teachers = _context.Users
                                   .Where(u => u.LoaiNguoiDung == "teacher" && u.HoTen.Contains(keyword))
                                   .ToList();

            var result = teachers.Select(t => new
            {
                Teacher = t,
                Courses = _context.Courses.Where(c => c.UserID == t.Id).ToList()
            });

            return View(result);
        }

    }
}
