using Microsoft.AspNetCore.Mvc;
using UI.Models.DTOs;
using UI.Models.ViewModels;
using UI.Services;

namespace UI.Controllers
{
    public class AdminController : BaseController
    {
        private readonly ILogger<AdminController> _logger;

        public AdminController(IApiService apiService, ILogger<AdminController> logger)
            : base(apiService)
        {
            _logger = logger;
        }

        // Головна сторінка адмін панелі
        public IActionResult Index()
        {
            // Перевіряємо, чи користувач адміністратор
            if (ViewBag.IsAdministrator != true)
            {
                TempData["ErrorMessage"] = "Доступ заборонено. Тільки адміністратори можуть переглядати цю сторінку.";
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // Список всіх користувачів
        public async Task<IActionResult> Users()
        {
            if (ViewBag.IsAdministrator != true)
            {
                TempData["ErrorMessage"] = "Доступ заборонено.";
                return RedirectToAction("Index", "Home");
            }

            var result = await _apiService.GetAllUsersAsync();

            if (result.Success)
            {
                return View(result.Data);
            }

            TempData["ErrorMessage"] = result.Message;
            return View(new List<UserDTO>());
        }

        // Деталі користувача
        [HttpGet]
        public async Task<IActionResult> UserDetails(string id)
        {
            if (ViewBag.IsAdministrator != true)
            {
                TempData["ErrorMessage"] = "Доступ заборонено.";
                return RedirectToAction("Index", "Home");
            }

            var result = await _apiService.GetUserByIdAsync(id);

            if (result.Success)
            {
                return View(result.Data);
            }

            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction("Users");
        }

        // Створення користувача - GET
        [HttpGet]
        public IActionResult CreateUser()
        {
            if (ViewBag.IsAdministrator != true)
            {
                TempData["ErrorMessage"] = "Доступ заборонено.";
                return RedirectToAction("Index", "Home");
            }

            return View(new CreateUserViewModel());
        }

        // Створення користувача - POST
        // Оновіть метод CreateUser в AdminController
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            if (ViewBag.IsAdministrator != true)
            {
                TempData["ErrorMessage"] = "Доступ заборонено.";
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var request = new CreateUserRequest
            {
                Email = model.Email,
                Password = model.Password,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber,
                Role = model.UserType // ВИПРАВЛЕНО: передаємо роль з моделі
            };

            ApiResponse<object> result = model.UserType switch
            {
                "RegisteredUser" => await _apiService.CreateUserAsync(request),
                "Manager" => await _apiService.CreateManagerAsync(request),
                "Administrator" => await _apiService.CreateAdminAsync(request),
                _ => new ApiResponse<object> { Success = false, Message = "Невірний тип користувача" }
            };

            if (result.Success)
            {
                TempData["SuccessMessage"] = $"Користувач створений успішно з роллю {model.UserType}!";
                return RedirectToAction("Users");
            }

            // ВИПРАВЛЕНО: краща обробка помилок
            if (result.ValidationErrors?.Any() == true)
            {
                foreach (var error in result.ValidationErrors)
                {
                    foreach (var message in error.Value)
                    {
                        ModelState.AddModelError(error.Key, message);
                    }
                }
            }
            else if (result.Errors?.Any() == true)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error);
                }
            }
            else
            {
                ModelState.AddModelError("", result.Message);
            }

            return View(model);
        }

        // Редагування користувача - GET
        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            if (ViewBag.IsAdministrator != true)
            {
                TempData["ErrorMessage"] = "Доступ заборонено.";
                return RedirectToAction("Index", "Home");
            }

            var result = await _apiService.GetUserByIdAsync(id);

