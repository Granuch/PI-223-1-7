using Mapping.DTOs;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Interfaces
{
    public interface IUserService
    {
        // Methods for retrieving users
        Task<IEnumerable<UserDTO>> GetAllUsersAsync();
        Task<UserDTO> GetUserByIdAsync(string userId);
        Task<UserDTO> GetUserByEmailAsync(string email);

        // Methods for creating users
        Task<IdentityResult> CreateUserAsync(CreateUserRequest request);
        Task<IdentityResult> CreateAdminAsync(CreateUserRequest request);
        Task<IdentityResult> CreateManagerAsync(CreateUserRequest request);

        // Methods for managing users
        Task<IdentityResult> UpdateUserAsync(string userId, UpdateUserRequest request);
        Task<IdentityResult> DeleteUserAsync(string userId);
        Task<IdentityResult> ChangeUserPasswordAsync(string userId, string newPassword);

        // Methods for managing roles
        Task<IdentityResult> AssignRoleToUserAsync(string userId, string roleName);
        Task<IdentityResult> RemoveRoleFromUserAsync(string userId, string roleName);
        Task<IEnumerable<string>> GetUserRolesAsync(string userId);

        // Methods for roles
        Task<IEnumerable<RoleDTO>> GetAllRolesAsync();
        Task<IdentityResult> CreateRoleAsync(string roleName, string description = null);
    }
}
