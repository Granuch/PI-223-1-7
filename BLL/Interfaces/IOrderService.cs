using Mapping.DTOs;
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
        public Task<OrderDTO> CreateOrder(OrderDTO order);
        public Task<IEnumerable<OrderDTO>> GetAllWithDetails();
        public Task<IEnumerable<OrderDTO>> GetAllWithoutDetails();
        public Task DeleteOrderById(int i);
        public Task UpdateOrder(OrderDTO order);
        public Task<OrderDTO> GetSpecificOrder(int i);
    }

}
