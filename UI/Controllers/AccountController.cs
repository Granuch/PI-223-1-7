using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
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

        [HttpGet]
        public IActionResult Register()
        {
            _logger.LogInformation("GET Register page requested");
            return View(new RegisterViewModel());
        }

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

                    // JWT токени вже збережені в Session через ApiService
                    // Витягуємо claims з токену
                    var accessToken = HttpContext.Session.GetString("AccessToken");
                    var claims = ExtractClaimsFromToken(accessToken);

                    if (claims != null)
                    {
                        _logger.LogInformation("User authenticated with JWT. Roles: {Roles}",
                            string.Join(", ", claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value)));
                    }

                    TempData["SuccessMessage"] = "Registration completed successfully! Welcome!";
                    return RedirectToAction("Index", "Home");
                }

                if (result.Errors?.Any() == true)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                }
                else
                {
                    ModelState.AddModelError("", result.Message ?? "Registration error");
                }

                _logger.LogWarning("Registration failed for {Email}: {Message}", model.Email, result.Message);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during registration for {Email}", model?.Email);
                ModelState.AddModelError("", "An error occurred during registration. Please try again later.");
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

                // ДОДАЙТЕ ЦЕ ЛОГУВАННЯ
                var accessToken = HttpContext.Session.GetString("AccessToken");
                _logger.LogInformation("AccessToken in session: {HasToken}", !string.IsNullOrEmpty(accessToken));

                if (!string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogInformation("Token length: {Length}", accessToken.Length);
                }

                var claims = ExtractClaimsFromToken(accessToken);

                if (claims != null)
                {
                    var roles = claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);
                    _logger.LogInformation("User roles from JWT: {Roles}", string.Join(", ", roles));
                }
                else
                {
                    _logger.LogWarning("Failed to extract claims from token!");
                }

                TempData["SuccessMessage"] = "Login successful!";

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
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userEmail = User.Identity?.Name;
            _logger.LogInformation("User logout: {Email}", userEmail);

            // Очищаємо токени з Session
            HttpContext.Session.Remove("AccessToken");
            HttpContext.Session.Remove("RefreshToken");
            HttpContext.Session.Remove("TokenExpiry");
            HttpContext.Session.Clear();

            try
            {
                await _apiService.LogoutAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calling API logout for {Email}", userEmail);
            }

            _logger.LogInformation("User {Email} successfully logged out", userEmail);
            TempData["SuccessMessage"] = "Logout successful!";
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AuthStatus()
        {
            var accessToken = HttpContext.Session.GetString("AccessToken");
            var isAuthenticated = !string.IsNullOrEmpty(accessToken);

            List<Claim> claims = null;
            if (isAuthenticated)
            {
                claims = ExtractClaimsFromToken(accessToken);
            }

            var authTime = claims?.FirstOrDefault(c => c.Type == "iat")?.Value;
            DateTimeOffset? authTimeUtc = null;
            TimeSpan? timeLoggedIn = null;

            if (!string.IsNullOrEmpty(authTime) && long.TryParse(authTime, out var iat))
            {
                authTimeUtc = DateTimeOffset.FromUnixTimeSeconds(iat);
                timeLoggedIn = DateTimeOffset.UtcNow - authTimeUtc.Value;
            }

            var status = new
            {
                IsAuthenticated = isAuthenticated,
                UserName = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value,
                Email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value,
                Roles = claims?.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray() ?? Array.Empty<string>(),
                LoginTime = authTimeUtc?.ToString("O"),
                TimeLoggedIn = timeLoggedIn?.ToString(@"hh\:mm\:ss"),
                TokenExpiry = HttpContext.Session.GetString("TokenExpiry"),
                HasRefreshToken = !string.IsNullOrEmpty(HttpContext.Session.GetString("RefreshToken"))
            };

            return Json(status);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> KeepAlive()
        {
            try
            {
                var accessToken = HttpContext.Session.GetString("AccessToken");
                var isAuthenticated = !string.IsNullOrEmpty(accessToken);

                _logger.LogInformation("KeepAlive called. IsAuthenticated: {IsAuth}", isAuthenticated);

                if (!isAuthenticated)
                {
                    return Json(new { success = false, message = "Not authenticated" });
                }

                var result = await _apiService.RefreshSessionAsync();
                _logger.LogInformation("API session refresh result: {Success}", result.Success);

                if (result.Success)
                {
                    var tokenExpiry = HttpContext.Session.GetString("TokenExpiry");
                    return Json(new
                    {
                        success = true,
                        message = "Session refreshed",
                        expiresAt = tokenExpiry
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

        // Helper method to extract claims from JWT token
        private List<Claim> ExtractClaimsFromToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                return null;

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                return jwtToken.Claims.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting claims from token");
                return null;
            }
        }
    }
}