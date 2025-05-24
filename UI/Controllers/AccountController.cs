using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using UI.Models.ViewModels;
using UI.Services;

namespace UI.Controllers
{
    public class AccountController : Controller // Залишаємо Controller, не BaseController
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
                // Зберігаємо дані в сесії
                HttpContext.Session.SetString("IsAuthenticated", "true");
                HttpContext.Session.SetString("UserData", JsonConvert.SerializeObject(result.Data.User));

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
                // Зберігаємо дані в сесії
                HttpContext.Session.SetString("IsAuthenticated", "true");

                // ApiService тепер правильно парсить UserId
                string userId = result.Data.User.UserId ?? result.Data.User.Id ?? result.Data.User.Email;

                _logger.LogInformation("Login successful - UserId: {UserId}, Email: {Email}", userId, result.Data.User.Email);

                // Зберігаємо повні дані користувача включаючи правильний UserId
                HttpContext.Session.SetString("UserData", JsonConvert.SerializeObject(result.Data.User));

                _logger.LogInformation("Saved user data to session with UserId: {UserId}", userId);

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
            // Очищаємо сесію
            HttpContext.Session.Clear();

            // Також викликаємо logout на API (опціонально)
            await _apiService.LogoutAsync();

            TempData["SuccessMessage"] = "Вихід виконано успішно!";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> KeepAlive()
        {
            try
            {
                var isAuthenticated = HttpContext.Session.GetString("IsAuthenticated") == "true";

                if (!isAuthenticated)
                {
                    return Json(new { success = false, message = "Not authenticated" });
                }

                // Викликаємо API для оновлення cookie
                var result = await _apiService.RefreshSessionAsync();

                if (result.Success)
                {
                    _logger.LogInformation("KeepAlive: Session refreshed successfully");
                    return Json(new { success = true, message = "Session refreshed" });
                }
                else
                {
                    _logger.LogWarning("KeepAlive: Session refresh failed");
                    // Очищаємо сесію якщо оновлення не вдалося
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