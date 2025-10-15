# ASP.NET MVC Integration Guide

This document provides step-by-step instructions for integrating the Spring Boot Livestream Service with the existing ASP.NET MVC website.

## Overview

The integration involves:
1. Creating a service layer in ASP.NET MVC to communicate with the Spring Boot API
2. Adding controllers and views for livestream functionality
3. Implementing WebRTC client-side code
4. Handling authentication between both systems

## Step 1: Create LivestreamService Class

Create a new service in `DACSN10/Services/LivestreamService.cs`:

```csharp
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DACSN10.Services
{
    public class LivestreamService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public LivestreamService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["LivestreamService:BaseUrl"] ?? "http://localhost:8080/api";
        }

        // Authentication
        public async Task<LivestreamAuthResponse> RegisterAsync(string username, string password, 
            string email, string fullName, string role)
        {
            var registerData = new
            {
                username,
                password,
                email,
                fullName,
                role
            };

            var json = JsonSerializer.Serialize(registerData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/auth/register", content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<LivestreamAuthResponse>(responseBody);
        }

        public async Task<LivestreamAuthResponse> LoginAsync(string username, string password)
        {
            var loginData = new { username, password };
            var json = JsonSerializer.Serialize(loginData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/auth/login", content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<LivestreamAuthResponse>(responseBody);
        }

        // Room Management
        public async Task<RoomResponse> CreateRoomAsync(string token, string roomName, 
            string description, int maxParticipants = 100)
        {
            var roomData = new
            {
                roomName,
                description,
                maxParticipants
            };

            var json = JsonSerializer.Serialize(roomData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.PostAsync($"{_baseUrl}/rooms/create", content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<RoomResponse>(responseBody);
        }

        public async Task<List<RoomResponse>> GetActiveRoomsAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/rooms/active");
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<RoomResponse>>(responseBody);
        }

        public async Task<RoomResponse> JoinRoomAsync(string token, string roomId)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.PostAsync($"{_baseUrl}/rooms/{roomId}/join", null);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<RoomResponse>(responseBody);
        }

        public async Task<RoomResponse> EndRoomAsync(string token, string roomId)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.PostAsync($"{_baseUrl}/rooms/{roomId}/end", null);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<RoomResponse>(responseBody);
        }
    }

    // DTOs
    public class LivestreamAuthResponse
    {
        public string Token { get; set; }
        public string Type { get; set; }
        public long Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
    }

    public class RoomResponse
    {
        public long Id { get; set; }
        public string RoomId { get; set; }
        public string RoomName { get; set; }
        public string Description { get; set; }
        public string TeacherName { get; set; }
        public long TeacherId { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ParticipantCount { get; set; }
        public int MaxParticipants { get; set; }
    }
}
```

## Step 2: Register Service in Program.cs

Add the service registration in `Program.cs`:

```csharp
// Add HttpClient for LivestreamService
builder.Services.AddHttpClient<LivestreamService>();
builder.Services.AddScoped<LivestreamService>();
```

## Step 3: Add Configuration in appsettings.json

Add the livestream service configuration:

```json
{
  "LivestreamService": {
    "BaseUrl": "http://localhost:8080/api"
  }
}
```

## Step 4: Create LivestreamController

Create `DACSN10/Controllers/LivestreamController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DACSN10.Services;
using System.Threading.Tasks;

namespace DACSN10.Controllers
{
    [Authorize]
    public class LivestreamController : Controller
    {
        private readonly LivestreamService _livestreamService;

        public LivestreamController(LivestreamService livestreamService)
        {
            _livestreamService = livestreamService;
        }

        // GET: Livestream
        public async Task<IActionResult> Index()
        {
            try
            {
                var rooms = await _livestreamService.GetActiveRoomsAsync();
                return View(rooms);
            }
            catch
            {
                ViewBag.Error = "Unable to load active rooms";
                return View(new List<RoomResponse>());
            }
        }

        // GET: Livestream/Create
        [Authorize(Roles = "Teacher")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Livestream/Create
        [HttpPost]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> Create(string roomName, string description)
        {
            try
            {
                // Get or create livestream token for current user
                var token = HttpContext.Session.GetString("LivestreamToken");
                
                if (string.IsNullOrEmpty(token))
                {
                    // Register/login the user with the livestream service
                    var user = User.Identity.Name;
                    var authResponse = await _livestreamService.LoginAsync(user, "defaultPassword");
                    token = authResponse.Token;
                    HttpContext.Session.SetString("LivestreamToken", token);
                }

                var room = await _livestreamService.CreateRoomAsync(token, roomName, description);
                return RedirectToAction("Room", new { id = room.RoomId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Failed to create room: " + ex.Message);
                return View();
            }
        }

        // GET: Livestream/Room/{id}
        public IActionResult Room(string id)
        {
            ViewBag.RoomId = id;
            ViewBag.IsTeacher = User.IsInRole("Teacher");
            return View();
        }
    }
}
```

