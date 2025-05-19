using PI_223_1_7.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PI_223_1_7.Models
{
    public class Book
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public required string Name { get; set; }

        [Required]
        public required string Author {  get; set; }

        [Required]
        public required string Description { get; set; }

        [Required]
        public required GenreTypes Genre { get; set; }

        [Required]
        public required BookTypes Type { get; set; }

        [Required]
        public bool IsAvaliable { get; set; }

        [Required]
        public required DateTime Year { get; set; }

        public virtual ICollection<Order>? Orders { get; set; }

        public override string ToString()
        {
            return $"Book: {Name}, Author: {Author}, Genre: {Genre}, Type: {Type}, Available: {IsAvaliable}, Year: {Year.Year}";
        }
    }
}
