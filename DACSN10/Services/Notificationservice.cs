using DACSN10.Models;
using Microsoft.EntityFrameworkCore;

namespace DACSN10.Services
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(string userId, string title, string message, string type, string relatedId = null, string link = null);
        Task CreateBulkNotificationsAsync(List<string> userIds, string title, string message, string type, string relatedId = null, string link = null);
        Task<List<Notification>> GetUserNotificationsAsync(string userId, int pageSize = 20, int pageNumber = 1);
        Task<int> GetUnreadCountAsync(string userId);
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync(string userId);
        Task DeleteNotificationAsync(int notificationId);
        Task NotifyNewLessonAsync(int lessonId, int courseId);
        Task NotifyNewQuizAsync(int quizId, int courseId);
        Task NotifyNewCourseAsync(int courseId, string teacherId);
        Task NotifyLiveStreamAsync(string streamInfo, string teacherId);
        Task NotifyEnrollmentSuccessAsync(int enrollmentId);
    }

    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            AppDbContext context,
            IEmailService emailService,
            ILogger<NotificationService> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task CreateNotificationAsync(string userId, string title, string message, string type, string relatedId = null, string link = null)
        {
            try
            {
                var notification = new Notification
                {
                    UserID = userId,
                    Title = title,
                    Message = message,
                    Type = type,
                    RelatedID = relatedId,
                    Link = link,
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Gửi email thông báo (optional)
                var user = await _context.Users.FindAsync(userId);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    // Chạy background task để không block main thread
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _emailService.SendNotificationEmailAsync(user.Email, title, message, link);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error sending notification email to {Email}", user.Email);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification for user {UserId}", userId);
            }
        }

        public async Task CreateBulkNotificationsAsync(List<string> userIds, string title, string message, string type, string relatedId = null, string link = null)
        {
            try
            {
                var notifications = new List<Notification>();

                foreach (var userId in userIds)
                {
                    notifications.Add(new Notification
                    {
                        UserID = userId,
                        Title = title,
                        Message = message,
                        Type = type,
                        RelatedID = relatedId,
                        Link = link,
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    });
                }

                _context.Notifications.AddRange(notifications);
                await _context.SaveChangesAsync();

                // Gửi email cho tất cả users (chạy background)
                _ = Task.Run(async () =>
                {
                    foreach (var userId in userIds)
                    {
                        try
                        {
                            var user = await _context.Users.FindAsync(userId);
                            if (user != null && !string.IsNullOrEmpty(user.Email))
                            {
                                await _emailService.SendNotificationEmailAsync(user.Email, title, message, link);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error sending email to user {UserId}", userId);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bulk notifications");
            }
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(string userId, int pageSize = 20, int pageNumber = 1)
        {
            return await _context.Notifications
                .Where(n => n.UserID == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserID == userId && !n.IsRead);
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserID == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteNotificationAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
            }
        }

        // Thông báo khi có bài học mới
        public async Task NotifyNewLessonAsync(int lessonId, int courseId)
        {
            try
            {
                var lesson = await _context.Lessons
                    .Include(l => l.Course)
                    .ThenInclude(c => c.User)
                    .FirstOrDefaultAsync(l => l.LessonID == lessonId);

                if (lesson == null) return;

                var course = lesson.Course;
                var teacher = course.User;

                // Lấy danh sách học viên đã đăng ký khóa học
                var enrolledStudentIds = await _context.Enrollments
                    .Where(e => e.CourseID == courseId)
                    .Select(e => e.UserID)
                    .ToListAsync();

                // Lấy danh sách followers của giáo viên
                var followerIds = await _context.Follows
                    .Where(f => f.FollowedTeacherID == teacher.Id)
                    .Select(f => f.FollowerID)
                    .ToListAsync();

                // Gộp 2 danh sách và loại bỏ trùng lặp
                var userIds = enrolledStudentIds.Union(followerIds).Distinct().ToList();

                if (userIds.Any())
                {
                    var title = $"📚 Bài học mới: {lesson.TenBaiHoc}";
                    var message = $"Giáo viên {teacher.HoTen} vừa đăng bài học mới \"{lesson.TenBaiHoc}\" trong khóa học \"{course.TenKhoaHoc}\" lúc {DateTime.Now:HH:mm dd/MM/yyyy}";
                    var link = $"/Course/LessonDetail/{lessonId}";

                    await CreateBulkNotificationsAsync(userIds, title, message, NotificationType.NewLesson, lessonId.ToString(), link);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying new lesson {LessonId}", lessonId);
            }
        }

        // Thông báo khi có bài kiểm tra mới
        public async Task NotifyNewQuizAsync(int quizId, int courseId)
        {
            try
            {
                var quiz = await _context.Quizzes
                    .Include(q => q.Course)
                    .ThenInclude(c => c.User)
                    .FirstOrDefaultAsync(q => q.QuizID == quizId);

                if (quiz == null) return;

                var course = quiz.Course;
                var teacher = course.User;

                // Lấy danh sách học viên đã đăng ký khóa học
                var enrolledStudentIds = await _context.Enrollments
                    .Where(e => e.CourseID == courseId)
                    .Select(e => e.UserID)
                    .ToListAsync();

                if (enrolledStudentIds.Any())
                {
                    var title = $"📝 Bài kiểm tra mới: {quiz.Title}";
                    var message = $"Giáo viên {teacher.HoTen} vừa đăng bài kiểm tra mới \"{quiz.Title}\" trong khóa học \"{course.TenKhoaHoc}\" lúc {DateTime.Now:HH:mm dd/MM/yyyy}";
                    var link = $"/Quiz/Take/{quizId}";

                    await CreateBulkNotificationsAsync(enrolledStudentIds, title, message, NotificationType.NewQuiz, quizId.ToString(), link);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying new quiz {QuizId}", quizId);
            }
        }

        // Thông báo khi giáo viên đăng khóa học mới
        public async Task NotifyNewCourseAsync(int courseId, string teacherId)
        {
            try
            {
                var course = await _context.Courses
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.CourseID == courseId);

                if (course == null) return;

                var teacher = course.User;

                // Lấy danh sách followers của giáo viên
                var followerIds = await _context.Follows
                    .Where(f => f.FollowedTeacherID == teacherId)
                    .Select(f => f.FollowerID)
                    .ToListAsync();

                if (followerIds.Any())
                {
                    var title = $"🎓 Khóa học mới từ {teacher.HoTen}";
                    var message = $"Giáo viên {teacher.HoTen} vừa ra mắt khóa học mới: \"{course.TenKhoaHoc}\" lúc {DateTime.Now:HH:mm dd/MM/yyyy}";
                    var link = $"/Course/Details/{courseId}";

                    await CreateBulkNotificationsAsync(followerIds, title, message, NotificationType.NewCourse, courseId.ToString(), link);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying new course {CourseId}", courseId);
            }
        }

        // Thông báo khi giáo viên mở live stream
        public async Task NotifyLiveStreamAsync(string streamInfo, string teacherId)
        {
            try
            {
                var teacher = await _context.Users.FindAsync(teacherId);
                if (teacher == null) return;

                // Lấy học viên từ tất cả khóa học của giáo viên
                var enrolledStudentIds = await _context.Enrollments
                    .Where(e => e.Course.UserID == teacherId)
                    .Select(e => e.UserID)
                    .Distinct()
                    .ToListAsync();

                // Lấy followers của giáo viên
                var followerIds = await _context.Follows
                    .Where(f => f.FollowedTeacherID == teacherId)
                    .Select(f => f.FollowerID)
                    .ToListAsync();

                // Gộp và loại bỏ trùng lặp
                var userIds = enrolledStudentIds.Union(followerIds).Distinct().ToList();

                if (userIds.Any())
                {
                    var title = $"🔴 Live: {teacher.HoTen} đang phát trực tiếp";
                    var message = $"Giáo viên {teacher.HoTen} đang live stream: {streamInfo} lúc {DateTime.Now:HH:mm dd/MM/yyyy}";
                    var link = $"/LiveStream/Join/{teacherId}";

                    await CreateBulkNotificationsAsync(userIds, title, message, NotificationType.LiveStream, teacherId, link);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying live stream for teacher {TeacherId}", teacherId);
            }
        }

        // Thông báo khi đăng ký khóa học thành công
        public async Task NotifyEnrollmentSuccessAsync(int enrollmentId)
        {
            try
            {
                var enrollment = await _context.Enrollments
                    .Include(e => e.User)
                    .Include(e => e.Course)
                    .ThenInclude(c => c.User)
                    .FirstOrDefaultAsync(e => e.EnrollmentID == enrollmentId);

                if (enrollment == null) return;

                var title = "🎉 Đăng ký khóa học thành công!";
                var message = $"Bạn đã đăng ký thành công khóa học \"{enrollment.Course.TenKhoaHoc}\" của giáo viên {enrollment.Course.User?.HoTen} lúc {DateTime.Now:HH:mm dd/MM/yyyy}. Hãy bắt đầu học ngay!";
                var link = $"/Course/Details/{enrollment.CourseID}";

                await CreateNotificationAsync(enrollment.UserID, title, message, NotificationType.EnrollmentSuccess, enrollmentId.ToString(), link);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying enrollment success {EnrollmentId}", enrollmentId);
            }
        }
    }

    // Helper class for notification types
    public static class NotificationType
    {
        public const string NewLesson = "NewLesson";
        public const string NewQuiz = "NewQuiz";
        public const string NewCourse = "NewCourse";
        public const string LiveStream = "LiveStream";
        public const string EnrollmentSuccess = "EnrollmentSuccess";
    }
}