using System.Security.Claims;
using DACSN10.Models;
using DACSN10.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Yarp.ReverseProxy;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day);
});

builder.Services.AddScoped<IEmailService, EmailService>();

// MVC + Razor Pages (Identity UI)
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// HttpClientFactory + named client "LiveApi"
builder.Services.AddHttpClient();
builder.Services.AddHttpClient("LiveApi", (sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var baseUrl = (config["LiveStreamApi:BaseUrl"] ?? "").TrimEnd('/');

    // Fallback nội bộ nếu chưa cấu hình
    if (string.IsNullOrWhiteSpace(baseUrl))
        baseUrl = "http://localhost:8080";

    client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// YARP reverse proxy: định nghĩa route/cluster bằng code và loại prefix "/spring"
var routes = new[]
{
    new RouteConfig
    {
        RouteId = "spring-all",
        ClusterId = "spring",
        Match = new RouteMatch { Path = "/spring/{**catch-all}" },
        Transforms = new[]
        {
            new Dictionary<string, string> { ["PathRemovePrefix"] = "/spring" }
        }
    }
};

var clusters = new[]
{
    new ClusterConfig
    {
        ClusterId = "spring",
        Destinations = new Dictionary<string, DestinationConfig>
        {
            ["d1"] = new DestinationConfig { Address = "http://localhost:8080/" }
        }
    }
};

builder.Services.AddReverseProxy()
    .LoadFromMemory(routes, clusters);

// CORS cho dev
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecific", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()));

// Identity + Roles
builder.Services
    .AddIdentity<User, IdentityRole>(options =>
    {
        // Cho phép confirm email nếu bạn muốn; giữ nguyên cấu hình hiện có
        options.SignIn.RequireConfirmedAccount = true;

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = false;

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders()
    .AddDefaultUI();

// Cookie paths (giúp redirect chính xác khi AccessDenied/Login)
builder.Services.ConfigureApplicationCookie(opt =>
{
    opt.LoginPath = "/Identity/Account/Login";
    opt.AccessDeniedPath = "/Identity/Account/AccessDenied";
    opt.SlidingExpiration = true;
    opt.Cookie.HttpOnly = true;
    opt.Cookie.SameSite = SameSiteMode.Lax;
});

// External auth
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
        options.CallbackPath = "/signin-google";
        options.Scope.Add("email");
        options.Scope.Add("profile");
        options.SaveTokens = true;
        options.ClaimActions.MapJsonKey("urn:google:picture", "picture", "url");
    })
    .AddFacebook(options =>
    {
        options.AppId = builder.Configuration["Authentication:Facebook:AppId"];
        options.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
        options.CallbackPath = "/signin-facebook";
        options.Scope.Add("public_profile");
        options.Scope.Add("email");
        options.Fields.Add("name");
        options.Fields.Add("email");
        options.SaveTokens = true;
    });

// Authorization
// LƯU Ý: Chuyển policy sang kiểm tra ROLE thay vì chỉ Claim "LoaiNguoiDung"
// vẫn giữ fallback theo claim để tương thích dữ liệu cũ.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.IsInRole(RoleNames.Admin) ||
            ctx.User.HasClaim("LoaiNguoiDung", RoleNames.Admin) ||
            ctx.User.HasClaim(ClaimTypes.Role, RoleNames.Admin)
        ));

    options.AddPolicy("TeacherOrAdmin", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.IsInRole(RoleNames.Teacher) ||
            ctx.User.IsInRole(RoleNames.Admin) ||
            ctx.User.HasClaim("LoaiNguoiDung", RoleNames.Teacher) ||
            ctx.User.HasClaim("LoaiNguoiDung", RoleNames.Admin) ||
            ctx.User.HasClaim(ClaimTypes.Role, RoleNames.Teacher) ||
            ctx.User.HasClaim(ClaimTypes.Role, RoleNames.Admin)
        ));

    options.AddPolicy("UserOnly", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.IsInRole(RoleNames.User) ||
            ctx.User.HasClaim("LoaiNguoiDung", RoleNames.User) ||
            ctx.User.HasClaim(ClaimTypes.Role, RoleNames.User)
        ));
});

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto
});

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// CORS
app.UseCors("AllowSpecific");

app.UseAuthentication();
app.UseAuthorization();

// Bật WebSockets trước proxy để hỗ trợ WS/SockJS
app.UseWebSockets();

// Map reverse proxy (/spring/** -> http://localhost:8080/**, đã bỏ prefix "/spring")
app.MapReverseProxy();

// Seed data (roles + tài khoản admin/teacher mẫu để test policy)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<User>>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        // Tạo roles nếu chưa có
        foreach (var roleName in new[] { RoleNames.Admin, RoleNames.Teacher, RoleNames.User })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var res = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (!res.Succeeded)
                    logger.LogError("Failed to create role {Role}: {Errors}", roleName, string.Join(", ", res.Errors.Select(e => e.Description)));
            }
        }

        // Admin mẫu
        var adminEmail = "admin@example.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new User
            {
                UserName = adminEmail,
                Email = adminEmail,
                HoTen = "Quản trị viên",
                NgayDangKy = DateTime.Now,
                TrangThai = "Active",
                LoaiNguoiDung = RoleNames.Admin,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, RoleNames.Admin);
                // giữ claim tương thích nếu code cũ còn dùng
                await userManager.AddClaimAsync(adminUser, new Claim("LoaiNguoiDung", RoleNames.Admin));
            }
            else
            {
                logger.LogError("Failed to create admin user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            // đảm bảo role/claim tồn tại
            if (!await userManager.IsInRoleAsync(adminUser, RoleNames.Admin))
                await userManager.AddToRoleAsync(adminUser, RoleNames.Admin);
            var claims = await userManager.GetClaimsAsync(adminUser);
            if (!claims.Any(c => c.Type == "LoaiNguoiDung"))
                await userManager.AddClaimAsync(adminUser, new Claim("LoaiNguoiDung", RoleNames.Admin));
        }

        // Teacher mẫu
        var teacherEmail = "teacher@example.com";
        var teacherUser = await userManager.FindByEmailAsync(teacherEmail);
        if (teacherUser == null)
        {
            teacherUser = new User
            {
                UserName = teacherEmail,
                Email = teacherEmail,
                HoTen = "Teacher 0",
                NgayDangKy = DateTime.Now,
                TrangThai = "Active",
                LoaiNguoiDung = RoleNames.Teacher,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(teacherUser, "Teacher@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(teacherUser, RoleNames.Teacher);
                await userManager.AddClaimAsync(teacherUser, new Claim("LoaiNguoiDung", RoleNames.Teacher));
            }
            else
            {
                logger.LogError("Failed to create teacher user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            if (!await userManager.IsInRoleAsync(teacherUser, RoleNames.Teacher))
                await userManager.AddToRoleAsync(teacherUser, RoleNames.Teacher);
            var claims = await userManager.GetClaimsAsync(teacherUser);
            if (!claims.Any(c => c.Type == "LoaiNguoiDung"))
                await userManager.AddClaimAsync(teacherUser, new Claim("LoaiNguoiDung", RoleNames.Teacher));
        }
    }
    catch (Exception ex)
    {
        services.GetRequiredService<ILogger<Program>>()
            .LogError(ex, "An error occurred during seeding data");
    }
}

// Routes
app.MapControllerRoute(
    name: "Admin",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "Teacher",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();