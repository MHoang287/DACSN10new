using DACSN10.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Configure DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Identity with optimized settings
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();

// Cấu hình cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.SlidingExpiration = true;
});

// Cấu hình xác thực Google và Facebook
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
.AddGoogle(googleOptions =>
{
    googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    googleOptions.CallbackPath = "/signin-google";

    // Thêm các scope cần thiết
    googleOptions.Scope.Add("email");
    googleOptions.Scope.Add("profile");

    // Lưu tokens để sử dụng sau này
    googleOptions.SaveTokens = true;

    // Cấu hình thêm claims
    googleOptions.ClaimActions.MapJsonKey("urn:google:picture", "picture", "url");
    googleOptions.ClaimActions.MapJsonKey("urn:google:locale", "locale", "string");
})
.AddFacebook(facebookOptions =>
{
    facebookOptions.AppId = builder.Configuration["Authentication:Facebook:AppId"];
    facebookOptions.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
    facebookOptions.CallbackPath = "/signin-facebook";
    facebookOptions.SaveTokens = true;

    // Thêm scope để lấy thêm thông tin
    facebookOptions.Scope.Add("public_profile");
    facebookOptions.Scope.Add("email");

    // Facebook API version
    facebookOptions.Fields.Add("name");
    facebookOptions.Fields.Add("email");
});

// Simplified authorization using LoaiNguoiDung
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c =>
                c.Type == "LoaiNguoiDung" && c.Value == RoleNames.Admin)));

    options.AddPolicy("TeacherOrAdmin", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c =>
                c.Type == "LoaiNguoiDung" &&
                (c.Value == RoleNames.Admin || c.Value == RoleNames.Teacher))));
});

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto
});

// Configure the HTTP pipeline
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
app.UseAuthentication();
app.UseAuthorization();

// Seed data with LoaiNguoiDung and Role synchronization
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<User>>();

        // Ensure roles exist
        foreach (var roleName in new[] { RoleNames.Admin, RoleNames.Teacher, RoleNames.User })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // Create admin user if not exists
        var adminEmail = "admin@example.com";
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var adminUser = new User
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
                // Add both role and claim for consistency
                await userManager.AddToRoleAsync(adminUser, RoleNames.Admin);
                await userManager.AddClaimAsync(adminUser,
                    new System.Security.Claims.Claim("LoaiNguoiDung", RoleNames.Admin));
            }
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Seed data error");
    }
}

// Route configuration
app.MapControllerRoute(
    name: "Admin",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();