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

        [HttpGet("GetAllUsers")]
        [Authorize] // Базова авторизація
        public async Task<ActionResult<ApiResponse<IEnumerable<UserDTO>>>> GetAllUsers()
        {
            try
            {
                _logger.LogInformation("=== GetAllUsers API Called ===");

                // ДІАГНОСТИКА
                _userContext.LogCurrentUserInfo();

                if (!_userContext.IsAuthenticated())
                {
                    _logger.LogWarning("User not authenticated");
                    return Unauthorized(new ApiResponse<IEnumerable<UserDTO>>
                    {
                        Success = false,
                        Message = "Користувач не авторизований"
                    });
                }

                if (!_userContext.IsAdministrator())
                {
                    _logger.LogWarning("Access denied - user is not administrator. User: {User}, Roles: {Roles}",
                        _userContext.GetCurrentUserEmail(), string.Join(", ", _userContext.GetCurrentUserRoles()));
                    return Forbid();
                }

                _logger.LogInformation("Access granted for administrator: {User}", _userContext.GetCurrentUserEmail());

                var users = await _userService.GetAllUsersAsync();

                _logger.LogInformation("Successfully retrieved {Count} users", users?.Count() ?? 0);

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
                _logger.LogInformation("GetUserById called for: {UserId}", id);

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

        [HttpPost("AssignRole")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> AssignRole(string id, [FromBody] AssignRoleRequest request)
        {
            try
            {
                _logger.LogInformation("AssignRole called for user {UserId} with role {RoleName}", id, request.RoleName);

                // КРИТИЧНО: Логируем детальную информацию
                _userContext.LogCurrentUserInfo();

                if (!_userContext.IsAuthenticated())
                {
                    _logger.LogWarning("User not authenticated for role assignment");
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Требуется аутентификация"
                    });
                }

                if (!_userContext.IsAdministrator())
                {
                    _logger.LogWarning("User {User} is not administrator. Roles: {Roles}",
                        _userContext.GetCurrentUserEmail(),
                        string.Join(", ", _userContext.GetCurrentUserRoles()));

                    return StatusCode(403, new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Недостаточно прав. Требуется роль Administrator"
                    });
                }

                // Добавьте валидацию роли
                var validRoles = new[] { "Administrator", "Manager", "RegisteredUser" };
                if (!validRoles.Contains(request.RoleName))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Недопустимая роль: {request.RoleName}"
                    });
                }

                var result = await _userService.AssignRoleToUserAsync(id, request.RoleName);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Role {RoleName} assigned to user {UserId} by {Admin}",
                        request.RoleName, id, _userContext.GetCurrentUserEmail());

                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Роль успешно назначена"
                    });
                }

                var errorMessage = string.Join("; ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to assign role {RoleName} to user {UserId}: {Errors}",
                    request.RoleName, id, errorMessage);

                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ошибка назначения роли: " + errorMessage
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in AssignRole for user {UserId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Внутренняя ошибка сервера"
                });
            }
        }

        // Решта методів аналогічно з перевірками _userContext.IsAdministrator()...

        [HttpGet("GetAllRoles")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<IEnumerable<RoleDTO>>>> GetAllRoles()
        {
            try
            {
                if (!_userContext.IsAdministrator())
                {
                    return Forbid();
                }

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

        // ДІАГНОСТИЧНИЙ ENDPOINT
        [HttpGet("TestAuth")]
        public IActionResult TestAuth()
        {
            _logger.LogInformation("=== TestAuth Called ===");
            _userContext.LogCurrentUserInfo();

            return Ok(new
            {
                IsAuthenticated = _userContext.IsAuthenticated(),
                IsAdministrator = _userContext.IsAdministrator(),
                Email = _userContext.GetCurrentUserEmail(),
                UserId = _userContext.GetCurrentUserId(),
                Roles = _userContext.GetCurrentUserRoles(),
                Timestamp = DateTime.UtcNow
            });
        }
    }
}