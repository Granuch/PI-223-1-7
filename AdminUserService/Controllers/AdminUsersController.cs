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
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<ApiResponse<IEnumerable<UserDTO>>>> GetAllUsers()
        {
            try
            {
                _logger.LogInformation("=== GetAllUsers API Called ===");
                _userContext.LogCurrentUserInfo();

                if (!_userContext.IsAdministrator())
                {
                    _logger.LogWarning("User not authorized - not an administrator");
                    return Forbid();
                }

                var users = await _userService.GetAllUsersAsync();

                _logger.LogInformation("Successfully retrieved {Count} users", users?.Count() ?? 0);

                return Ok(new ApiResponse<IEnumerable<UserDTO>>
                {
                    Success = true,
                    Data = users,
                    Message = "Users retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                return StatusCode(500, new ApiResponse<IEnumerable<UserDTO>>
                {
                    Success = false,
                    Message = "Error retrieving users"
                });
            }
        }

        [HttpGet("GetUserById")]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<ActionResult<ApiResponse<UserDTO>>> GetUserById(string id)
        {
            try
            {
                _logger.LogInformation("GetUserById called for: {UserId}", id);

                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new ApiResponse<UserDTO>
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                return Ok(new ApiResponse<UserDTO>
                {
                    Success = true,
                    Data = user,
                    Message = "User found"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user {UserId}", id);
                return StatusCode(500, new ApiResponse<UserDTO>
                {
                    Success = false,
                    Message = "Error retrieving user"
                });
            }
        }

        [HttpPost("CreateUser")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<ApiResponse<object>>> CreateUser([FromBody] CreateUserRequest request)
        {
            try
            {
                _logger.LogInformation("CreateUser called");

                if (!ModelState.IsValid)
                {
                    return BadRequest();
                }

                var result = await _userService.CreateUserAsync(request);

                if (result.Succeeded)
                {
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "User created successfully"
                    });
                }

                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error creating user",
                    Errors = result.Errors.Select(e => e.Description)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error creating user"
                });
            }
        }

        [HttpPost("CreateAdmin")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<ApiResponse<object>>> CreateAdmin([FromBody] CreateUserRequest request)
        {
            try
            {
                request.Role = "Administrator";
                var result = await _userService.CreateUserAsync(request);

                if (result.Succeeded)
                {
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Administrator created successfully"
                    });
                }

                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error creating administrator",
                    Errors = result.Errors.Select(e => e.Description)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating admin");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error creating administrator"
                });
            }
        }

        [HttpPost("CreateManager")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<ApiResponse<object>>> CreateManager([FromBody] CreateUserRequest request)
        {
            try
            {
                request.Role = "Manager";
                var result = await _userService.CreateUserAsync(request);

                if (result.Succeeded)
                {
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Manager created successfully"
                    });
                }

                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error creating manager",
                    Errors = result.Errors.Select(e => e.Description)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating manager");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error creating manager"
                });
            }
        }

        [HttpPut("UpdateUser")]
        [Authorize(Roles = "Administrator")]
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
                        Message = "User updated successfully"
                    });
                }

                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error updating user",
                    Errors = result.Errors.Select(e => e.Description)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error updating user"
                });
            }
        }

        [HttpDelete("DeleteUser")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteUser(string id)
        {
            try
            {
                var result = await _userService.DeleteUserAsync(id);

                if (result.Succeeded)
                {
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "User deleted successfully"
                    });
                }

                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error deleting user",
                    Errors = result.Errors.Select(e => e.Description)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error deleting user"
                });
            }
        }

        [HttpPost("ChangePassword")]
        [Authorize(Roles = "Administrator")]
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
                        Message = "Password changed successfully"
                    });
                }

                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error changing password",
                    Errors = result.Errors.Select(e => e.Description)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error changing password"
                });
            }
        }

        [HttpPost("AssignRole")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<ApiResponse<object>>> AssignRole(string id, [FromBody] AssignRoleRequest request)
        {
            try
            {
                _logger.LogInformation("AssignRole called for user {UserId} with role {RoleName}", id, request.RoleName);

                var result = await _userService.AssignRoleToUserAsync(id, request.RoleName);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Role {RoleName} assigned to user {UserId}", request.RoleName, id);
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Role assigned successfully"
                    });
                }

                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error assigning role",
                    Errors = result.Errors.Select(e => e.Description)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role to user {UserId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        [HttpPost("RemoveRole")]
        [Authorize(Roles = "Administrator")]
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
                        Message = "Role removed successfully"
                    });
                }

                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error removing role",
                    Errors = result.Errors.Select(e => e.Description)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing role from user {UserId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error removing role"
                });
            }
        }

        [HttpGet("GetUserRoles")]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<ActionResult<ApiResponse<IEnumerable<string>>>> GetUserRoles(string id)
        {
            try
            {
                var roles = await _userService.GetUserRolesAsync(id);
                return Ok(new ApiResponse<IEnumerable<string>>
                {
                    Success = true,
                    Data = roles,
                    Message = "Roles retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user roles for {UserId}", id);
                return StatusCode(500, new ApiResponse<IEnumerable<string>>
                {
                    Success = false,
                    Message = "Error retrieving roles"
                });
            }
        }

        [HttpGet("GetAllRoles")]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<ActionResult<ApiResponse<IEnumerable<RoleDTO>>>> GetAllRoles()
        {
            try
            {
                var roles = await _userService.GetAllRolesAsync();
                return Ok(new ApiResponse<IEnumerable<RoleDTO>>
                {
                    Success = true,
                    Data = roles,
                    Message = "Roles retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all roles");
                return StatusCode(500, new ApiResponse<IEnumerable<RoleDTO>>
                {
                    Success = false,
                    Message = "Error retrieving roles"
                });
            }
        }

        [HttpGet("TestAuth")]
        [AllowAnonymous] // For testing
        public IActionResult TestAuth()
        {
            _logger.LogInformation("=== TestAuth Called in AdminUsers Service ===");

            try
            {
                _userContext.LogCurrentUserInfo();

                var result = new
                {
                    ServiceName = "AdminUsers",
                    Timestamp = DateTime.UtcNow,
                    Authentication = new
                    {
                        IsAuthenticated = _userContext.IsAuthenticated(),
                        IsAdministrator = _userContext.IsAdministrator(),
                        IsManager = _userContext.IsManager(),
                        Email = _userContext.GetCurrentUserEmail(),
                        UserId = _userContext.GetCurrentUserId(),
                        Roles = _userContext.GetCurrentUserRoles()
                    },
                    Identity = new
                    {
                        IsAuthenticated = User?.Identity?.IsAuthenticated,
                        Name = User?.Identity?.Name,
                        AuthenticationType = User?.Identity?.AuthenticationType,
                        Claims = User?.Claims?.Select(c => new { c.Type, c.Value }).ToList()
                    }
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TestAuth endpoint");
                return StatusCode(500, new
                {
                    Error = ex.Message,
                    ServiceName = "AdminUsers"
                });
            }
        }
    }
}