using AutoMapper;
using BLL.Interfaces;
using Mapping.DTOs;
using PI_223_1_7.DbContext;
using PI_223_1_7.Models;
using PI_223_1_7.Patterns.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;

        public OrderService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
        }

        public async Task<OrderDTO> GetSpecificOrder(int i)
        {
            var order = await unitOfWork.orders.GetByIdAsync(i);
            return mapper.Map<OrderDTO>(order);
        }

        public async Task<OrderDTO> CreateOrder(OrderDTO order)
        {
            if(order == null)
                throw new ArgumentNullException(nameof(order));

            await unitOfWork.orders.AddAsync(mapper.Map<Order>(order));
            await unitOfWork.Complete();
            return order;
        }

        public async Task<IEnumerable<OrderDTO>> GetAllWithDetails()
        {
            var order = await unitOfWork.orders.GetAllWithDetailsAsync();
            return mapper.Map<IEnumerable<OrderDTO>>(order);
        }

        public async Task<IEnumerable<OrderDTO>> GetAllWithoutDetails()
        {
            var order = await unitOfWork.orders.GetAllAsync();
            return mapper.Map<IEnumerable<OrderDTO>>(order);
        }

        public async Task DeleteOrderById(int i)
        {
            var order = await unitOfWork.orders.GetByIdAsync(i);

            if (order == null)
                throw new ArgumentException();

            unitOfWork.orders.Delete(order);
            await unitOfWork.orders.SaveAsync();
        }

        public async Task UpdateOrder(OrderDTO order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            var existingOrder = await unitOfWork.orders.GetByIdAsync(order.Id);

            mapper.Map(order, existingOrder);

            unitOfWork.orders.Update(existingOrder);
            await unitOfWork.Complete();
        }
    }
}
