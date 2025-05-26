using Newtonsoft.Json;

namespace UI.Models.DTOs
{
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
        public IEnumerable<string> Roles { get; set; }
    }

    public class CreateUserRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string Role { get; set; } = "RegisteredUser";
    }

    public class UpdateUserRequest
    {
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
    }

    public class ChangePasswordRequest
    {
        public string UserId { get; set; }
        public string NewPassword { get; set; }
    }

    public class AssignRoleRequest
    {
        [JsonProperty("userId")]
        public string UserId { get; set; }

        [JsonProperty("roleName")]
        public string RoleName { get; set; }
    }

    public class RoleDTO
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public static class RoleConstants
    {
        public const string RegisteredUser = "RegisteredUser";
        public const string Manager = "Manager";
        public const string Administrator = "Administrator";

        public static readonly string[] ValidRoles = { RegisteredUser, Manager, Administrator };

        public static bool IsValidRole(string roleName)
        {
            Console.WriteLine($"RoleConstants.IsValidRole called with: '{roleName}'");
            var isValid = ValidRoles.Contains(roleName);
            Console.WriteLine($"RoleConstants.IsValidRole result: {isValid}");
            return isValid;
        }
    }

    public class ValidationErrorResponse
    {
        public string Type { get; set; }
        public string Title { get; set; }
        public int Status { get; set; }
        public string TraceId { get; set; }
        public Dictionary<string, string[]> Errors { get; set; }
    }
}