## Step 5: Create Views

### Create `Views/Livestream/Index.cshtml`:

```html
@model List<DACSN10.Services.RoomResponse>
@{
    ViewData["Title"] = "Active Livestreams";
}

<div class="container mt-4">
    <h1>Active Livestreams</h1>

    @if (User.IsInRole("Teacher"))
    {
        <a asp-action="Create" class="btn btn-primary mb-3">
            <i class="fas fa-video"></i> Create New Livestream
        </a>
    }

    @if (!Model.Any())
    {
        <div class="alert alert-info">
            No active livestreams at the moment.
        </div>
    }
    else
    {
        <div class="row">
            @foreach (var room in Model)
            {
                <div class="col-md-4 mb-3">
                    <div class="card">
                        <div class="card-body">
                            <h5 class="card-title">@room.RoomName</h5>
                            <p class="card-text">@room.Description</p>
                            <p class="text-muted">
                                <small>Teacher: @room.TeacherName</small><br>
                                <small>Participants: @room.ParticipantCount / @room.MaxParticipants</small>
                            </p>
                            <a asp-action="Room" asp-route-id="@room.RoomId" class="btn btn-success">
                                <i class="fas fa-sign-in-alt"></i> Join Livestream
                            </a>
                        </div>
                    </div>
                </div>
            }
        </div>
    }
</div>
```

### Create `Views/Livestream/Create.cshtml`:

```html
@{
    ViewData["Title"] = "Create Livestream";
}

<div class="container mt-4">
    <h1>Create New Livestream</h1>

    <form asp-action="Create" method="post">
        <div class="form-group">
            <label for="roomName">Room Name</label>
            <input type="text" class="form-control" id="roomName" name="roomName" required>
        </div>
        <div class="form-group">
            <label for="description">Description</label>
            <textarea class="form-control" id="description" name="description" rows="3"></textarea>
        </div>
        <button type="submit" class="btn btn-primary">
            <i class="fas fa-video"></i> Create Livestream
        </button>
        <a asp-action="Index" class="btn btn-secondary">Cancel</a>
    </form>
</div>
```

### Create `Views/Livestream/Room.cshtml`:

