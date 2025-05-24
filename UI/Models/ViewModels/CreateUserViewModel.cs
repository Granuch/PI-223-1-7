using System.ComponentModel.DataAnnotations;
using UI.Models.DTOs;

namespace UI.Models.ViewModels
{
    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "Email є обов'язковим")]
        [EmailAddress(ErrorMessage = "Невірний формат email")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Пароль є обов'язковим")]
        [StringLength(100, ErrorMessage = "Пароль повинен містити від {2} до {1} символів.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Підтвердження паролю")]
        [Compare("Password", ErrorMessage = "Пароль та підтвердження паролю не співпадають.")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Ім'я є обов'язковим")]
        [Display(Name = "Ім'я")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Прізвище є обов'язковим")]
        [Display(Name = "Прізвище")]
        public string LastName { get; set; }

        [Phone(ErrorMessage = "Невірний формат телефону")]
        [Display(Name = "Телефон")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Оберіть тип користувача")]
        [Display(Name = "Тип користувача")]
        public string UserType { get; set; }

        public List<SelectListItem> UserTypeOptions => new List<SelectListItem>
        {
            new SelectListItem { Value = "RegisteredUser", Text = "Звичайний користувач" },
            new SelectListItem { Value = "Manager", Text = "Менеджер" },
            new SelectListItem { Value = "Administrator", Text = "Адміністратор" }
        };
    }

    public class EditUserViewModel
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "Email є обов'язковим")]
        [EmailAddress(ErrorMessage = "Невірний формат email")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Ім'я є обов'язковим")]
        [Display(Name = "Ім'я")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Прізвище є обов'язковим")]
        [Display(Name = "Прізвище")]
        public string LastName { get; set; }

        [Phone(ErrorMessage = "Невірний формат телефону")]
        [Display(Name = "Телефон")]
        public string PhoneNumber { get; set; }

        public List<string> Roles { get; set; } = new List<string>();
    }

    public class ChangePasswordViewModel
    {
        public string UserId { get; set; }
        public string UserEmail { get; set; }

        [Required(ErrorMessage = "Новий пароль є обов'язковим")]
        [StringLength(100, ErrorMessage = "Пароль повинен містити від {2} до {1} символів.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Новий пароль")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Підтвердження нового паролю")]
        [Compare("NewPassword", ErrorMessage = "Новий пароль та підтвердження не співпадають.")]
        public string ConfirmNewPassword { get; set; }
    }

    public class ManageRolesViewModel
    {
        public string UserId { get; set; }
        public string UserEmail { get; set; }
        public List<RoleDTO> AllRoles { get; set; } = new List<RoleDTO>();
        public List<string> UserRoles { get; set; } = new List<string>();

        public bool HasRole(string roleName)
        {
            return UserRoles.Contains(roleName);
        }
    }

    public class SelectListItem
    {
        public string Value { get; set; }
        public string Text { get; set; }
        public bool Selected { get; set; }
    }
}