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
        // Методи для отримання користувачів
        Task<IEnumerable<UserDTO>> GetAllUsersAsync();
        Task<UserDTO> GetUserByIdAsync(string userId);
        Task<UserDTO> GetUserByEmailAsync(string email);

        // Методи для створення користувачів
        Task<IdentityResult> CreateUserAsync(CreateUserRequest request);
        Task<IdentityResult> CreateAdminAsync(CreateUserRequest request);
        Task<IdentityResult> CreateManagerAsync(CreateUserRequest request);

        // Методи для управління користувачами
        Task<IdentityResult> UpdateUserAsync(string userId, UpdateUserRequest request);
        Task<IdentityResult> DeleteUserAsync(string userId);
        Task<IdentityResult> ChangeUserPasswordAsync(string userId, string newPassword);

        // Методи для управління ролями
        Task<IdentityResult> AssignRoleToUserAsync(string userId, string roleName);
        Task<IdentityResult> RemoveRoleFromUserAsync(string userId, string roleName);
        Task<IEnumerable<string>> GetUserRolesAsync(string userId);

        // Методи для ролей
        Task<IEnumerable<RoleDTO>> GetAllRolesAsync();
        Task<IdentityResult> CreateRoleAsync(string roleName, string description = null);
    }
}
