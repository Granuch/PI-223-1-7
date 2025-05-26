using AutoMapper;
using BLL.Interfaces;
using Mapping.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PI_223_1_7.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly ILogger<UserService> _logger;

        public UserService(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IConfiguration configuration,
            IMapper mapper,
            ILogger<UserService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<UserDTO>> GetAllUsersAsync()
        {
            var users = await _userManager.Users.ToListAsync();
            var userDTOs = new List<UserDTO>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDTOs.Add(new UserDTO
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    FullName = $"{user.FirstName} {user.LastName}",
                    CreatedAt = user.CreatedAt,
                    Roles = roles
                });
            }

            return userDTOs;
        }

        public async Task<UserDTO> GetUserByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);

            return new UserDTO
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = $"{user.FirstName} {user.LastName}",
                CreatedAt = user.CreatedAt,
                Roles = roles
            };
        }

        public async Task<UserDTO> GetUserByEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);

            return new UserDTO
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = $"{user.FirstName} {user.LastName}",
                CreatedAt = user.CreatedAt,
                Roles = roles
            };
        }

        public async Task<IdentityResult> CreateUserAsync(CreateUserRequest request)
        {
            return await CreateUserWithRoleAsync(request, "RegisteredUser");
        }

        public async Task<IdentityResult> CreateAdminAsync(CreateUserRequest request)
        {
            return await CreateUserWithRoleAsync(request, "Administrator");
        }

        public async Task<IdentityResult> CreateManagerAsync(CreateUserRequest request)
        {
            return await CreateUserWithRoleAsync(request, "Manager");
        }

        private async Task<IdentityResult> CreateUserWithRoleAsync(CreateUserRequest request, string roleName)
        {
            try
            {
                var existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return IdentityResult.Failed(new IdentityError
                    {
                        Description = "A user with this email already exists"
                    });
                }

                var user = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    PhoneNumber = request.PhoneNumber,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, request.Password);

                if (result.Succeeded)
                {
                    if (!await _roleManager.RoleExistsAsync(roleName))
                    {
                        await _roleManager.CreateAsync(new ApplicationRole(roleName));
                    }

                    await _userManager.AddToRoleAsync(user, roleName);

                    _logger.LogInformation("User {Email} created successfully with role {Role}", request.Email, roleName);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user {Email}", request.Email);
                return IdentityResult.Failed(new IdentityError
                {
                    Description = "User creation error"
                });
            }
        }

        public async Task<IdentityResult> UpdateUserAsync(string userId, UpdateUserRequest request)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return IdentityResult.Failed(new IdentityError
                    {
                        Description = "User not found"
                    });
                }

                user.FirstName = request.FirstName ?? user.FirstName;
                user.LastName = request.LastName ?? user.LastName;
                user.PhoneNumber = request.PhoneNumber ?? user.PhoneNumber;

                if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
                {
                    user.Email = request.Email;
                    user.UserName = request.Email;
                }

                return await _userManager.UpdateAsync(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", userId);
                return IdentityResult.Failed(new IdentityError
                {
                    Description = "User update error"
                });
            }
        }

        public async Task<IdentityResult> DeleteUserAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return IdentityResult.Failed(new IdentityError
                    {
                        Description = "User not found"
                    });
                }

                var result = await _userManager.DeleteAsync(user);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User {UserId} deleted successfully", userId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", userId);
                return IdentityResult.Failed(new IdentityError
                {
                    Description = "Error deleting user"
                });
            }
        }

        public async Task<IdentityResult> ChangeUserPasswordAsync(string userId, string newPassword)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return IdentityResult.Failed(new IdentityError
                    {
                        Description = "User not found"
                    });
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                return await _userManager.ResetPasswordAsync(user, token, newPassword);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", userId);
                return IdentityResult.Failed(new IdentityError
                {
                    Description = "Error changing password"
                });
            }
        }

        public async Task<IdentityResult> AssignRoleToUserAsync(string userId, string roleName)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return IdentityResult.Failed(new IdentityError
                    {
                        Description = "User not found"
                    });
                }

                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    return IdentityResult.Failed(new IdentityError
                    {
                        Description = "Role does not exist"
                    });
                }

                if (await _userManager.IsInRoleAsync(user, roleName))
                {
                    return IdentityResult.Failed(new IdentityError
                    {
                        Description = "The user already has this role"
                    });
                }

                return await _userManager.AddToRoleAsync(user, roleName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role {Role} to user {UserId}", roleName, userId);
                return IdentityResult.Failed(new IdentityError
                {
                    Description = "Role assignment error"
                });
            }
        }

        public async Task<IdentityResult> RemoveRoleFromUserAsync(string userId, string roleName)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return IdentityResult.Failed(new IdentityError
                    {
                        Description = "User not found"
                    });
                }

                if (!await _userManager.IsInRoleAsync(user, roleName))
                {
                    return IdentityResult.Failed(new IdentityError
                    {
                        Description = "The user does not have this role"
                    });
                }

                return await _userManager.RemoveFromRoleAsync(user, roleName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing role {Role} from user {UserId}", roleName, userId);
                return IdentityResult.Failed(new IdentityError
                {
                    Description = "Role removal error"
                });
            }
        }

        public async Task<IEnumerable<string>> GetUserRolesAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return new List<string>();

            return await _userManager.GetRolesAsync(user);
        }
        public async Task<IEnumerable<RoleDTO>> GetAllRolesAsync()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            return roles.Select(r => new RoleDTO
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description
            });
        }

        public async Task<IdentityResult> CreateRoleAsync(string roleName, string description = null)
        {
            try
            {
                if (await _roleManager.RoleExistsAsync(roleName))
                {
                    return IdentityResult.Failed(new IdentityError
                    {
                        Description = "Role already exist"
                    });
                }

                var role = new ApplicationRole(roleName)
                {
                    Description = description
                };

                return await _roleManager.CreateAsync(role);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating role {RoleName}", roleName);
                return IdentityResult.Failed(new IdentityError
                {
                    Description = "Role creation error"
                });
            }
        }

    }
}
