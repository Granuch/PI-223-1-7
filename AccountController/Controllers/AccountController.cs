using Mapping.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using PI_223_1_7.Models;
using PL.ViewModels;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PL.Controllers
{
    [Route("api/account")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            IConfiguration configuration,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterViewModel model)
        {
            _logger.LogInformation($"Отримано запит на реєстрацію для {model.Email}");

            if (ModelState.IsValid)
            {
                // Перевіряємо, чи не існує вже такий користувач
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    _logger.LogWarning($"Користувач з email {model.Email} вже існує");
                    return BadRequest(new
                    {
                        success = false,
                        message = "Користувач з таким email вже існує"
                    });
                }

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation($"Користувач {user.Email} успішно зареєстрований");

                    // Додаємо роль
                    await _userManager.AddToRoleAsync(user, "RegisteredUser");

                    // ДОДАНО: Виконуємо Cookie вхід для MVC частини
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    // ДОДАНО: Генеруємо JWT токен для API
                    var token = await GenerateJwtToken(user);
                    var roles = await _userManager.GetRolesAsync(user);

                    _logger.LogInformation($"Автоматичний вхід для {user.Email} після реєстрації");

                    return Ok(new
                    {
                        success = true,
                        message = "Реєстрація пройшла успішно",
                        token = token, // ДОДАНО: JWT токен
                        user = new
                        {
                            id = user.Id,
                            userId = user.Id,
                            email = user.Email,
                            firstName = user.FirstName,
                            lastName = user.LastName,
                            roles = roles
                        }
                    });
                }

                _logger.LogWarning($"Помилка при реєстрації користувача {model.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                return BadRequest(new
                {
                    success = false,
                    errors = result.Errors.Select(e => e.Description)
                });
            }

            _logger.LogWarning($"Неправильна модель даних для реєстрації: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
            return BadRequest(new
            {
                success = false,
                errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            _logger.LogInformation($"Отримано запит на вхід для користувача: {model.Email}");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning($"Неправильна модель даних для входу: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
                return BadRequest(new
                {
                    success = false,
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });
            }

            // Явно шукаємо користувача за Email
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                _logger.LogWarning($"Користувач з email {model.Email} не знайдений в базі даних");
                return BadRequest(new { success = false, message = "Невірний логін або пароль" });
            }

            _logger.LogInformation($"Знайдено користувача: ID={user.Id}, UserName={user.UserName}");

            // Перевірка паролю
            var isPasswordValid = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!isPasswordValid)
            {
                _logger.LogWarning($"Неправильний пароль для користувача {model.Email}");
                return BadRequest(new { success = false, message = "Невірний логін або пароль" });
            }

            _logger.LogInformation($"Пароль підтверджено для {model.Email}, виконується вхід");

            // ОНОВЛЕНО: Виконуємо Cookie вхід для MVC частини
            var signInResult = await _signInManager.PasswordSignInAsync(
                user.UserName,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);

            if (signInResult.Succeeded)
            {
                var roles = await _userManager.GetRolesAsync(user);
                _logger.LogInformation($"Користувач {model.Email} успішно увійшов. Ролі: {string.Join(", ", roles)}");

                // ДОДАНО: Генеруємо JWT токен для API запитів
                var token = await GenerateJwtToken(user);

                return Ok(new
                {
                    success = true,
                    message = "Вхід виконано успішно",
                    token = token, // ДОДАНО: JWT токен
                    user = new
                    {
                        id = user.Id,
                        userId = user.Id,
                        email = user.Email,
                        firstName = user.FirstName,
                        lastName = user.LastName,
                        roles = roles
                    }
                });
            }

            // Детальне логування причини невдачі
            _logger.LogWarning($"Невдала спроба входу для {model.Email}. " +
                $"IsLockedOut: {signInResult.IsLockedOut}, " +
                $"IsNotAllowed: {signInResult.IsNotAllowed}, " +
                $"RequiresTwoFactor: {signInResult.RequiresTwoFactor}");

            string errorMessage = "Невірний логін або пароль";
            if (signInResult.IsLockedOut)
                errorMessage = "Обліковий запис тимчасово заблоковано. Спробуйте пізніше.";
            else if (signInResult.IsNotAllowed)
                errorMessage = "Вхід заборонено. Можливо, потрібно підтвердити email.";
            else if (signInResult.RequiresTwoFactor)
                errorMessage = "Потрібна двофакторна автентифікація.";

            return BadRequest(new { success = false, message = errorMessage });
        }

        // ДОДАНО: Метод для генерації JWT токенів
        private async Task<string> GenerateJwtToken(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim("FirstName", user.FirstName ?? ""),
                new Claim("LastName", user.LastName ?? "")
            };

            // Додаємо ролі як claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var secretKey = _configuration["JwtSettings:SecretKey"] ?? "your-super-secret-key-that-is-long-enough-for-jwt-signing-and-should-be-at-least-32-characters";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7), // Токен дійсний 7 днів
                SigningCredentials = credentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        [HttpGet("test-auth")]
        public IActionResult TestAuth()
        {
            bool isAuthenticated = User?.Identity?.IsAuthenticated ?? false;
            string userName = User?.Identity?.Name ?? "Не автентифіковано";
            var claims = User?.Claims?.Select(c => new { type = c.Type, value = c.Value }).ToList();

            return Ok(new
            {
                isAuthenticated = isAuthenticated,
                userName = userName,
                claims = claims
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            string email = User.Identity?.IsAuthenticated == true ? User.Identity.Name : "Анонімний користувач";
            _logger.LogInformation($"Отримано запит на вихід із системи для користувача {email}");

            await _signInManager.SignOutAsync();
            _logger.LogInformation($"Користувач {email} вийшов із системи");

            return Ok(new { success = true, message = "Вихід виконано успішно" });
        }

        [HttpGet("status")]
        public async Task<IActionResult> CheckAuthStatus()
        {
            _logger.LogInformation("Отримано запит на перевірку статусу автентифікації");

            if (User.Identity?.IsAuthenticated == true)
            {
                _logger.LogInformation($"Автентифікований користувач: {User.Identity.Name}");

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning($"Користувач автентифікований ({User.Identity.Name}), але не знайдений в базі даних");
                    return Ok(new { isAuthenticated = false });
                }

                var roles = await _userManager.GetRolesAsync(user);
                _logger.LogInformation($"Статус автентифікації для {user.Email}: автентифікований. Ролі: {string.Join(", ", roles)}");

                return Ok(new
                {
                    isAuthenticated = true,
                    user = new
                    {
                        id = user.Id,
                        userId = user.Id,
                        email = user.Email,
                        firstName = user.FirstName,
                        lastName = user.LastName,
                        roles = roles
                    }
                });
            }

            _logger.LogInformation("Перевірка статусу: користувач не автентифікований");
            return Ok(new { isAuthenticated = false });
        }

        // ДОДАНО: Endpoint для перевірки JWT токену
        [HttpGet("me")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                {
                    return BadRequest(new { success = false, message = "Не вдалося отримати дані користувача" });
                }

                var user = await _userManager.FindByEmailAsync(userEmail);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "Користувача не знайдено" });
                }

                var roles = await _userManager.GetRolesAsync(user);

                return Ok(new
                {
                    success = true,
                    user = new
                    {
                        id = user.Id,
                        userId = user.Id,
                        email = user.Email,
                        firstName = user.FirstName,
                        lastName = user.LastName,
                        roles = roles
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user from JWT token");
                return StatusCode(500, new { success = false, message = "Внутрішня помилка сервера" });
            }
        }

        [HttpPost("RefreshSession")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> RefreshSession()
        {
            try
            {
                _logger.LogInformation("RefreshSession called");
                _logger.LogInformation("User.Identity.IsAuthenticated: {IsAuthenticated}", User.Identity.IsAuthenticated);
                _logger.LogInformation("User.Identity.Name: {UserName}", User.Identity.Name);

                if (!User.Identity.IsAuthenticated)
                {
                    _logger.LogWarning("RefreshSession: User not authenticated");
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Користувач не авторизований"
                    });
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("RefreshSession: User not found in database");
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Користувач не знайдений"
                    });
                }

                // Оновлюємо cookie авторизації
                await _signInManager.RefreshSignInAsync(user);

                _logger.LogInformation("Session refreshed for user: {Email}", user.Email);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Сесія оновлена успішно"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing session");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Помилка оновлення сесії"
                });
            }
        }

        [HttpPost("CheckAndRefreshSession")]
        public async Task<ActionResult<ApiResponse<object>>> CheckAndRefreshSession([FromBody] RefreshSessionRequest request)
        {
            try
            {
                _logger.LogInformation("CheckAndRefreshSession called with email: {Email}", request.Email);

                if (string.IsNullOrEmpty(request.Email))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Email обов'язковий"
                    });
                }

                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    _logger.LogWarning("User not found: {Email}", request.Email);
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Користувач не знайдений"
                    });
                }

                // Підписуємо користувача знову з довгим терміном
                await _signInManager.SignInAsync(user, isPersistent: true);

                _logger.LogInformation("Session refreshed for user: {Email}", user.Email);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Сесія оновлена успішно"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CheckAndRefreshSession for email: {Email}", request?.Email);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Помилка оновлення сесії"
                });
            }
        }

        public class RefreshSessionRequest
        {
            public string Email { get; set; }
        }
    }
}