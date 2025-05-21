using Microsoft.EntityFrameworkCore;
using PI_223_1_7.DbContext;
using PI_223_1_7.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PI_223_1_7.Patterns.Repository
{
    
    public interface IOrderRepository : IRepository<Order>
    {
        Task<IEnumerable<Order>> GetAllWithDetailsAsync();
    }

    public class OrderRepository : Repository<Order>, IOrderRepository
    {
        private readonly LibraryDbContext libraryDbContext;

        public OrderRepository(LibraryDbContext context) : base(context)
        {
            libraryDbContext = context;
        }

        public async Task<IEnumerable<Order>> GetAllWithDetailsAsync()
        {
            return await libraryDbContext.Orders.Include(b => b.Book).ToListAsync();
        }

        public async override Task<Order> GetByIdAsync(int id)
        {
            return await libraryDbContext.Orders.Include(b => b.Book).FirstAsync(i => i.Id == id);
        }
    }
}
