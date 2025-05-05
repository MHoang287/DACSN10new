using DACSN10.Areas.Teacher.Models;
using DACSN10.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DACSN10.Areas.TeacherArea.Controllers
{
    [Area("Teacher")]
    [Authorize(Roles = "Teacher")]
    public class CourseController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CourseController(AppDbContext context, UserManager<User> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: /TeacherArea/Course
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var courses = await _context.Courses
                .Where(c => c.UserID == currentUser.Id)
                .ToListAsync();

            return View(courses);
        }

        // GET: /TeacherArea/Course/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new CourseViewModel
            {
                AvailableCategories = await _context.Categories.ToListAsync(),
                SelectedCategories = new List<int>(),
                TrangThai = "Draft" // Default status
            };

            return View(viewModel);
        }

        // POST: /TeacherArea/Course/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CourseViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.GetUserAsync(User);

                var course = new Course
                {
                    TenKhoaHoc = viewModel.TenKhoaHoc,
                    MoTa = viewModel.MoTa,
                    Gia = viewModel.Gia,
                    TrangThai = viewModel.TrangThai,
                    NgayTao = DateTime.Now,
                    UserID = currentUser.Id
                };

                _context.Courses.Add(course);
                await _context.SaveChangesAsync();

                // Add selected categories
                if (viewModel.SelectedCategories != null && viewModel.SelectedCategories.Any())
                {
                    foreach (var categoryId in viewModel.SelectedCategories)
                    {
                        var courseCategory = new CourseCategory
                        {
                            CourseID = course.CourseID,
                            CategoryID = categoryId
                        };

                        _context.CourseCategories.Add(courseCategory);
                    }

                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }

            // If we got this far, something failed; redisplay form
            viewModel.AvailableCategories = await _context.Categories.ToListAsync();
            return View(viewModel);
        }

        // GET: /TeacherArea/Course/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var course = await _context.Courses
                .Include(c => c.CourseCategories)
                .FirstOrDefaultAsync(c => c.CourseID == id && c.UserID == currentUser.Id);

            if (course == null)
            {
                return NotFound();
            }

            var viewModel = new CourseViewModel
            {
                CourseID = course.CourseID,
                TenKhoaHoc = course.TenKhoaHoc,
                MoTa = course.MoTa,
                Gia = course.Gia,
                TrangThai = course.TrangThai,
                NgayTao = course.NgayTao,
                SelectedCategories = course.CourseCategories.Select(cc => cc.CategoryID).ToList(),
                AvailableCategories = await _context.Categories.ToListAsync()
            };

            return View(viewModel);
        }

        // POST: /TeacherArea/Course/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CourseViewModel viewModel)
        {
            if (id != viewModel.CourseID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    var course = await _context.Courses
                        .Include(c => c.CourseCategories)
                        .FirstOrDefaultAsync(c => c.CourseID == id && c.UserID == currentUser.Id);

                    if (course == null)
                    {
                        return NotFound();
                    }

                    course.TenKhoaHoc = viewModel.TenKhoaHoc;
                    course.MoTa = viewModel.MoTa;
                    course.Gia = viewModel.Gia;
                    course.TrangThai = viewModel.TrangThai;

                    // Update categories
                    var existingCategories = course.CourseCategories.ToList();

                    // Remove categories that are no longer selected
                    foreach (var courseCategory in existingCategories)
                    {
                        if (!viewModel.SelectedCategories.Contains(courseCategory.CategoryID))
                        {
                            _context.CourseCategories.Remove(courseCategory);
                        }
                    }

                    // Add newly selected categories
                    if (viewModel.SelectedCategories != null)
                    {
                        foreach (var categoryId in viewModel.SelectedCategories)
                        {
                            if (!existingCategories.Any(cc => cc.CategoryID == categoryId))
                            {
                                _context.CourseCategories.Add(new CourseCategory
                                {
                                    CourseID = course.CourseID,
                                    CategoryID = categoryId
                                });
                            }
                        }
                    }

                    _context.Update(course);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CourseExists(viewModel.CourseID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return RedirectToAction(nameof(Index));
            }

            viewModel.AvailableCategories = await _context.Categories.ToListAsync();
            return View(viewModel);
        }

        // GET: /TeacherArea/Course/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseID == id && c.UserID == currentUser.Id);

            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        // POST: /TeacherArea/Course/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseID == id && c.UserID == currentUser.Id);

            if (course == null)
            {
                return NotFound();
            }

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: /TeacherArea/Course/Lessons/5
        public async Task<IActionResult> Lessons(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var course = await _context.Courses
                .Include(c => c.Lessons)
                .FirstOrDefaultAsync(c => c.CourseID == id && c.UserID == currentUser.Id);

            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        // GET: /TeacherArea/Course/CreateLesson/5
        public async Task<IActionResult> CreateLesson(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseID == id && c.UserID == currentUser.Id);

            if (course == null)
            {
                return NotFound();
            }

            var viewModel = new LessonViewModel
            {
                CourseID = id
            };

            return View(viewModel);
        }

        // POST: /TeacherArea/Course/CreateLesson
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLesson(LessonViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var course = await _context.Courses
                    .FirstOrDefaultAsync(c => c.CourseID == viewModel.CourseID && c.UserID == currentUser.Id);

                if (course == null)
                {
                    return NotFound();
                }

                var lesson = new Lesson
                {
                    TenBaiHoc = viewModel.TenBaiHoc,
                    NoiDung = viewModel.NoiDung,
                    ThoiLuong = viewModel.ThoiLuong,
                    VideoUrl = viewModel.VideoUrl,
                    CourseID = viewModel.CourseID
                };

                // Handle file upload if provided
                if (viewModel.DocumentFile != null && viewModel.DocumentFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Uploads", "lessons");
                    Directory.CreateDirectory(uploadsFolder); // Ensure directory exists

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + viewModel.DocumentFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await viewModel.DocumentFile.CopyToAsync(fileStream);
                    }

                    lesson.NoiDung = "/Uploads/lessons/" + uniqueFileName;
                }

                _context.Lessons.Add(lesson);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Lessons), new { id = viewModel.CourseID });
            }

            return View(viewModel);
        }

        // GET: /TeacherArea/Course/Students/5
        public async Task<IActionResult> Students(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseID == id && c.UserID == currentUser.Id);

            if (course == null)
            {
                return NotFound();
            }

            var enrollments = await _context.Enrollments
                .Include(e => e.User)
                .Where(e => e.CourseID == id)
                .Select(e => new EnrollmentViewModel
                {
                    EnrollmentID = e.EnrollmentID,
                    UserName = e.User.UserName,
                    Email = e.User.Email,
                    HoTen = e.User.HoTen,
                    EnrollDate = e.EnrollDate,
                    TrangThai = e.TrangThai,
                    Progress = e.Progress
                })
                .ToListAsync();

            ViewBag.Course = course;
            return View(enrollments);
        }

        // GET: /TeacherArea/Course/Followers/5
        public async Task<IActionResult> Followers(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseID == id && c.UserID == currentUser.Id);

            if (course == null)
            {
                return NotFound();
            }

            var followers = await _context.CourseFollows
                .Include(cf => cf.User)
                .Where(cf => cf.CourseID == id)
                .Select(cf => new CourseFollowViewModel
                {
                    UserName = cf.User.UserName,
                    Email = cf.User.Email,
                    HoTen = cf.User.HoTen,
                    FollowDate = cf.FollowDate
                })
                .ToListAsync();

            ViewBag.Course = course;
            return View(followers);
        }

        // GET: /TeacherArea/Course/EnrollmentStats
        public async Task<IActionResult> EnrollmentStats()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var coursesWithEnrollments = await _context.Courses
                .Where(c => c.UserID == currentUser.Id)
                .Select(c => new
                {
                    Course = c,
                    EnrollmentCount = c.Enrollments.Count
                })
                .OrderByDescending(c => c.EnrollmentCount)
                .ToListAsync();

            return View(coursesWithEnrollments);
        }

        // GET: /TeacherArea/Course/Quizzes/5
        public async Task<IActionResult> Quizzes(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var course = await _context.Courses
                .Include(c => c.Quizzes)
                .FirstOrDefaultAsync(c => c.CourseID == id && c.UserID == currentUser.Id);

            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        // GET: /TeacherArea/Course/CreateQuiz/5
        public async Task<IActionResult> CreateQuiz(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseID == id && c.UserID == currentUser.Id);

            if (course == null)
            {
                return NotFound();
            }

            var viewModel = new QuizViewModel
            {
                CourseID = id,
                Questions = new List<QuestionViewModel> { new QuestionViewModel() }
            };

            return View(viewModel);
        }

        // POST: /TeacherArea/Course/CreateQuiz
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateQuiz(QuizViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var course = await _context.Courses
                    .FirstOrDefaultAsync(c => c.CourseID == viewModel.CourseID && c.UserID == currentUser.Id);

                if (course == null)
                {
                    return NotFound();
                }

                var quiz = new Quiz
                {
                    Title = viewModel.Title,
                    CourseID = viewModel.CourseID,
                    Questions = new List<Question>()
                };

                foreach (var questionVM in viewModel.Questions)
                {
                    var question = new Question
                    {
                        QuestionText = questionVM.QuestionText,
                        OptionA = questionVM.OptionA,
                        OptionB = questionVM.OptionB,
                        OptionC = questionVM.OptionC,
                        OptionD = questionVM.OptionD,
                        CorrectAnswer = questionVM.CorrectAnswer
                    };

                    quiz.Questions.Add(question);
                }

                _context.Quizzes.Add(quiz);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Quizzes), new { id = viewModel.CourseID });
            }

            return View(viewModel);
        }

        private bool CourseExists(int id)
        {
            return _context.Courses.Any(e => e.CourseID == id);
        }
    }
}