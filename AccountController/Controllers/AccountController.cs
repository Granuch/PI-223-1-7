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
            _logger.LogInformation($"�������� ����� �� ��������� ��� {model.Email}");

            if (ModelState.IsValid)
            {
                // ����������, �� �� ���� ��� ����� ����������
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    _logger.LogWarning($"���������� � email {model.Email} ��� ����");
                    return BadRequest(new
                    {
                        success = false,
                        message = "���������� � ����� email ��� ����"
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
                    _logger.LogInformation($"���������� {user.Email} ������ �������������");

                    // ������ ����
                    await _userManager.AddToRoleAsync(user, "RegisteredUser");

                    // ������: �������� Cookie ���� ��� MVC �������
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    // ������: �������� JWT ����� ��� API
                    var token = await GenerateJwtToken(user);
                    var roles = await _userManager.GetRolesAsync(user);

                    _logger.LogInformation($"������������ ���� ��� {user.Email} ���� ���������");

                    return Ok(new
                    {
                        success = true,
                        message = "��������� ������� ������",
                        token = token, // ������: JWT �����
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

                _logger.LogWarning($"������� ��� ��������� ����������� {model.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                return BadRequest(new
                {
                    success = false,
                    errors = result.Errors.Select(e => e.Description)
                });
            }

            _logger.LogWarning($"����������� ������ ����� ��� ���������: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
            return BadRequest(new
            {
                success = false,
                errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            _logger.LogInformation($"�������� ����� �� ���� ��� �����������: {model.Email}");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning($"����������� ������ ����� ��� �����: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
                return BadRequest(new
                {
                    success = false,
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });
            }

            // ���� ������ ����������� �� Email
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                _logger.LogWarning($"���������� � email {model.Email} �� ��������� � ��� �����");
                return BadRequest(new { success = false, message = "������� ���� ��� ������" });
            }

            _logger.LogInformation($"�������� �����������: ID={user.Id}, UserName={user.UserName}");

            // �������� ������
            var isPasswordValid = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!isPasswordValid)
            {
                _logger.LogWarning($"������������ ������ ��� ����������� {model.Email}");
                return BadRequest(new { success = false, message = "������� ���� ��� ������" });
            }

            _logger.LogInformation($"������ ����������� ��� {model.Email}, ���������� ����");

            // ��������: �������� Cookie ���� ��� MVC �������
            var signInResult = await _signInManager.PasswordSignInAsync(
                user.UserName,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);

            if (signInResult.Succeeded)
            {
                var roles = await _userManager.GetRolesAsync(user);
                _logger.LogInformation($"���������� {model.Email} ������ ������. ���: {string.Join(", ", roles)}");

                // ������: �������� JWT ����� ��� API ������
                var token = await GenerateJwtToken(user);

                return Ok(new
                {
                    success = true,
                    message = "���� �������� ������",
                    token = token, // ������: JWT �����
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

            // �������� ��������� ������� �������
            _logger.LogWarning($"������� ������ ����� ��� {model.Email}. " +
                $"IsLockedOut: {signInResult.IsLockedOut}, " +
                $"IsNotAllowed: {signInResult.IsNotAllowed}, " +
                $"RequiresTwoFactor: {signInResult.RequiresTwoFactor}");

            string errorMessage = "������� ���� ��� ������";
            if (signInResult.IsLockedOut)
                errorMessage = "�������� ����� ��������� �����������. ��������� �����.";
            else if (signInResult.IsNotAllowed)
                errorMessage = "���� ����������. �������, ������� ���������� email.";
            else if (signInResult.RequiresTwoFactor)
                errorMessage = "������� ����������� ��������������.";

            return BadRequest(new { success = false, message = errorMessage });
        }

        // ������: ����� ��� ��������� JWT ������
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

            // ������ ��� �� claims
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
                Expires = DateTime.UtcNow.AddDays(7), // ����� ������ 7 ���
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
            string userName = User?.Identity?.Name ?? "�� ���������������";
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
            string email = User.Identity?.IsAuthenticated == true ? User.Identity.Name : "�������� ����������";
            _logger.LogInformation($"�������� ����� �� ����� �� ������� ��� ����������� {email}");

            await _signInManager.SignOutAsync();
            _logger.LogInformation($"���������� {email} ������ �� �������");

            return Ok(new { success = true, message = "����� �������� ������" });
        }

        [HttpGet("status")]
        public async Task<IActionResult> CheckAuthStatus()
        {
            _logger.LogInformation("�������� ����� �� �������� ������� ��������������");

            if (User.Identity?.IsAuthenticated == true)
            {
                _logger.LogInformation($"���������������� ����������: {User.Identity.Name}");

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning($"���������� ���������������� ({User.Identity.Name}), ��� �� ��������� � ��� �����");
                    return Ok(new { isAuthenticated = false });
                }

                var roles = await _userManager.GetRolesAsync(user);
                _logger.LogInformation($"������ �������������� ��� {user.Email}: ����������������. ���: {string.Join(", ", roles)}");

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

            _logger.LogInformation("�������� �������: ���������� �� ����������������");
            return Ok(new { isAuthenticated = false });
        }

        // ������: Endpoint ��� �������� JWT ������
        [HttpGet("me")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                {
                    return BadRequest(new { success = false, message = "�� ������� �������� ��� �����������" });
                }

                var user = await _userManager.FindByEmailAsync(userEmail);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "����������� �� ��������" });
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
                return StatusCode(500, new { success = false, message = "�������� ������� �������" });
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
                        Message = "���������� �� �������������"
                    });
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("RefreshSession: User not found in database");
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "���������� �� ���������"
                    });
                }

                // ��������� cookie �����������
                await _signInManager.RefreshSignInAsync(user);

                _logger.LogInformation("Session refreshed for user: {Email}", user.Email);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "���� �������� ������"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing session");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "������� ��������� ���"
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
                        Message = "Email ����'�������"
                    });
                }

                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    _logger.LogWarning("User not found: {Email}", request.Email);
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "���������� �� ���������"
                    });
                }

                // ϳ������� ����������� ����� � ������ �������
                await _signInManager.SignInAsync(user, isPersistent: true);

                _logger.LogInformation("Session refreshed for user: {Email}", user.Email);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "���� �������� ������"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CheckAndRefreshSession for email: {Email}", request?.Email);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "������� ��������� ���"
                });
            }
        }

        public class RefreshSessionRequest
        {
            public string Email { get; set; }
        }
    }
}