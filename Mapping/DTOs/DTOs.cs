using PI_223_1_7.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mapping.DTOs
{
    public class BookDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public GenreTypes Genre { get; set; }
        public BookTypes Type { get; set; }
        public bool IsAvailable { get; set; }
        public DateTime Year { get; set; }
    }

    public class OrderDTO
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int BookId { get; set; }
        public DateTime OrderDate { get; set; }
        public OrderStatusTypes Type { get; set; }
        public BookDTO Book { get; set; }
    }

    public class UserDTO
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public DateTime CreatedAt { get; set; }
        public ICollection<string> Roles { get; set; }
    }

    public class RoleDTO
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    // DTO для створення користувача
    public class CreateUserRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        public string PhoneNumber { get; set; }

        [Required]
        public string Role { get; set; } // "Administrator", "Manager", "RegisteredUser"
    }

    // DTO для оновлення користувача
    public class UpdateUserRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
    }

    // DTO для зміни паролю
    public class ChangePasswordRequest
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string NewPassword { get; set; }
    }

    // DTO для призначення ролі
    public class AssignRoleRequest
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        public string RoleName { get; set; }
    }

    // DTO для створення ролі
    public class CreateRoleRequest
    {
        [Required]
        public string Name { get; set; }

        public string Description { get; set; }
    }

    // Відповідь API
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public IEnumerable<string> Errors { get; set; }
    }
}

