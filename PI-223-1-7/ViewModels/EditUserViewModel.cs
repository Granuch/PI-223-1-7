using System.ComponentModel.DataAnnotations;

namespace PL.ViewModels
{
    public class EditUserViewModel
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "Введіть Email")]
        [EmailAddress(ErrorMessage = "Неправильний формат Email")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Введіть ім'я")]
        [Display(Name = "Ім'я")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Введіть прізвище")]
        [Display(Name = "Прізвище")]
        public string LastName { get; set; }

        public List<string> UserRoles { get; set; } = new List<string>();
        public List<RoleViewModel> AllRoles { get; set; } = new List<RoleViewModel>();
    }
}