```html
@{
    ViewData["Title"] = "Livestream Room";
    var roomId = ViewBag.RoomId;
    var isTeacher = ViewBag.IsTeacher;
}

<div class="container-fluid mt-3">
    <h2>Livestream Room</h2>
    <div class="row">
        <div class="col-md-8">
            <div class="video-container">
                <video id="localVideo" autoplay muted playsinline style="width: 100%; max-width: 640px; background: #000;"></video>
                <video id="remoteVideo" autoplay playsinline style="width: 100%; max-width: 640px; background: #000;"></video>
            </div>
            <div class="controls mt-3">
                @if (isTeacher)
                {
                    <button id="startStreamBtn" class="btn btn-success">
                        <i class="fas fa-video"></i> Start Streaming
                    </button>
                    <button id="stopStreamBtn" class="btn btn-danger" disabled>
                        <i class="fas fa-stop"></i> Stop Streaming
                    </button>
                }
                else
                {
                    <button id="joinBtn" class="btn btn-primary">
                        <i class="fas fa-sign-in-alt"></i> Join Stream
                    </button>
                    <button id="leaveBtn" class="btn btn-secondary" disabled>
                        <i class="fas fa-sign-out-alt"></i> Leave
                    </button>
                }
            </div>
        </div>
        <div class="col-md-4">
            <div class="card">
                <div class="card-header">
                    <h5>Participants</h5>
                </div>
                <div class="card-body" id="participants">
                    <p class="text-muted">Loading...</p>
                </div>
            </div>
        </div>
    </div>
</div>

@section Scripts {
    <script src="https://cdn.jsdelivr.net/npm/sockjs-client@1/dist/sockjs.min.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/stompjs@2.3.3/lib/stomp.min.js"></script>
    <script>
        const roomId = '@roomId';
        const isTeacher = @(isTeacher ? "true" : "false");
        const wsUrl = 'http://localhost:8080/ws';
        
        let stompClient = null;
        let localStream = null;
        let peerConnection = null;

        // WebSocket connection
        function connect() {
            const socket = new SockJS(wsUrl);
            stompClient = Stomp.over(socket);

            stompClient.connect({}, function(frame) {
                console.log('Connected: ' + frame);
                
                stompClient.subscribe('/topic/signal/' + roomId, function(message) {
                    const signal = JSON.parse(message.body);
                    handleSignal(signal);
                });

                // Notify that we joined
                stompClient.send('/app/join/' + roomId, {}, JSON.stringify({
                    from: '@User.Identity.Name',
                    type: 'join'
                }));
            });
        }

        // Handle signaling messages
        function handleSignal(signal) {
            console.log('Received signal:', signal);
            // Implement WebRTC signaling logic here
        }

        // Start streaming (teacher)
        document.getElementById('startStreamBtn')?.addEventListener('click', async function() {
            try {
                localStream = await navigator.mediaDevices.getUserMedia({
                    video: true,
                    audio: true
                });
                document.getElementById('localVideo').srcObject = localStream;
                
                connect();
                
                this.disabled = true;
                document.getElementById('stopStreamBtn').disabled = false;
            } catch (error) {
                console.error('Error starting stream:', error);
                alert('Failed to access camera/microphone');
            }
        });

        // Stop streaming (teacher)
        document.getElementById('stopStreamBtn')?.addEventListener('click', function() {
            if (localStream) {
                localStream.getTracks().forEach(track => track.stop());
                document.getElementById('localVideo').srcObject = null;
            }
            if (stompClient) {
                stompClient.disconnect();
            }
            
            this.disabled = true;
            document.getElementById('startStreamBtn').disabled = false;
        });

        // Join stream (student)
        document.getElementById('joinBtn')?.addEventListener('click', function() {
            connect();
            this.disabled = true;
            document.getElementById('leaveBtn').disabled = false;
        });

        // Leave stream (student)
        document.getElementById('leaveBtn')?.addEventListener('click', function() {
            if (stompClient) {
                stompClient.send('/app/leave/' + roomId, {}, JSON.stringify({
                    from: '@User.Identity.Name',
                    type: 'leave'
                }));
                stompClient.disconnect();
            }
            
            this.disabled = true;
            document.getElementById('joinBtn').disabled = false;
        });
    </script>
}
```

## Step 6: Add Navigation Link

Add a link to the livestream section in your layout file (`_Layout.cshtml`):

```html
<li class="nav-item">
    <a class="nav-link" asp-controller="Livestream" asp-action="Index">
        <i class="fas fa-video"></i> Livestreams
    </a>
</li>
```

## Step 7: Session Configuration

Ensure session is configured in `Program.cs`:

```csharp
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ... after app builder
app.UseSession();
```

## Testing the Integration

1. Start the Spring Boot service:
   ```bash
   cd livestream-service
   mvn spring-boot:run
   ```

2. Start the ASP.NET MVC application:
   ```bash
   cd DACSN10
   dotnet run
   ```

3. Navigate to the Livestream section in the website
4. As a teacher, create a new livestream room
5. As a student, join an active room

## Notes

- This is a basic integration. For production, implement proper WebRTC peer connection logic
- Consider implementing automatic synchronization of users between both systems
- Add proper error handling and user feedback
- Implement authentication synchronization or single sign-on
- For production deployment, use HTTPS for both services
- Consider implementing STUN/TURN servers for NAT traversal in WebRTC
