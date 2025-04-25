using Microsoft.AspNetCore.Mvc;
using DACSN10.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;

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

            _context.SaveChanges();
            TempData["Success"] = "Cập nhật thông tin thành công!";
            return RedirectToAction("EditProfile");
        }

        // Hồ sơ giảng viên
        public IActionResult TeacherProfile(string id)
        {
            var teacher = _context.Users
                .Where(u => u.Id == id && u.LoaiNguoiDung == "teacher")
                .Select(t => new TeacherSearchViewModel
                {
                    Teacher = t,
                    Courses = _context.Courses.Where(c => c.UserID == t.Id).ToList()
                })
                .FirstOrDefault();

            if (teacher == null)
            {
                return NotFound();
            }

            return View(teacher);
        }

        // Follow giảng viên
        [HttpPost]
        public IActionResult FollowTeacher(string teacherId)
        {
            var currentUser = _context.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để theo dõi giảng viên." });
            }

            var teacher = _context.Users.FirstOrDefault(u => u.Id == teacherId && u.LoaiNguoiDung == "teacher");
            if (teacher == null)
            {
                return Json(new { success = false, message = "Không tìm thấy giảng viên." });
            }

            var exists = _context.Follows.Any(f => f.FollowerID == currentUser.Id && f.FollowedTeacherID == teacherId);
            if (!exists)
            {
                _context.Follows.Add(new Follow
                {
                    FollowerID = currentUser.Id,
                    FollowedTeacherID = teacherId
                });
                _context.SaveChanges();
            }

            return Json(new { success = true, message = "Đã theo dõi giảng viên!" });
        }

        // Bỏ theo dõi giảng viên
        [HttpPost]
        public IActionResult UnfollowTeacher(string teacherId)
        {
            var currentUser = _context.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để bỏ theo dõi giảng viên." });
            }

            var teacher = _context.Users.FirstOrDefault(u => u.Id == teacherId && u.LoaiNguoiDung == "teacher");
            if (teacher == null)
            {
                return Json(new { success = false, message = "Không tìm thấy giảng viên." });
            }

            var follow = _context.Follows.FirstOrDefault(f => f.FollowerID == currentUser.Id && f.FollowedTeacherID == teacherId);
            if (follow != null)
            {
                _context.Follows.Remove(follow);
                _context.SaveChanges();
            }

            return Json(new { success = true, message = "Đã bỏ theo dõi giảng viên!" });
        }

        // Tìm kiếm giảng viên + đề xuất khóa học của giảng viên đó
        public IActionResult SearchTeacher(string keyword)
        {
            var currentUser = _context.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
            string currentUserId = currentUser?.Id;

            var teachers = _context.Users
                .Where(u => u.LoaiNguoiDung == "teacher" && (string.IsNullOrEmpty(keyword) || u.HoTen.Contains(keyword)))
                .ToList();

            var result = teachers.Select(t => new TeacherSearchViewModel
            {
                Teacher = t,
                Courses = _context.Courses.Where(c => c.UserID == t.Id).ToList(),
                IsFollowed = currentUserId != null && _context.Follows.Any(f => f.FollowerID == currentUserId && f.FollowedTeacherID == t.Id)
            }).ToList();

            ViewBag.Keyword = keyword;
            return View(result);
        }
    }
}