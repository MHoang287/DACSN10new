using DACSN10.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace DACSN10.Controllers
{
    [Authorize]
    public class LiveController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly UserManager<User> _userManager;

        public LiveController(IHttpClientFactory clientFactory, UserManager<User> userManager)
        {
            _clientFactory = clientFactory;
            _userManager = userManager;
        }

        // Teacher starts a livestream (create room if not exists, then join as TEACHER)
        [Authorize(Policy = "TeacherOrAdmin")] // hoặc [Authorize(Roles = "Teacher,Admin")]
        [HttpGet]
        public async Task<IActionResult> Teacher(long teacherId, long userId, string? displayName = null)
        {
            var http = _clientFactory.CreateClient("LiveApi");

            // Lấy họ tên từ Identity
            var me = await _userManager.GetUserAsync(User);
            var finalDisplayName = string.IsNullOrWhiteSpace(displayName)
                ? (me?.HoTen ?? me?.UserName ?? User.Identity?.Name ?? "Teacher")
                : displayName;

            // 1) Tạo/lấy phòng
            var createResp = await http.PostAsJsonAsync("api/rooms", new { teacherId });
            createResp.EnsureSuccessStatusCode();
            var room = await createResp.Content.ReadFromJsonAsync<RoomResponse>();

            // 2) Join với vai trò TEACHER
            var joinResp = await http.PostAsJsonAsync($"api/rooms/{room!.roomId}/join",
                              new { userId, role = "TEACHER", displayName = finalDisplayName });
            joinResp.EnsureSuccessStatusCode();
            var join = await joinResp.Content.ReadFromJsonAsync<JoinRoomResponse>();

            // 3) Cấu hình cho JS (qua reverse proxy /spring)
            var apiBaseForJs = $"{Request.Scheme}://{Request.Host}/spring";

            ViewBag.ApiBase = apiBaseForJs;
            ViewBag.RoomId = join!.roomId;
            ViewBag.ParticipantId = join.participantId;
            ViewBag.TeacherParticipantId = join.participantId;
            ViewBag.DisplayName = finalDisplayName;

            return View();
        }

        // Student joins a room by roomId
        [HttpGet]
        public async Task<IActionResult> Student(Guid roomId, long userId, string? displayName = null)
        {
            var http = _clientFactory.CreateClient("LiveApi");

            var me = await _userManager.GetUserAsync(User);
            var finalDisplayName = string.IsNullOrWhiteSpace(displayName)
                ? (me?.HoTen ?? me?.UserName ?? User.Identity?.Name ?? "Student")
                : displayName;

            var joinReq = new JoinRoomRequest
            {
                userId = userId,
                role = ParticipantRole.STUDENT,
                displayName = finalDisplayName
            };
            var joinRespMsg = await http.PostAsJsonAsync($"api/rooms/{roomId}/join", joinReq);
            joinRespMsg.EnsureSuccessStatusCode();
            var join = await joinRespMsg.Content.ReadFromJsonAsync<JoinRoomResponse>();

            var apiBaseForJs = $"{Request.Scheme}://{Request.Host}/spring";

            ViewBag.ApiBase = apiBaseForJs;
            ViewBag.RoomId = join!.roomId;
            ViewBag.ParticipantId = join.participantId;
            ViewBag.TeacherParticipantId = join.teacherParticipantId;
            ViewBag.DisplayName = finalDisplayName;

            return View();
        }
    }
}