using PI_223_1_7.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Interfaces
{
    public interface IOrderService
    {
        public Task<Order> CreateOrder(Order order);
        public Task<IEnumerable<Order>> GetAllWithDetails();
        public Task<IEnumerable<Order>> GetAllWithoutDetails();
    }

}
