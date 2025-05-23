using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PI_223_1_7.Models;
using PL.ViewModels;
using System.Security.Claims;

namespace PL.Controllers
{
    [Route("api/account")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
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
                    EmailConfirmed = true // Підтверджуємо email автоматично для тестування
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation($"Користувач {user.Email} успішно зареєстрований");

                    // Додаємо роль
                    await _userManager.AddToRoleAsync(user, "RegisteredUser");

                    // Виконуємо вхід
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    _logger.LogInformation($"Автоматичний вхід для {user.Email} після реєстрації");

                    return Ok(new
                    {
                        success = true,
                        message = "Реєстрація пройшла успішно",
                        user = new
                        {
                            email = user.Email,
                            firstName = user.FirstName,
                            lastName = user.LastName,
                            roles = new[] { "RegisteredUser" }
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

        // Авторизація користувача з детальною діагностикою
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

            // Перевірка паролю без входу
            var isPasswordValid = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!isPasswordValid)
            {
                _logger.LogWarning($"Неправильний пароль для користувача {model.Email}");
                return BadRequest(new { success = false, message = "Невірний логін або пароль" });
            }

            _logger.LogInformation($"Пароль підтверджено для {model.Email}, виконується вхід");

            // Якщо дані правильні, виконуємо вхід
            var result = await _signInManager.PasswordSignInAsync(
                user.UserName, // ВАЖЛИВО: використовуємо UserName (не Email)
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                var roles = await _userManager.GetRolesAsync(user);
                _logger.LogInformation($"Користувач {model.Email} успішно увійшов. Ролі: {string.Join(", ", roles)}");

                // Перевіряємо, чи дійсно користувач автентифікований після входу
                var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
                _logger.LogInformation($"Перевірка автентифікації після входу: {isAuthenticated}");

                return Ok(new
                {
                    success = true,
                    message = "Вхід виконано успішно",
                    user = new
                    {
                        userId = user.Id,          // ДОДАНО: дублюємо для сумісності
                        email = user.Email,
                        firstName = user.FirstName,
                        lastName = user.LastName,
                        roles = roles
                    }
                });
            }

            // Детальне логування причини невдачі
            _logger.LogWarning($"Невдала спроба входу для {model.Email}. " +
                $"IsLockedOut: {result.IsLockedOut}, " +
                $"IsNotAllowed: {result.IsNotAllowed}, " +
                $"RequiresTwoFactor: {result.RequiresTwoFactor}");

            // Конкретніше повідомлення про помилку
            string errorMessage = "Невірний логін або пароль";
            if (result.IsLockedOut)
                errorMessage = "Обліковий запис тимчасово заблоковано. Спробуйте пізніше.";
            else if (result.IsNotAllowed)
                errorMessage = "Вхід заборонено. Можливо, потрібно підтвердити email.";
            else if (result.RequiresTwoFactor)
                errorMessage = "Потрібна двофакторна автентифікація.";

            return BadRequest(new { success = false, message = errorMessage });
        }

        // Перевірка роботи входу через тестовий метод
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

        // Вихід з системи
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            string email = User.Identity?.IsAuthenticated == true ? User.Identity.Name : "Анонімний користувач";
            _logger.LogInformation($"Отримано запит на вихід із системи для користувача {email}");

            await _signInManager.SignOutAsync();
            _logger.LogInformation($"Користувач {email} вийшов із системи");

            return Ok(new { success = true, message = "Вихід виконано успішно" });
        }

        // Перевірка статусу автентифікації
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
    }
}