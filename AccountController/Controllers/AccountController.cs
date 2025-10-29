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
            _logger.LogInformation($"Received registration request for {model.Email}");

            if (ModelState.IsValid)
            {
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    _logger.LogWarning($"User with email {model.Email} already exists");
                    return BadRequest(new
                    {
                        success = false,
                        message = "A user with this email already exists"
                    });

                }

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PhoneNumber = model.PhoneNumber,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation($"User {user.Email} registered successfully");

                    await _userManager.AddToRoleAsync(user, "RegisteredUser");

                    await _signInManager.SignInAsync(user, isPersistent: false);

                    var token = await GenerateJwtToken(user);
                    var roles = await _userManager.GetRolesAsync(user);

                    _logger.LogInformation($"Automatic login for {user.Email} after registration");

                    return Ok(new
                    {
                        success = true,
                        message = "Registration was successful",
                        token = token,
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

                _logger.LogWarning($"Error during registration of user {model.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                return BadRequest(new
                {
                    success = false,
                    errors = result.Errors.Select(e => e.Description)
                });
            }

            _logger.LogWarning($"Invalid registration data model: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
            return BadRequest(new
            {
                success = false,
                errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            _logger.LogInformation($"Login request received for user: {model.Email}");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning($"Invalid login data model: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
                return BadRequest(new
                {
                    success = false,
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                _logger.LogWarning($"User with email {model.Email} not found in the database");
                return BadRequest(new { success = false, message = "Invalid login or password" });
            }

            _logger.LogInformation($"User found: ID={user.Id}, UserName={user.UserName}");

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!isPasswordValid)
            {
                _logger.LogWarning($"Incorrect password for user {model.Email}");
                return BadRequest(new { success = false, message = "Invalid login or password" });
            }

            _logger.LogInformation($"Password confirmed for {model.Email}, logging in");

            var signInResult = await _signInManager.PasswordSignInAsync(
                user.UserName,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);

            if (signInResult.Succeeded)
            {
                var roles = await _userManager.GetRolesAsync(user);
                _logger.LogInformation($"User {model.Email} logged in successfully. Roles: {string.Join(", ", roles)}");

                var token = await GenerateJwtToken(user);

                return Ok(new
                {
                    success = true,
                    message = "Login successful",
                    token = token,
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

            _logger.LogWarning($"Failed login attempt for {model.Email}. " +
                $"IsLockedOut: {signInResult.IsLockedOut}, " +
                $"IsNotAllowed: {signInResult.IsNotAllowed}, " +
                $"RequiresTwoFactor: {signInResult.RequiresTwoFactor}");

            string errorMessage = "Invalid login or password";
            if (signInResult.IsLockedOut)
                errorMessage = "Account is temporarily locked. Please try again later.";
            else if (signInResult.IsNotAllowed)
                errorMessage = "Login is not allowed. Email confirmation might be required.";
            else if (signInResult.RequiresTwoFactor)
                errorMessage = "Two-factor authentication is required.";

            return BadRequest(new { success = false, message = errorMessage });

        }

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
                Expires = DateTime.UtcNow.AddDays(7),
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
            string userName = User?.Identity?.Name ?? "Not authenticated";
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
            string email = User.Identity?.IsAuthenticated == true ? User.Identity.Name : "Anonymous user";
            _logger.LogInformation($"Logout request received for user {email}");

            await _signInManager.SignOutAsync();
            _logger.LogInformation($"User {email} has logged out");

            return Ok(new { success = true, message = "Logout successful" });
        }

        [HttpGet("status")]
        public async Task<IActionResult> CheckAuthStatus()
        {
            _logger.LogInformation("Authentication status check request received");

            if (User.Identity?.IsAuthenticated == true)
            {
                _logger.LogInformation($"Authenticated user: {User.Identity.Name}");

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning($"User is authenticated ({User.Identity.Name}) but not found in the database");
                    return Ok(new { isAuthenticated = false });
                }

                var roles = await _userManager.GetRolesAsync(user);
                _logger.LogInformation($"Authentication status for {user.Email}: authenticated. Roles: {string.Join(", ", roles)}");

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

            _logger.LogInformation("Status check: user is not authenticated");
            return Ok(new { isAuthenticated = false });
        }


        [HttpGet("me")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                {
                    return BadRequest(new { success = false, message = "Failed to retrieve user data" });
                }

                var user = await _userManager.FindByEmailAsync(userEmail);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found" });
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
                return StatusCode(500, new { success = false, message = "Internal server error" });
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
                        Message = "User is not authorized"
                    });
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("RefreshSession: User not found in database");
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                await _signInManager.RefreshSignInAsync(user);

                _logger.LogInformation("Session refreshed for user: {Email}", user.Email);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Session refreshed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing session");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error refreshing session"
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
                        Message = "Email is required"
                    });
                }

                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    _logger.LogWarning("User not found: {Email}", request.Email);
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                await _signInManager.SignInAsync(user, isPersistent: true);

                _logger.LogInformation("Session refreshed for user: {Email}", user.Email);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Session refreshed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CheckAndRefreshSession for email: {Email}", request?.Email);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error refreshing session"
                });
            }
        }


        public class RefreshSessionRequest
        {
            public string Email { get; set; }
        }
    }
}