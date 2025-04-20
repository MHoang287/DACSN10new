using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using DACSN10.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace DACSN10.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly UserManager<User> _userManager;

        public LoginModel(SignInManager<User> signInManager, ILogger<LoginModel> logger, UserManager<User> userManager)
        {
            _signInManager = signInManager;
            _logger = logger;
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Email là bắt buộc")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ")]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
            [DataType(DataType.Password)]
            [Display(Name = "Mật khẩu")]
            public string Password { get; set; }

            [Display(Name = "Ghi nhớ đăng nhập?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Xóa cookie xác thực bên ngoài đã tồn tại
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            // Lấy danh sách nhà cung cấp đăng nhập bên ngoài
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            // Lấy danh sách nhà cung cấp đăng nhập bên ngoài
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                // Kiểm tra tài khoản trước khi đăng nhập
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user != null && user.TrangThai != "Active")
                {
                    ModelState.AddModelError(string.Empty, "Tài khoản đã bị khóa hoặc chưa được kích hoạt.");
                    return Page();
                }

                // Đăng nhập với mật khẩu
                var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Người dùng đã đăng nhập thành công.");

                    // Lưu thông tin đăng nhập vào localStorage (nếu cần)
                    TempData["StoreLoginInfo"] = $@"
                        localStorage.setItem('email', '{Input.Email}');
                        // Không lưu mật khẩu và các thông tin nhạy cảm
                    ";

                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("Tài khoản bị khóa.");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Đăng nhập không thành công. Vui lòng kiểm tra lại thông tin.");
                    return Page();
                }
            }

            // Nếu có lỗi, hiển thị lại form
            return Page();
        }

        // Thêm phương thức xử lý đăng nhập bên ngoài
        public IActionResult OnPostExternalLogin(string provider, string returnUrl = null)
        {
            // Yêu cầu chuyển hướng đến nhà cung cấp đăng nhập bên ngoài
            var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }
    }
}