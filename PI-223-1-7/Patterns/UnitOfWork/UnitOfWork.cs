using PI_223_1_7.DbContext;
using PI_223_1_7.Models;
using PI_223_1_7.Patterns.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PI_223_1_7.Patterns.UnitOfWork
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        public IOrderRepository orders { get; set; }
        public IBookRepository books { get; set; }
        public IRepository<ApplicationUser> users { get; set; }
    }

    public class UnitOfWork : IUnitOfWork
    {
        private readonly LibraryDbContext _libraryDbContext;
        public IOrderRepository orders {  set; get; }
        public IBookRepository books {  set; get; }
        public IRepository<ApplicationUser> users {  set; get; }

        public UnitOfWork(LibraryDbContext libraryDbContext)
        {
            _libraryDbContext = libraryDbContext;
            orders = new OrderRepository(_libraryDbContext);
            books = new BookRepository(_libraryDbContext);
            users = new Repository<ApplicationUser>(_libraryDbContext);
        }

        public async Task<int> Complete()
        {
            return await _libraryDbContext.SaveChangesAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await _libraryDbContext.DisposeAsync();
        }
    }
}