            if (result.Success)
            {
                var model = new EditUserViewModel
                {
                    Id = result.Data.Id,
                    Email = result.Data.Email,
                    FirstName = result.Data.FirstName,
                    LastName = result.Data.LastName,
                    PhoneNumber = result.Data.PhoneNumber,
                    Roles = result.Data.Roles?.ToList() ?? new List<string>()
                };

                return View(model);
            }

            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction("Users");
        }

        // Редагування користувача - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (ViewBag.IsAdministrator != true)
            {
                TempData["ErrorMessage"] = "Доступ заборонено.";
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var request = new UpdateUserRequest
            {
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber
            };

            var result = await _apiService.UpdateUserAsync(model.Id, request);

            if (result.Success)
            {
                TempData["SuccessMessage"] = "Користувач оновлений успішно!";
                return RedirectToAction("Users");
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
                ModelState.AddModelError("", result.Message);
            }

            return View(model);
        }

        // Видалення користувача - GET
        [HttpGet]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (ViewBag.IsAdministrator != true)
            {
                TempData["ErrorMessage"] = "Доступ заборонено.";
                return RedirectToAction("Index", "Home");
            }

            var result = await _apiService.GetUserByIdAsync(id);

            if (result.Success)
            {
                return View(result.Data);
            }

            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction("Users");
        }

        // Видалення користувача - POST
        [HttpPost, ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUserConfirmed(string id)
        {
            if (ViewBag.IsAdministrator != true)
            {
                TempData["ErrorMessage"] = "Доступ заборонено.";
                return RedirectToAction("Index", "Home");
            }

            var result = await _apiService.DeleteUserAsync(id);

            if (result.Success)
            {
                TempData["SuccessMessage"] = "Користувач видалений успішно!";
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction("Users");
        }

        // Зміна паролю - GET
        [HttpGet]
        public async Task<IActionResult> ChangePassword(string id)
        {
            if (ViewBag.IsAdministrator != true)
            {
                TempData["ErrorMessage"] = "Доступ заборонено.";
                return RedirectToAction("Index", "Home");
            }

            var userResult = await _apiService.GetUserByIdAsync(id);
            if (!userResult.Success)
            {
                TempData["ErrorMessage"] = userResult.Message;
                return RedirectToAction("Users");
            }

            var model = new ChangePasswordViewModel
            {
                UserId = id,
                UserEmail = userResult.Data.Email
            };

            return View(model);
        }

        // Зміна паролю - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            _logger.LogInformation("ChangePassword POST called for user {UserId}", model?.UserId);

            if (ViewBag.IsAdministrator != true)
            {
                _logger.LogWarning("Non-administrator tried to change password");
                TempData["ErrorMessage"] = "Доступ заборонено.";
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid for password change");
                foreach (var error in ModelState)
                {
                    _logger.LogWarning("ModelState error - Field: {Field}, Errors: {Errors}",
                        error.Key, string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage)));
                }

                // Перезавантажуємо дані користувача для відображення
                var userResult = await _apiService.GetUserByIdAsync(model.UserId);
                if (userResult.Success)
                {
                    model.UserEmail = userResult.Data.Email;
                }

                return View(model);
            }

            try
            {
                var request = new ChangePasswordRequest
                {
                    UserId = model.UserId,        // ДОДАНО: передаємо UserId
                    NewPassword = model.NewPassword
                };

                _logger.LogInformation("Calling API to change password for user {UserId}", model.UserId);
                var result = await _apiService.ChangeUserPasswordAsync(model.UserId, request);

                _logger.LogInformation("API response: Success={Success}, Message={Message}",
                    result.Success, result.Message);

                if (result.Success)
                {
                    _logger.LogInformation("Password changed successfully for user {UserId}", model.UserId);
                    TempData["SuccessMessage"] = "Пароль змінений успішно!";
                    return RedirectToAction("Users");
                }

                _logger.LogError("Password change failed for user {UserId}: {Message}",
                    model.UserId, result.Message);

                if (result.Errors?.Any() == true)
                {
                    foreach (var error in result.Errors)
                    {
                        _logger.LogError("API Error: {Error}", error);
                        ModelState.AddModelError("", error);
                    }
                }
                else
                {
                    ModelState.AddModelError("", result.Message ?? "Невідома помилка");
                }

                // Перезавантажуємо дані користувача для відображення
                var userReloadResult = await _apiService.GetUserByIdAsync(model.UserId);
                if (userReloadResult.Success)
                {
                    model.UserEmail = userReloadResult.Data.Email;
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in ChangePassword for user {UserId}", model.UserId);
                ModelState.AddModelError("", "Сталася неочікувана помилка");

                // Перезавантажуємо дані користувача для відображення
                try
                {
                    var userReloadResult = await _apiService.GetUserByIdAsync(model.UserId);
                    if (userReloadResult.Success)
                    {
                        model.UserEmail = userReloadResult.Data.Email;
                    }
                }
                catch
                {
                    // Ігноруємо помилки при перезавантаженні даних
                }

                return View(model);
            }
        }

        // Управління ролями - GET
        [HttpGet]
        public async Task<IActionResult> ManageRoles(string id)
        {
            if (ViewBag.IsAdministrator != true)
            {
                TempData["ErrorMessage"] = "Доступ заборонено.";
                return RedirectToAction("Index", "Home");
            }

            var userResult = await _apiService.GetUserByIdAsync(id);
            var rolesResult = await _apiService.GetAllRolesAsync();
            var userRolesResult = await _apiService.GetUserRolesAsync(id);

            if (userResult.Success && rolesResult.Success && userRolesResult.Success)
            {
                var model = new ManageRolesViewModel
                {
                    UserId = id,
                    UserEmail = userResult.Data.Email,
                    AllRoles = rolesResult.Data.ToList(),
                    UserRoles = userRolesResult.Data.ToList()
                };

                return View(model);
            }

            TempData["ErrorMessage"] = "Помилка завантаження даних";
            return RedirectToAction("Users");
        }

        // Призначення ролі
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(string userId, string roleName)
        {
            // КРИТИЧНО ВАЖЛИВЕ ЛОГУВАННЯ
            Console.WriteLine("=================================================");
            Console.WriteLine("ASSIGNROLE МЕТОД ВИКЛИКАНИЙ!");
            Console.WriteLine($"userId: '{userId}'");
            Console.WriteLine($"roleName: '{roleName}'");
            Console.WriteLine($"ViewBag.IsAdministrator: {ViewBag.IsAdministrator}");
            Console.WriteLine("=================================================");

            if (ViewBag.IsAdministrator != true)
            {
                Console.WriteLine("ДОСТУП ЗАБОРОНЕНО!");
                TempData["ErrorMessage"] = "Доступ заборонено.";
                return RedirectToAction("Index", "Home");
            }

            if (!RoleConstants.IsValidRole(roleName))
            {
                Console.WriteLine($"НЕПРИПУСТИМА РОЛЬ: {roleName}");
                TempData["ErrorMessage"] = $"Недопустима роль: {roleName}";
                return RedirectToAction("ManageRoles", new { id = userId });
            }

            Console.WriteLine("СТВОРЮЄМО REQUEST...");
            var request = new AssignRoleRequest { RoleName = roleName };

            Console.WriteLine("ВИКЛИКАЄМО API СЕРВІС...");
            var result = await _apiService.AssignRoleToUserAsync(userId, request);

            Console.WriteLine($"РЕЗУЛЬТАТ API: Success={result.Success}, Message='{result.Message}'");

            if (result.Success)
            {
                TempData["SuccessMessage"] = $"Роль {roleName} призначена успішно!";
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            Console.WriteLine("ПЕРЕНАПРАВЛЯЄМО НА MANAGEROLES...");
            return RedirectToAction("ManageRoles", new { id = userId });
        }

        // Видалення ролі
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveRole(string userId, string roleName)
        {
            if (ViewBag.IsAdministrator != true)
            {
                TempData["ErrorMessage"] = "Доступ заборонено.";
                return RedirectToAction("Index", "Home");
            }

            var request = new AssignRoleRequest { RoleName = roleName };
            var result = await _apiService.RemoveRoleFromUserAsync(userId, request);

            if (result.Success)
            {
                TempData["SuccessMessage"] = $"Роль {roleName} видалена успішно!";
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction("ManageRoles", new { id = userId });
        }
    }
}