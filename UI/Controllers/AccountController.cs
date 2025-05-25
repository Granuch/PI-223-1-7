using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Newtonsoft.Json;
using System.Security.Claims;
using UI.Models.ViewModels;
using UI.Services;

namespace UI.Controllers
{
    public class AccountController : Controller
    {
        private readonly IApiService _apiService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IApiService apiService, ILogger<AccountController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _apiService.RegisterAsync(model);

            if (result.Success)
            {
                // Створюємо claims для cookie
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, result.Data.User.UserId ?? result.Data.User.Id),
                    new Claim(ClaimTypes.Email, result.Data.User.Email),
                    new Claim(ClaimTypes.Name, result.Data.User.Email),
                    new Claim("FirstName", result.Data.User.FirstName ?? ""),
                    new Claim("LastName", result.Data.User.LastName ?? "")
                };

                // Додаємо ролі
                foreach (var role in result.Data.User.Roles ?? new List<string>())
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = false,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1)
                };

                // ВСТАНОВЛЮЄМО AUTHENTICATION COOKIE
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity), authProperties);

                // Зберігаємо дані в сесії для сумісності
                HttpContext.Session.SetString("IsAuthenticated", "true");
                HttpContext.Session.SetString("UserData", JsonConvert.SerializeObject(result.Data.User));

                _logger.LogInformation("User registered and signed in: {Email}", model.Email);
                TempData["SuccessMessage"] = "Реєстрація пройшла успішно!";
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", result.Message);
            return View(model);
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            var model = new LoginViewModel { ReturnUrl = returnUrl };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (string.IsNullOrEmpty(model.ReturnUrl))
            {
                model.ReturnUrl = "/";
            }

            var result = await _apiService.LoginAsync(model);

            if (result.Success)
            {
                // Створюємо claims для cookie
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, result.Data.User.UserId ?? result.Data.User.Id ?? result.Data.User.Email),
                    new Claim(ClaimTypes.Email, result.Data.User.Email),
                    new Claim(ClaimTypes.Name, result.Data.User.Email),
                    new Claim("FirstName", result.Data.User.FirstName ?? ""),
                    new Claim("LastName", result.Data.User.LastName ?? "")
                };

                // Додаємо ролі
                foreach (var role in result.Data.User.Roles ?? new List<string>())
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                    _logger.LogInformation("Adding role to claims: {Role}", role);
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(model.RememberMe ? 30 : 1)
                };

                // ВСТАНОВЛЮЄМО AUTHENTICATION COOKIE
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity), authProperties);

                // Зберігаємо дані в сесії для сумісності
                HttpContext.Session.SetString("IsAuthenticated", "true");
                HttpContext.Session.SetString("UserData", JsonConvert.SerializeObject(result.Data.User));

                _logger.LogInformation("User logged in and signed in: {Email}", model.Email);
                _logger.LogInformation("User roles: {Roles}", string.Join(", ", result.Data.User.Roles ?? new List<string>()));

                TempData["SuccessMessage"] = "Вхід виконано успішно!";

                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", result.Message);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Очищаємо authentication cookie
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Очищаємо сесію
            HttpContext.Session.Clear();

            // Викликаємо logout на API (опціонально)
            await _apiService.LogoutAsync();

            _logger.LogInformation("User logged out");
            TempData["SuccessMessage"] = "Вихід виконано успішно!";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> KeepAlive()
        {
            try
            {
                var isAuthenticated = User.Identity?.IsAuthenticated ?? false;

                if (!isAuthenticated)
                {
                    return Json(new { success = false, message = "Not authenticated" });
                }

                // Викликаємо API для оновлення
                var result = await _apiService.RefreshSessionAsync();

                if (result.Success)
                {
                    return Json(new { success = true, message = "Session refreshed" });
                }
                else
                {
                    // Очищаємо authentication якщо оновлення не вдалося
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    HttpContext.Session.Clear();
                    return Json(new { success = false, message = "Session expired" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in KeepAlive");
                return Json(new { success = false, message = "Error refreshing session" });
            }
        }
    }
}