using DACSN10.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DACSN10.Controllers
{
    [Authorize]
    public class LiveController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<LiveController> _logger;

        public LiveController(IHttpClientFactory clientFactory, UserManager<User> userManager, ILogger<LiveController> logger)
        {
            _clientFactory = clientFactory;
            _userManager = userManager;
            _logger = logger;
        }

        // Teacher starts a livestream (create room if not exists, then join as TEACHER)
        [Authorize(Policy = "TeacherOrAdmin")] // hoặc [Authorize(Roles = "Teacher,Admin")]
        [HttpGet]
        public async Task<IActionResult> Teacher(long teacherId, long userId, string? displayName = null, CancellationToken ct = default)
        {
            var http = _clientFactory.CreateClient("LiveApi");

            // Lấy họ tên từ Identity
            var me = await _userManager.GetUserAsync(User);
            var finalDisplayName = string.IsNullOrWhiteSpace(displayName)
                ? (me?.HoTen ?? me?.UserName ?? User.Identity?.Name ?? "Teacher")
                : displayName;

            try
            {
                // 1) Tạo/lấy phòng
                using var createResp = await http.PostAsJsonAsync("api/rooms", new { teacherId }, ct);
                if (!createResp.IsSuccessStatusCode)
                {
                    return await FailFromResponse(createResp, "Không tạo được phòng livestream. Vui lòng thử lại sau.");
                }

                var room = await SafeReadJsonAsync<RoomResponse>(createResp);
                if (room == null || room.roomId == Guid.Empty)
                {
                    TempData["Error"] = "Phòng livestream trả về dữ liệu không hợp lệ.";
                    return RedirectToAction("Index", "Home");
                }

                // 2) Join với vai trò TEACHER (gửi role dạng chuỗi để Spring hiểu đúng enum)
                var joinBody = new { userId, role = "TEACHER", displayName = finalDisplayName };
                using var joinResp = await http.PostAsJsonAsync($"api/rooms/{room.roomId}/join", joinBody, ct);
                if (!joinResp.IsSuccessStatusCode)
                {
                    return await FailFromResponse(joinResp, "Không thể tham gia phòng livestream với vai trò giảng viên.");
                }

                var join = await SafeReadJsonAsync<JoinRoomResponse>(joinResp);
                if (join == null || join.roomId == Guid.Empty || join.participantId == Guid.Empty)
                {
                    TempData["Error"] = "Không lấy được thông tin tham gia phòng.";
                    return RedirectToAction("Index", "Home");
                }

                // 3) Cấu hình cho JS (qua reverse proxy /spring)
                var apiBaseForJs = $"{Request.Scheme}://{Request.Host}/spring";

                ViewBag.ApiBase = apiBaseForJs;
                ViewBag.RoomId = join.roomId;
                ViewBag.ParticipantId = join.participantId;
                ViewBag.TeacherParticipantId = join.participantId; // teacher chính là participant hiện tại
                ViewBag.DisplayName = finalDisplayName;

                return View();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Teacher() HttpRequestException");
                TempData["Error"] = "Không kết nối được dịch vụ livestream. Vui lòng thử lại sau.";
                return RedirectToAction("Index", "Home");
            }
            catch (TaskCanceledException) when (ct.IsCancellationRequested)
            {
                // Request bị hủy theo CT của ASP.NET (client hủy trang)
                return new StatusCodeResult(499); // Client Closed Request (non-standard)
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Teacher() unexpected error");
                TempData["Error"] = "Đã xảy ra lỗi không xác định khi khởi tạo livestream.";
                return RedirectToAction("Index", "Home");
            }
        }

        // Student joins a room by roomId
        [HttpGet]
        public async Task<IActionResult> Student(Guid roomId, long userId, string? displayName = null, CancellationToken ct = default)
        {
            var http = _clientFactory.CreateClient("LiveApi");

            var me = await _userManager.GetUserAsync(User);
            var finalDisplayName = string.IsNullOrWhiteSpace(displayName)
                ? (me?.HoTen ?? me?.UserName ?? User.Identity?.Name ?? "Student")
                : displayName;

            try
            {
                // Gửi role dạng chuỗi để tương thích với Spring enum
                var joinBody = new { userId = userId, role = "STUDENT", displayName = finalDisplayName };
                using var joinRespMsg = await http.PostAsJsonAsync($"api/rooms/{roomId}/join", joinBody, ct);

                if (!joinRespMsg.IsSuccessStatusCode)
                {
                    // Xử lý riêng 404: phòng không tồn tại hoặc đã kết thúc
                    if (joinRespMsg.StatusCode == HttpStatusCode.NotFound)
                    {
                        var detail = await TryReadErrorMessageAsync(joinRespMsg);
                        TempData["Error"] = string.IsNullOrWhiteSpace(detail)
                            ? "Phòng livestream không tồn tại hoặc đã kết thúc."
                            : detail;
                        return RedirectToAction("Index", "Home");
                    }
                    return await FailFromResponse(joinRespMsg, "Không thể tham gia phòng livestream.");
                }

                var join = await SafeReadJsonAsync<JoinRoomResponse>(joinRespMsg);
                if (join == null || join.roomId == Guid.Empty || join.participantId == Guid.Empty)
                {
                    TempData["Error"] = "Không lấy được thông tin tham gia phòng.";
                    return RedirectToAction("Index", "Home");
                }

                var apiBaseForJs = $"{Request.Scheme}://{Request.Host}/spring";

                ViewBag.ApiBase = apiBaseForJs;
                ViewBag.RoomId = join.roomId;
                ViewBag.ParticipantId = join.participantId;
                ViewBag.TeacherParticipantId = join.teacherParticipantId;
                ViewBag.DisplayName = finalDisplayName;

                return View();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Student() HttpRequestException");
                TempData["Error"] = "Không kết nối được dịch vụ livestream. Vui lòng thử lại sau.";
                return RedirectToAction("Index", "Home");
            }
            catch (TaskCanceledException) when (ct.IsCancellationRequested)
            {
                return new StatusCodeResult(499);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Student() unexpected error");
                TempData["Error"] = "Đã xảy ra lỗi không xác định khi tham gia phòng.";
                return RedirectToAction("Index", "Home");
            }
        }

        // ============ Helpers ============

        private async Task<IActionResult> FailFromResponse(HttpResponseMessage resp, string userFallback, string redirectController = "Home", string redirectAction = "Index")
        {
            var detail = await TryReadErrorMessageAsync(resp);
            var msg = !string.IsNullOrWhiteSpace(detail)
                ? detail
                : $"{userFallback} (HTTP {(int)resp.StatusCode}).";

            TempData["Error"] = msg;
            return RedirectToAction(redirectAction, redirectController);
        }

        private static async Task<string?> TryReadErrorMessageAsync(HttpResponseMessage resp)
        {
            try
            {
                var raw = await resp.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(raw)) return null;

                // Backend Java ApiExceptionHandler trả về JSON có "message"
                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.TryGetProperty("message", out var m))
                    return m.GetString();

                // fallback lấy "error" nếu có
                if (doc.RootElement.TryGetProperty("error", out var e))
                    return e.GetString();

                return raw.Length <= 256 ? raw : raw[..256] + "...";
            }
            catch
            {
                return null;
            }
        }

        private static async Task<T?> SafeReadJsonAsync<T>(HttpResponseMessage resp)
        {
            try
            {
                return await resp.Content.ReadFromJsonAsync<T>();
            }
            catch
            {
                return default;
            }
        }
    }
}