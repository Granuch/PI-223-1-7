using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PI_223_1_7.Enums;

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
        public int UserId { get; set; }
        public int BookId { get; set; }
        public DateTime OrderDate { get; set; }
        public OrderStatusTypes Type { get; set; }
        public BookDTO Book { get; set; }
    }
}

