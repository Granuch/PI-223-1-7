using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PI_223_1_7.Enums;
using System.ComponentModel.DataAnnotations;


namespace PI_223_1_7.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public int BookId { get; set; }

        [Required]
        public required DateTime OrderDate { get; set; }

        [Required]
        public required OrderStatusTypes Type { get; set; } // Pending, Approved, Completed, Cancelled

        public virtual Book Book { get; set; }
    }
}
