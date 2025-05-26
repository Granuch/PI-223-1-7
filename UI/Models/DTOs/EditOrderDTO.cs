using System.ComponentModel.DataAnnotations;

namespace UI.Models.DTOs
{
    public class EditOrderDTO
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Користувач")]
        public string UserId { get; set; }

        [Required]
        [Display(Name = "Книга")]
        public int BookId { get; set; }

        [Required]
        [Display(Name = "Дата замовлення")]
        public DateTime OrderDate { get; set; }

        [Display(Name = "Дата повернення")]
        public DateTime? ReturnDate { get; set; }

        [Required]
        [Display(Name = "Статус")]
        public int Type { get; set; }

        public BookDTO? Book { get; set; }
        public string? UserEmail { get; set; }
    }
}
