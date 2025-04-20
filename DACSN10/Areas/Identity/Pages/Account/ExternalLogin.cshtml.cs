using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using DACSN10.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace DACSN10.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ExternalLoginModel> _logger;

        public ExternalLoginModel(
            SignInManager<User> signInManager,
            UserManager<User> userManager,
            ILogger<ExternalLoginModel> logger,
            IEmailSender emailSender)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ProviderDisplayName { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Email là bắt buộc")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Họ tên là bắt buộc")]
            [Display(Name = "Họ và tên")]
            public string HoTen { get; set; }
        }

        public IActionResult OnGetAsync()
        {
            return RedirectToPage("./Login");
        }

        public IActionResult OnPost(string provider, string returnUrl = null)
        {
            // Chuyển hướng tới trang callback sau khi đăng nhập với Google
            var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        public async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            if (remoteError != null)
            {
                ErrorMessage = $"Lỗi từ nhà cung cấp bên ngoài: {remoteError}";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Lỗi khi tải thông tin đăng nhập bên ngoài.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            // Sign in the user with this external login provider if the user already has a login.
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                _logger.LogInformation("{Name} đã đăng nhập với {LoginProvider}.", info.Principal.Identity.Name, info.LoginProvider);
                return LocalRedirect(returnUrl);
            }
            if (result.IsLockedOut)
            {
                return RedirectToPage("./Lockout");
            }
            else
            {
                // If the user does not have an account, then ask the user to create an account.
                ReturnUrl = returnUrl;
                ProviderDisplayName = info.ProviderDisplayName;

                // Lấy thông tin từ claims
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                var name = info.Principal.FindFirstValue(ClaimTypes.Name);

                // Nếu không có email từ claims, thử lấy từ identity
                if (string.IsNullOrEmpty(email))
                {
                    email = info.Principal.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress");
                }

                // Nếu vẫn không có, sử dụng tên đăng nhập
                if (string.IsNullOrEmpty(email))
                {
                    email = info.Principal.Identity?.Name;
                }

                Input = new InputModel
                {
                    Email = email,
                    HoTen = name ?? "Người dùng Google"
                };

                return Page();
            }
        }

        public async Task<IActionResult> OnPostConfirmationAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            // Lấy thông tin từ nhà cung cấp bên ngoài mà chúng ta đã lưu trữ trong cookie
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Lỗi khi tải thông tin đăng nhập bên ngoài trong quá trình xác nhận.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            if (ModelState.IsValid)
            {
                // Kiểm tra email đã tồn tại chưa
                var existingUser = await _userManager.FindByEmailAsync(Input.Email);
                if (existingUser != null)
                {
                    // Nếu người dùng đã tồn tại, liên kết tài khoản bên ngoài với tài khoản hiện có
                    var addLoginResult = await _userManager.AddLoginAsync(existingUser, info);
                    if (addLoginResult.Succeeded)
                    {
                        await _signInManager.SignInAsync(existingUser, isPersistent: false, info.LoginProvider);
                        return LocalRedirect(returnUrl);
                    }
                    else
                    {
                        foreach (var error in addLoginResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        return Page();
                    }
                }

                // Tạo người dùng mới nếu chưa tồn tại
                var user = new User
                {
                    UserName = Input.Email,
                    Email = Input.Email,
                    HoTen = Input.HoTen,
                    NgayDangKy = DateTime.Now,
                    TrangThai = "Active",
                    LoaiNguoiDung = RoleNames.User,
                    EmailConfirmed = true // Đánh dấu là đã xác nhận email vì đã xác thực qua nhà cung cấp bên ngoài
                };

                var result = await _userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    // Gán vai trò người dùng
                    await _userManager.AddToRoleAsync(user, RoleNames.User);

                    // Thêm thông tin đăng nhập bên ngoài
                    result = await _userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("Người dùng đã tạo tài khoản bằng nhà cung cấp {Name}.", info.LoginProvider);

                        // Đăng nhập sau khi tạo tài khoản thành công
                        await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);

                        // Xóa cookie xác thực bên ngoài
                        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

                        return LocalRedirect(returnUrl);
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ProviderDisplayName = info.ProviderDisplayName;
            ReturnUrl = returnUrl;
            return Page();
        }
    }
}