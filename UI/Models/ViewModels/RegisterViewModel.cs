using System.ComponentModel.DataAnnotations;

namespace UI.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Email обов'язковий")]
        [EmailAddress(ErrorMessage = "Невірний формат email")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Ім'я обов'язкове")]
        [Display(Name = "Ім'я")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Прізвище обов'язкове")]
        [Display(Name = "Прізвище")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Номер телефону обов'язковий")]
        [Display(Name = "Номер телефону")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Пароль обов'язковий")]
        [StringLength(100, ErrorMessage = "Пароль повинен містити мінімум {2} символів", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Підтвердження паролю")]
        [Compare("Password", ErrorMessage = "Паролі не співпадають")]
        public string ConfirmPassword { get; set; }
    }
}
