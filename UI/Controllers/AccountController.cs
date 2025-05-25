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
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // ДОДАНО: GET метод для Register
        [HttpGet]
        public IActionResult Register()
        {
            _logger.LogInformation("GET Register page requested");
            return View(new RegisterViewModel());
        }

        // ВИПРАВЛЕНО: POST метод для Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            _logger.LogInformation("POST Register attempt for email: {Email}", model?.Email);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Register model state invalid");
                return View(model);
            }

            try
            {
                var result = await _apiService.RegisterAsync(model);

                if (result.Success)
                {
                    _logger.LogInformation("Registration successful for {Email}", model.Email);

                    // Створюємо claims для cookie з УСІМА необхідними даними
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, result.Data.User.UserId ?? result.Data.User.Id ?? result.Data.User.Email),
                        new Claim(ClaimTypes.Email, result.Data.User.Email),
                        new Claim(ClaimTypes.Name, result.Data.User.Email),
                        new Claim("FirstName", result.Data.User.FirstName ?? ""),
                        new Claim("LastName", result.Data.User.LastName ?? ""),
                        new Claim("LoginTime", DateTimeOffset.UtcNow.ToString())
                    };

                    // Додаємо ролі як claims
                    foreach (var role in result.Data.User.Roles ?? new List<string>())
                    {
                        claims.Add(new Claim(ClaimTypes.Role, role));
                        _logger.LogInformation("Adding role to claims: {Role}", role);
                    }

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = false, // Для реєстрації не зберігаємо надовго
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
                        IssuedUtc = DateTimeOffset.UtcNow,
                        AllowRefresh = true
                    };

                    // ВСТАНОВЛЮЄМО AUTHENTICATION COOKIE
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity), authProperties);

                    _logger.LogInformation("Authentication cookie created for new user: {Email}", model.Email);

                    // Зберігаємо дані в сесії для сумісності
                    HttpContext.Session.SetString("IsAuthenticated", "true");
                    HttpContext.Session.SetString("UserData", JsonConvert.SerializeObject(result.Data.User));
                    HttpContext.Session.SetString("LoginTime", DateTimeOffset.UtcNow.ToString());

                    TempData["SuccessMessage"] = "Реєстрацію завершено успішно! Ласкаво просимо!";
                    return RedirectToAction("Index", "Home");
                }

                // Обробка помилок від API
                if (result.Errors?.Any() == true)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                }
                else
                {
                    ModelState.AddModelError("", result.Message ?? "Помилка реєстрації");
                }

                _logger.LogWarning("Registration failed for {Email}: {Message}", model.Email, result.Message);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during registration for {Email}", model?.Email);
                ModelState.AddModelError("", "Сталася помилка під час реєстрації. Спробуйте пізніше.");
                return View(model);
            }
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
                _logger.LogInformation("API login successful for {Email}", model.Email);
                _logger.LogInformation("User roles from API: {Roles}",
                    string.Join(", ", result.Data.User.Roles ?? new List<string>()));

                // Створюємо claims для cookie з УСІМА необхідними даними
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, result.Data.User.UserId ?? result.Data.User.Id ?? result.Data.User.Email),
                    new Claim(ClaimTypes.Email, result.Data.User.Email),
                    new Claim(ClaimTypes.Name, result.Data.User.Email),
                    new Claim("FirstName", result.Data.User.FirstName ?? ""),
                    new Claim("LastName", result.Data.User.LastName ?? ""),
                    new Claim("LoginTime", DateTimeOffset.UtcNow.ToString()) // Для діагностики
                };

                // Додаємо ролі як claims
                foreach (var role in result.Data.User.Roles ?? new List<string>())
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                    _logger.LogInformation("Adding role to MVC claims: {Role}", role);
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(model.RememberMe ? 24 * 7 : 8), // 8 годин або 1 тиждень
                    IssuedUtc = DateTimeOffset.UtcNow,
                    AllowRefresh = true // ВАЖЛИВО для sliding expiration
                };

                // ВСТАНОВЛЮЄМО AUTHENTICATION COOKIE
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity), authProperties);

                _logger.LogInformation("MVC authentication cookie created for {Email}. Expires: {Expires}",
                    model.Email, authProperties.ExpiresUtc);

                // Зберігаємо дані в сесії для сумісності
                HttpContext.Session.SetString("IsAuthenticated", "true");
                HttpContext.Session.SetString("UserData", JsonConvert.SerializeObject(result.Data.User));
                HttpContext.Session.SetString("LoginTime", DateTimeOffset.UtcNow.ToString());

                _logger.LogInformation("Session data saved for {Email}", model.Email);

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
            var userEmail = User.Identity?.Name;
            _logger.LogInformation("User logout: {Email}", userEmail);

            // Очищаємо authentication cookie
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Очищаємо сесію
            HttpContext.Session.Clear();

            // Викликаємо logout на API (опціонально)
            try
            {
                await _apiService.LogoutAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calling API logout for {Email}", userEmail);
            }

            _logger.LogInformation("User {Email} successfully logged out", userEmail);
            TempData["SuccessMessage"] = "Вихід виконано успішно!";
            return RedirectToAction("Index", "Home");
        }

        // НОВИЙ ENDPOINT для діагностики
        [HttpGet]
        public IActionResult AuthStatus()
        {
            var authTime = User.FindFirst("LoginTime")?.Value;
            var authTimeUtc = !string.IsNullOrEmpty(authTime) ? DateTimeOffset.Parse(authTime) : (DateTimeOffset?)null;
            var timeLoggedIn = authTimeUtc.HasValue ? DateTimeOffset.UtcNow - authTimeUtc.Value : (TimeSpan?)null;

            var status = new
            {
                IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
                UserName = User.Identity?.Name,
                Email = User.FindFirst(ClaimTypes.Email)?.Value,
                Roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray(),
                LoginTime = authTime,
                TimeLoggedIn = timeLoggedIn?.ToString(@"hh\:mm\:ss"),
                SessionExists = HttpContext.Session.GetString("IsAuthenticated") == "true",
                Cookies = Request.Cookies.Select(c => new { c.Key, ValueLength = c.Value.Length }).ToArray()
            };

            return Json(status);
        }

        [HttpPost]
        public async Task<IActionResult> KeepAlive()
        {
            try
            {
                var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
                _logger.LogInformation("KeepAlive called. IsAuthenticated: {IsAuth}, User: {User}",
                    isAuthenticated, User.Identity?.Name ?? "Anonymous");

                if (!isAuthenticated)
                {
                    return Json(new { success = false, message = "Not authenticated" });
                }

                // Автоматично оновлюємо authentication cookie
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, User,
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
                        IssuedUtc = DateTimeOffset.UtcNow,
                        AllowRefresh = true
                    });

                // Викликаємо API для оновлення
                var result = await _apiService.RefreshSessionAsync();
                _logger.LogInformation("API session refresh result: {Success}", result.Success);

                if (result.Success)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Session refreshed",
                        expiresAt = DateTimeOffset.UtcNow.AddHours(8)
                    });
                }
                else
                {
                    _logger.LogWarning("API session refresh failed");
                    return Json(new { success = false, message = "Session refresh failed" });
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