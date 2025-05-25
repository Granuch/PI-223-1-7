// Оновлений AdminUsersController.cs

using BLL.Interfaces;
using Mapping.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PL.Services;

namespace PL.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AdminUsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IUserContextService _userContext;
        private readonly ILogger<AdminUsersController> _logger;

        public AdminUsersController(
            IUserService userService,
            IUserContextService userContext,
            ILogger<AdminUsersController> logger)
        {
            _userService = userService;
            _userContext = userContext;
            _logger = logger;
        }

        /// <summary>
        /// Отримати всіх користувачів
        /// </summary>
        [HttpGet("GetAllUsers")]
        [Authorize] // Базова авторизація
        public async Task<ActionResult<ApiResponse<IEnumerable<UserDTO>>>> GetAllUsers()
        {
            try
            {
                _logger.LogInformation("GetAllUsers called");
                _logger.LogInformation("User authenticated: {IsAuth}", _userContext.IsAuthenticated());
                _logger.LogInformation("User email: {Email}", _userContext.GetCurrentUserEmail());
                _logger.LogInformation("User roles: {Roles}", string.Join(", ", _userContext.GetCurrentUserRoles()));
                _logger.LogInformation("Is Administrator: {IsAdmin}", _userContext.IsAdministrator());

                // Перевірка ролі
                if (!_userContext.IsAdministrator())
                {
                    _logger.LogWarning("Access denied - user is not administrator");
                    return Forbid();
                }

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

        [HttpGet("GetUserById")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<UserDTO>>> GetUserById(string id)
        {
            try
            {
                if (!_userContext.IsAdministrator())
                {
                    return Forbid();
                }

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

        [HttpPost("CreateUser")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> CreateUser([FromBody] CreateUserRequest request)
        {
            try
            {
                if (!_userContext.IsAdministrator())
                {
                    _logger.LogWarning("Non-administrator tried to create user");
                    return Forbid();
                }

                if (string.IsNullOrEmpty(request.Role))
                {
                    request.Role = "RegisteredUser";
                }

                var result = await _userService.CreateUserAsync(request);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User {Email} created successfully by {Admin}",
                        request.Email, _userContext.GetCurrentUserEmail());
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

        [HttpPost("AssignRole")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> AssignRole(string id, [FromBody] AssignRoleRequest request)
        {
            try
            {
                _logger.LogInformation("AssignRole called for user {UserId} with role {RoleName}", id, request.RoleName);
                _logger.LogInformation("Current user: {Email}, IsAdmin: {IsAdmin}",
                    _userContext.GetCurrentUserEmail(), _userContext.IsAdministrator());

                if (!_userContext.IsAdministrator())
                {
                    _logger.LogWarning("Non-administrator tried to assign role");
                    return Forbid();
                }

                var result = await _userService.AssignRoleToUserAsync(id, request.RoleName);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Role {RoleName} assigned to user {UserId} by {Admin}",
                        request.RoleName, id, _userContext.GetCurrentUserEmail());
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


        [HttpPost("RemoveRole")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> RemoveRole(string id, [FromBody] AssignRoleRequest request)
        {
            try
            {
                if (!_userContext.IsAdministrator())
                {
                    return Forbid();
                }

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
    }
}