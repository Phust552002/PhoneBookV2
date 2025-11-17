using PhoneBook.Models;
using PhoneBook.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace PhoneBook.Controllers
{
    public class AccountController : Controller
    {
        private readonly IApiService _apiService;

        public AccountController(IApiService apiService)
        {
            _apiService = apiService;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // B1: Đăng nhập với tài khoản admin (admin::123) để lấy JWT token
                // B2: Dùng JWT token để gọi API Account/login với username/password của user
                // Kiểm tra response từ API
                // Lấy User từ response
                // B3: Tạo Claims từ thông tin user nhận được từ API
                // Lưu JWT token để dùng cho các request sau
                // Thêm roles vào claims
                // B4: Tạo Cookie Authentication
                // B5: Redirect sau khi đăng nhập thành công
                var jwtToken = await _apiService.GetAdminTokenAsync();

                if (string.IsNullOrEmpty(jwtToken))
                {
                    ModelState.AddModelError(string.Empty, "Không thể kết nối với hệ thống xác thực.");
                    return View(model);
                }

                var loginResponse = await _apiService.LoginWithTokenAsync(
                    model.Username,
                    model.Password,
                    model.RememberMe
                );

                if (loginResponse == null || loginResponse.User == null)
                {
                    ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
                    return View(model);
                }

                var userInfo = loginResponse.User; 

                
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userInfo.UserId.ToString()),
                    new Claim(ClaimTypes.Name, userInfo.Username ?? ""),
                    new Claim("FullName", userInfo.FullName ?? userInfo.Username ?? ""),
                    new Claim("EmployeeCode", userInfo.EmployeeCode ?? ""),
                    new Claim("PositionName", userInfo.PositionName ?? "Nhân viên"),
                    new Claim("DepartmentId", userInfo.DepartmentId?.ToString() ?? "0"),
                    new Claim("IsAdmin", userInfo.IsAdmin.ToString()),
                    new Claim("JwtToken", jwtToken) 
                };

                
                if (userInfo.Roles != null && userInfo.Roles.Any())
                {
                    foreach (var roleId in userInfo.Roles)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, roleId.ToString()));
                    }
                }

                
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(1) : DateTimeOffset.UtcNow.AddHours(1)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                System.Threading.Thread.Sleep(1000);
                return RedirectToAction("Index", "Home");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, $"Lỗi kết nối : {ex.Message}");
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"lỗi: {ex.Message}");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
                var token = User.FindFirst("JwtToken")?.Value;
                if (!string.IsNullOrEmpty(token))
                {
                    _apiService.SetToken(token);
                    // await _apiService.PostAsync<object>("/api/Account/logout", null);
                }


            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            Response.Cookies.Delete(".AspNetCore.Cookies");
            HttpContext.Session.Clear();
            System.Threading.Thread.Sleep(500);
            return RedirectToAction("Login", "Account");
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}