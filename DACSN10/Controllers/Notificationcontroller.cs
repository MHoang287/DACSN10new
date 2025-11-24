using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DACSN10.Models;
using DACSN10.Services;
using System.Security.Claims;

namespace DACSN10.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(
            INotificationService notificationService,
            ILogger<NotificationController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        // GET: /Notification/Index
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [Route("/Api/Notifications/GetNotifications")]
        public async Task<IActionResult> GetNotifications(int pageSize = 20, int pageNumber = 1)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }

                var notifications = await _notificationService.GetUserNotificationsAsync(userId, pageSize, pageNumber);

                var notificationDtos = notifications.Select(n => new
                {
                    notificationID = n.NotificationID,
                    title = n.Title,
                    message = n.Message,
                    type = n.Type,
                    link = n.Link,
                    isRead = n.IsRead,
                    createdAt = n.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    readAt = n.ReadAt?.ToString("dd/MM/yyyy HH:mm"),
                    timeAgo = GetTimeAgo(n.CreatedAt)
                }).ToList();

                return Json(new { success = true, data = notificationDtos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải thông báo" });
            }
        }

        // API: GET /Notification/GetUnreadCount
        // API: GET /Api/Notifications/GetUnread
        [HttpGet]
        [Route("/Api/Notifications/GetUnread")]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }

                var count = await _notificationService.GetUnreadCountAsync(userId);
                return Json(new { success = true, count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count");
                return Json(new { success = false, count = 0 });
            }
        }

        // API: POST /Notification/MarkAsRead
        // API: POST /Api/Notifications/MarkAsRead
        [HttpPost]
        [Route("/Api/Notifications/MarkAsRead")]
        public async Task<IActionResult> MarkAsRead([FromBody] int notificationId)
        {
            try
            {
                await _notificationService.MarkAsReadAsync(notificationId);
                return Json(new { success = true, message = "Đã đánh dấu đã đọc" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read");
                return Json(new { success = false, message = "Có lỗi xảy ra" });
            }
        }

        // API: POST /Notification/MarkAllAsRead
        // API: POST /Api/Notifications/MarkAllAsRead
        [HttpPost]
        [Route("/Api/Notifications/MarkAllAsRead")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }

                await _notificationService.MarkAllAsReadAsync(userId);
                return Json(new { success = true, message = "Đã đánh dấu tất cả đã đọc" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all as read");
                return Json(new { success = false, message = "Có lỗi xảy ra" });
            }
        }

        // API: POST /Notification/Delete
        // API: POST /Api/Notifications/Delete
        [HttpPost]
        [Route("/Api/Notifications/Delete")]
        public async Task<IActionResult> Delete([FromBody] int notificationId)
        {
            try
            {
                await _notificationService.DeleteNotificationAsync(notificationId);
                return Json(new { success = true, message = "Đã xóa thông báo" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification");
                return Json(new { success = false, message = "Có lỗi xảy ra" });
            }
        }

        #region Helper Methods

        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;

            if (timeSpan.TotalMinutes < 1)
                return "Vừa xong";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} phút trước";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} giờ trước";
            if (timeSpan.TotalDays < 30)
                return $"{(int)timeSpan.TotalDays} ngày trước";
            if (timeSpan.TotalDays < 365)
                return $"{(int)(timeSpan.TotalDays / 30)} tháng trước";

            return $"{(int)(timeSpan.TotalDays / 365)} năm trước";
        }

        #endregion
    }
}