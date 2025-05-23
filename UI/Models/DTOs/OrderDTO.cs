using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace UI.Models.DTOs
{
    public class OrderDTO
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "ID користувача")]
        public string UserId { get; set; }

        [Required]
        [Display(Name = "ID книги")]
        public int BookId { get; set; }

        [Display(Name = "Дата замовлення")]
        public DateTime OrderDate { get; set; }

        [Display(Name = "Дата повернення")]
        public DateTime? ReturnDate { get; set; }

        [Display(Name = "Тип замовлення")]
        public int Type { get; set; } // 1 - Активне, 2 - Повернено, тощо

        // Для відображення
        [Display(Name = "Книга")]
        public BookDTO Book { get; set; }

        [Display(Name = "Користувач")]
        public string UserEmail { get; set; }

        // Властивість для зручного відображення статусу
        [Display(Name = "Статус")]
        public string Status
        {
            get
            {
                return Type switch
                {
                    1 => "Active",
                    2 => "Returned",
                    3 => "Overdue",
                    _ => "Unknown"
                };
            }
        }

        // Властивість для відображення кольору статусу
        public string StatusBadgeClass
        {
            get
            {
                return Type switch
                {
                    1 => "bg-success", // Активне - зелений
                    2 => "bg-secondary", // Повернено - сірий
                    3 => "bg-danger", // Прострочено - червоний
                    _ => "bg-warning" // Невідомо - жовтий
                };
            }
        }

        // Властивість для відображення статусу українською
        public string StatusUkrainian
        {
            get
            {
                return Type switch
                {
                    1 => "Активне",
                    2 => "Повернено",
                    3 => "Прострочено",
                    _ => "Невідомо"
                };
            }
        }
    }
}