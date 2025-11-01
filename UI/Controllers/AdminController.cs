using Microsoft.AspNetCore.Authorization;
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

        public IActionResult Index()
        {
            if (ViewBag.IsAdministrator != true)
            {
                TempData["ErrorMessage"] = "Access denied. Only administrators can view this page.";
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        public async Task<IActionResult> Users()
        {
            if (ViewBag.IsAdministrator != true)
            {
                TempData["ErrorMessage"] = "Access denied.";
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


        [HttpGet]
        public async Task<IActionResult> UserDetails(string id)
        {
            if (ViewBag.IsAdministrator != true)
            {
                TempData["ErrorMessage"] = "Access denied.";
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


        [HttpGet]
        //[Authorize(Roles = "Administrator")]
        public IActionResult CreateUser()
        {
            if (ViewBag.IsAdministrator != true)
            {
                TempData["ErrorMessage"] = "Access denied.";
                return RedirectToAction("Index", "Home");
            }

            return View(new CreateUserViewModel());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            if (ViewBag.IsAdministrator != true)
            {
                TempData["ErrorMessage"] = "Access denied.";
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
                Role = model.UserType
            };

            ApiResponse<object> result = model.UserType switch
            {
                "RegisteredUser" => await _apiService.CreateUserAsync(request),
                "Manager" => await _apiService.CreateManagerAsync(request),
                "Administrator" => await _apiService.CreateAdminAsync(request),
                _ => new ApiResponse<object> { Success = false, Message = "Invalid user type" }
            };

            if (result.Success)
            {
                TempData["SuccessMessage"] = $"User successfully created with role {model.UserType}!";
                return RedirectToAction("Users");
            }

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


        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            if (ViewBag.IsAdministrator != true)
            {
                TempData["ErrorMessage"] = "Access denied.";
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        //[Authorize(Roles = "Administrator")]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (ViewBag.IsAdministrator != true)
            {
                TempData["ErrorMessage"] = "Access denied.";
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
                TempData["SuccessMessage"] = "User updated successfully!";
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


        [HttpGet]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (ViewBag.IsAdministrator != true)
            {
                TempData["ErrorMessage"] = "Access denied.";
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


        [HttpPost, ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        //[Authorize(Roles = "Administrator")]
        public async Task<IActionResult> DeleteUserConfirmed(string id)
        {
            if (ViewBag.IsAdministrator != true)
            {
                TempData["ErrorMessage"] = "Access denied.";
                return RedirectToAction("Index", "Home");
            }

            var result = await _apiService.DeleteUserAsync(id);

            if (result.Success)
            {
                TempData["SuccessMessage"] = "User deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction("Users");
        }


        [HttpGet]
        public async Task<IActionResult> ChangePassword(string id)
        {
            if (ViewBag.IsAdministrator != true)
            {
                TempData["ErrorMessage"] = "Access denied.";
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


        [HttpPost]
        [ValidateAntiForgeryToken]
        //[Authorize(Roles = "Administrator")]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            _logger.LogInformation("ChangePassword POST called for user {UserId}", model?.UserId);

            if (ViewBag.IsAdministrator != true)
            {
                _logger.LogWarning("Non-administrator tried to change password");
                TempData["ErrorMessage"] = "Access denied.";
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
                    UserId = model.UserId,
                    NewPassword = model.NewPassword
                };

                _logger.LogInformation("Calling API to change password for user {UserId}", model.UserId);
                var result = await _apiService.ChangeUserPasswordAsync(model.UserId, request);

                _logger.LogInformation("API response: Success={Success}, Message={Message}",
                    result.Success, result.Message);

                if (result.Success)
                {
                    _logger.LogInformation("Password changed successfully for user {UserId}", model.UserId);
                    TempData["SuccessMessage"] = "Password changed successfully!";
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
                    ModelState.AddModelError("", result.Message ?? "Unknown error");
                }

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
                ModelState.AddModelError("", "Unexcepted error");

                try
                {
                    var userReloadResult = await _apiService.GetUserByIdAsync(model.UserId);
                    if (userReloadResult.Success)
                    {
                        model.UserEmail = userReloadResult.Data.Email;
                    }
                }
                catch{}

                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ManageRoles(string id)
        {
            if (ViewBag.IsAdministrator != true)
            {
                TempData["ErrorMessage"] = "Access denied.";
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

            TempData["ErrorMessage"] = "Error loading data";
            return RedirectToAction("Users");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        //[Authorize(Roles = "Administrator")]
        public async Task<IActionResult> AssignRole(string userId, string roleName)
        {
            Console.WriteLine("=================================================");
            Console.WriteLine("ASSIGNROLE METHOD CALLED!");
            Console.WriteLine($"userId: '{userId}'");
            Console.WriteLine($"roleName: '{roleName}'");
            Console.WriteLine($"ViewBag.IsAdministrator: {ViewBag.IsAdministrator}");
            Console.WriteLine("=================================================");

            if (ViewBag.IsAdministrator != true)
            {
                Console.WriteLine("ACCESS DENIED!");
                TempData["ErrorMessage"] = "Access denied.";
                return RedirectToAction("Index", "Home");
            }

            if (!RoleConstants.IsValidRole(roleName))
            {
                Console.WriteLine($"INVALID ROLE: {roleName}");
                TempData["ErrorMessage"] = $"Invalid role: {roleName}";
                return RedirectToAction("ManageRoles", new { id = userId });
            }

            Console.WriteLine("CREATING REQUEST...");
            var request = new AssignRoleRequest { RoleName = roleName };

            Console.WriteLine("CALLING API SERVICE...");
            var result = await _apiService.AssignRoleToUserAsync(userId, request);

            Console.WriteLine($"API RESULT: Success={result.Success}, Message='{result.Message}'");

            if (result.Success)
            {
                TempData["SuccessMessage"] = $"Role {roleName} assigned successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            Console.WriteLine("REDIRECTING TO MANAGEROLES...");
            return RedirectToAction("ManageRoles", new { id = userId });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        //[Authorize(Roles = "Administrator")]
        public async Task<IActionResult> RemoveRole(string userId, string roleName)
        {
            if (ViewBag.IsAdministrator != true)
            {
                TempData["ErrorMessage"] = "Access denied.";
                return RedirectToAction("Index", "Home");
            }

            var request = new AssignRoleRequest { RoleName = roleName };
            var result = await _apiService.RemoveRoleFromUserAsync(userId, request);

            if (result.Success)
            {
                TempData["SuccessMessage"] = $"Role {roleName} removed successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction("ManageRoles", new { id = userId });
        }


        [HttpGet]
        public async Task<IActionResult> DiagnosticAuth()
        {
            var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AdminController>>();

            logger.LogInformation("=== DIAGNOSTIC AUTH TEST ===");

            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                var cookieHeader = string.Join("; ", HttpContext.Request.Cookies.Select(c => $"{c.Key}={c.Value}"));
                httpClient.DefaultRequestHeaders.Add("Cookie", cookieHeader);

                logger.LogInformation("Making direct call to AdminUsers service...");
                logger.LogInformation("Cookie header length: {Length}", cookieHeader.Length);

                var directResponse = await httpClient.GetAsync("http://localhost:5005/AdminUsers/TestAuth");
                var directContent = await directResponse.Content.ReadAsStringAsync();

                logger.LogInformation("Direct call result: Status={Status}, Content={Content}",
                    directResponse.StatusCode, directContent);

                var ocelotResponse = await httpClient.GetAsync("https://localhost:5003/api/users/testauth");
                var ocelotContent = await ocelotResponse.Content.ReadAsStringAsync();

                logger.LogInformation("Ocelot call result: Status={Status}, Content={Content}",
                    ocelotResponse.StatusCode, ocelotContent);

                var usersResponse = await httpClient.GetAsync("https://localhost:5003/api/users/getall");
                var usersContent = await usersResponse.Content.ReadAsStringAsync();

                logger.LogInformation("Users call result: Status={Status}, Content={Content}",
                    usersResponse.StatusCode, usersContent);

                return Json(new
                {
                    Success = true,
                    Tests = new
                    {
                        DirectCall = new
                        {
                            Status = directResponse.StatusCode.ToString(),
                            Content = directContent,
                            StatusCode = (int)directResponse.StatusCode
                        },
                        OcelotTestAuth = new
                        {
                            Status = ocelotResponse.StatusCode.ToString(),
                            Content = ocelotContent,
                            StatusCode = (int)ocelotResponse.StatusCode
                        },
                        OcelotGetUsers = new
                        {
                            Status = usersResponse.StatusCode.ToString(),
                            Content = usersContent,
                            StatusCode = (int)usersResponse.StatusCode
                        }
                    },
                    CurrentUserInfo = new
                    {
                        IsAuthenticated = User.Identity?.IsAuthenticated,
                        UserName = User.Identity?.Name,
                        Email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value,
                        Roles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToArray(),
                        UserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    },
                    Cookies = HttpContext.Request.Cookies.Select(c => new {
                        c.Key,
                        ValueLength = c.Value.Length,
                        Value = c.Key == "LibraryApp.AuthCookie" ? c.Value.Substring(0, Math.Min(100, c.Value.Length)) + "..." : "hidden"
                    }).ToArray(),
                    CookieHeaderLength = cookieHeader.Length
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in diagnostic test");
                return Json(new
                {
                    Success = false,
                    Error = ex.Message,
                    StackTrace = ex.StackTrace
                });
            }
        }

        [HttpGet]
        public IActionResult CookieDebug()
        {
            var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AdminController>>();

            logger.LogInformation("=== COOKIE DEBUG ===");

            var cookieInfo = new Dictionary<string, object>();

            foreach (var cookie in HttpContext.Request.Cookies)
            {
                logger.LogInformation("Cookie: {Name} = {Value}", cookie.Key,
                    cookie.Value.Length > 100 ? cookie.Value.Substring(0, 100) + "..." : cookie.Value);

                cookieInfo[cookie.Key] = new
                {
                    Length = cookie.Value.Length,
                    Value = cookie.Key == "LibraryApp.AuthCookie" ? cookie.Value : "hidden for security"
                };
            }

            var hasAuthCookie = HttpContext.Request.Cookies.ContainsKey("LibraryApp.AuthCookie");
            var authCookieValue = HttpContext.Request.Cookies["LibraryApp.AuthCookie"];

            logger.LogInformation("Has LibraryApp.AuthCookie: {HasCookie}", hasAuthCookie);
            if (hasAuthCookie)
            {
                logger.LogInformation("LibraryApp.AuthCookie length: {Length}", authCookieValue?.Length ?? 0);
            }

            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToArray();

            return Json(new
            {
                Success = true,
                HasLibraryAppAuthCookie = hasAuthCookie,
                LibraryAppAuthCookieLength = authCookieValue?.Length ?? 0,
                TotalCookies = HttpContext.Request.Cookies.Count,
                Cookies = cookieInfo,
                UserInfo = new
                {
                    IsAuthenticated = User.Identity?.IsAuthenticated,
                    Name = User.Identity?.Name,
                    AuthenticationType = User.Identity?.AuthenticationType,
                    Claims = claims
                },
                Tests = new
                {
                    DirectAdminUsersTest = "http://localhost:5005/AdminUsers/SimpleTest",
                    OcelotDebugTest = "https://localhost:5003/api/users/debug"
                }
            });
        }
    }
}