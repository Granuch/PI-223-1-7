using BLL.Interfaces;
using BLL.Services;
using Mapping.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PL.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Roles = "Administrator")] // Тільки адміністратори можуть управляти користувачами
    public class AdminUsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<AdminUsersController> _logger;

        public AdminUsersController(IUserService userService, ILogger<AdminUsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Отримати всіх користувачів
        /// </summary>
        [HttpGet("GetAllUsers")]
        public async Task<ActionResult<ApiResponse<IEnumerable<UserDTO>>>> GetAllUsers()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                return Ok(new ApiResponse<IEnumerable<UserDTO>>
                {
                    Success = true,
                    Data = users,
                    Message = "Користувачі отримані успішно"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                return StatusCode(500, new ApiResponse<IEnumerable<UserDTO>>
                {
                    Success = false,
                    Message = "Помилка отримання користувачів"
                });
            }
        }

        /// <summary>
        /// Отримати користувача за ID
        /// </summary>
        [HttpGet("GetUserById")]
        public async Task<ActionResult<ApiResponse<UserDTO>>> GetUserById(string id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new ApiResponse<UserDTO>
                    {
                        Success = false,
                        Message = "Користувача не знайдено"
                    });
                }

                return Ok(new ApiResponse<UserDTO>
                {
                    Success = true,
                    Data = user,
                    Message = "Користувач знайдений"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user {UserId}", id);
                return StatusCode(500, new ApiResponse<UserDTO>
                {
                    Success = false,
                    Message = "Помилка отримання користувача"
                });
            }
        }

        /// <summary>
        /// Створити нового користувача
        /// </summary>
        // Оновіть методи в AdminUsersController (API проект)

        [HttpPost("CreateUser")]
        public async Task<ActionResult<ApiResponse<object>>> CreateUser([FromBody] CreateUserRequest request)
        {
            try
            {
                // ДОДАНО: встановлюємо роль за замовчуванням якщо не вказана
                if (string.IsNullOrEmpty(request.Role))
                {
                    request.Role = "RegisteredUser";
                }

                var result = await _userService.CreateUserAsync(request);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User {Email} created successfully with role {Role}", request.Email, request.Role);
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Користувач створений успішно"
                    });
                }

                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Помилка створення користувача",
                    Errors = result.Errors.Select(e => e.Description)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user {Email}", request.Email);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Внутрішня помилка сервера"
                });
            }
        }

        [HttpPost("CreateAdmin")]
        public async Task<ActionResult<ApiResponse<object>>> CreateAdmin([FromBody] CreateUserRequest request)
        {
            try
            {
                // ДОДАНО: примусово встановлюємо роль адміністратора
                request.Role = "Administrator";

                var result = await _userService.CreateAdminAsync(request);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Admin {Email} created successfully", request.Email);
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Адміністратор створений успішно"
                    });
                }

                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Помилка створення адміністратора",
                    Errors = result.Errors.Select(e => e.Description)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating admin {Email}", request.Email);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Внутрішня помилка сервера"
                });
            }
        }

        [HttpPost("CreateManager")]
        public async Task<ActionResult<ApiResponse<object>>> CreateManager([FromBody] CreateUserRequest request)
        {
            try
            {
                // ДОДАНО: примусово встановлюємо роль менеджера
                request.Role = "Manager";

                var result = await _userService.CreateManagerAsync(request);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Manager {Email} created successfully", request.Email);
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Менеджер створений успішно"
                    });
                }

                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Помилка створення менеджера",
                    Errors = result.Errors.Select(e => e.Description)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating manager {Email}", request.Email);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Внутрішня помилка сервера"
                });
            }
        }

        /// <summary>
        /// Оновити користувача
        /// </summary>
        [HttpPut("UpdateUser")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateUser(string id, [FromBody] UpdateUserRequest request)
        {
            try
            {
                var result = await _userService.UpdateUserAsync(id, request);

                if (result.Succeeded)
                {
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Користувач оновлений успішно"
                    });
                }

                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Помилка оновлення користувача",
                    Errors = result.Errors.Select(e => e.Description)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Внутрішня помилка сервера"
                });
            }
        }

        /// <summary>
        /// Видалити користувача
        /// </summary>
        [HttpDelete("DeleteUser")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteUser(string id)
        {
            try
            {
                var result = await _userService.DeleteUserAsync(id);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User {UserId} deleted successfully", id);
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Користувач видалений успішно"
                    });
                }

                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Помилка видалення користувача",
                    Errors = result.Errors.Select(e => e.Description)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Внутрішня помилка сервера"
                });
            }
        }

        /// <summary>
        /// Змінити пароль користувача
        /// </summary>
        [HttpPost("ChangePassword")]
        public async Task<ActionResult<ApiResponse<object>>> ChangePassword(string id, [FromBody] ChangePasswordRequest request)
        {
            try
            {
                var result = await _userService.ChangeUserPasswordAsync(id, request.NewPassword);

                if (result.Succeeded)
                {
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Пароль змінений успішно"
                    });
                }

                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Помилка зміни паролю",
                    Errors = result.Errors.Select(e => e.Description)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Внутрішня помилка сервера"
                });
            }
        }


        /// <summary>
        /// Призначити роль користувачу
        /// </summary>
        [HttpPost("AssignRole")]
        public async Task<ActionResult<ApiResponse<object>>> AssignRole(string id, [FromBody] AssignRoleRequest request)
        {
            try
            {
                var result = await _userService.AssignRoleToUserAsync(id, request.RoleName);

                if (result.Succeeded)
                {
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Роль призначена успішно"
                    });
                }

                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Помилка призначення ролі",
                    Errors = result.Errors.Select(e => e.Description)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role to user {UserId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Внутрішня помилка сервера"
                });
            }
        }

        /// <summary>
        /// Видалити роль у користувача
        /// </summary>
        [HttpPost("RemoveRole")]
        public async Task<ActionResult<ApiResponse<object>>> RemoveRole(string id, [FromBody] AssignRoleRequest request)
        {
            try
            {
                var result = await _userService.RemoveRoleFromUserAsync(id, request.RoleName);

                if (result.Succeeded)
                {
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Роль видалена успішно"
                    });
                }

                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Помилка видалення ролі",
                    Errors = result.Errors.Select(e => e.Description)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing role from user {UserId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Внутрішня помилка сервера"
                });
            }
        }

        /// <summary>
        /// Отримати ролі користувача
        /// </summary>
        [HttpGet("GetUserRoles")]
        public async Task<ActionResult<ApiResponse<IEnumerable<string>>>> GetUserRoles(string id)
        {
            try
            {
                var roles = await _userService.GetUserRolesAsync(id);
                return Ok(new ApiResponse<IEnumerable<string>>
                {
                    Success = true,
                    Data = roles,
                    Message = "Ролі користувача отримані"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roles for user {UserId}", id);
                return StatusCode(500, new ApiResponse<IEnumerable<string>>
                {
                    Success = false,
                    Message = "Помилка отримання ролей"
                });
            }
        }

        /// <summary>
        /// Отримати всі ролі
        /// </summary>
        [HttpGet("GetAllRoles")]
        public async Task<ActionResult<ApiResponse<IEnumerable<RoleDTO>>>> GetAllRoles()
        {
            try
            {
                var roles = await _userService.GetAllRolesAsync();
                return Ok(new ApiResponse<IEnumerable<RoleDTO>>
                {
                    Success = true,
                    Data = roles,
                    Message = "Ролі отримані успішно"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all roles");
                return StatusCode(500, new ApiResponse<IEnumerable<RoleDTO>>
                {
                    Success = false,
                    Message = "Помилка отримання ролей"
                });
            }
        }
    }
}
