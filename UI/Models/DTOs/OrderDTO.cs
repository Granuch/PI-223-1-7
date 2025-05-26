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
        public int Type { get; set; }

        [Display(Name = "Книга")]
        public BookDTO Book { get; set; }

        [Display(Name = "Користувач")]
        public string UserEmail { get; set; }

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

        public string StatusBadgeClass
        {
            get
            {
                return Type switch
                {
                    1 => "bg-success",
                    2 => "bg-secondary",
                    3 => "bg-danger",
                    _ => "bg-warning"
                };
            }
        }

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