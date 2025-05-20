using AutoMapper;
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

    public interface IOrderService
    {
        public Task<Order> CreateOrder(Order order);
        public Task<IEnumerable<Order>> GetAllWithDetails();
        public Task<IEnumerable<Order>> GetAllWithoutDetails();
    }

    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;

        public OrderService(UnitOfWork unitOfWork, IMapper mapper)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
        }

        public async Task<Order> CreateOrder(Order order)
        {
            await unitOfWork.orders.AddAsync(order);
            await unitOfWork.Complete();
            return order;
        }

        public async Task<IEnumerable<Order>> GetAllWithDetails()
        {
            return await unitOfWork.orders.GetAllWithDetailsAsync();
        }

        public async Task<IEnumerable<Order>> GetAllWithoutDetails()
        {
            return await unitOfWork.orders.GetAllAsync();
        }
    }
}
