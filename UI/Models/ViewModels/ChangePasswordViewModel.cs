//using System.ComponentModel.DataAnnotations;

//public class ChangePasswordViewModel
//{
//    public string UserId { get; set; }
//    public string UserEmail { get; set; }

//    [Required(ErrorMessage = "Новий пароль є обов'язковим")]
//    [StringLength(100, ErrorMessage = "Пароль повинен містити від {2} до {1} символів.", MinimumLength = 6)]
//    [DataType(DataType.Password)]
//    [Display(Name = "Новий пароль")]
//    public string NewPassword { get; set; }

//    [DataType(DataType.Password)]
//    [Display(Name = "Підтвердження нового паролю")]
//    [Compare("NewPassword", ErrorMessage = "Новий пароль та підтвердження не співпадають.")]
//    public string ConfirmNewPassword { get; set; }
//}
