
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Twitter;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DACSN10.Controllers
{
    public class AuthController : Controller
    {
        public IActionResult Login(string provider)
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Auth");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };

            return provider switch
            {
                "Google" => Challenge(properties, GoogleDefaults.AuthenticationScheme),
                "Facebook" => Challenge(properties, FacebookDefaults.AuthenticationScheme),
                "Twitter" => Challenge(properties, TwitterDefaults.AuthenticationScheme),
                _ => RedirectToAction("Index", "Home")
            };
        }

        public async Task<IActionResult> ExternalLoginCallback()
        {
            var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!authenticateResult.Succeeded)
                return RedirectToAction("LoginFailed");

            // Tại đây bạn có thể xử lý đăng nhập, tạo tài khoản, lưu dữ liệu người dùng nếu cần

            return RedirectToAction("Index", "Home");
        }

        public IActionResult LoginFailed()
        {
            return View("LoginFailed");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}
