using Microsoft.EntityFrameworkCore;
using PI_223_1_7.DbContext;
using PI_223_1_7.Enums;
using PI_223_1_7.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PI_223_1_7.Patterns.Repository
{
    public interface IBookRepository : IRepository<Book>
    {
        Task<IEnumerable<Book>> FindBookByTypeAsync(BookTypes bookType);
        Task<IEnumerable<Book>> FindBookByGenreAsync(GenreTypes genre);
        Task<IEnumerable<Book>> FilterBookByAuthorAsync();
        Task<IEnumerable<Book>> FilterBookByYearAsync();
        Task<IEnumerable<Book>> FilterBookByAvailability();
    }

    public class BookRepository : Repository<Book>, IBookRepository
    {
        private readonly LibraryDbContext libraryContext;

        public BookRepository(LibraryDbContext context) : base(context)
        {
            libraryContext = context;
        }

        public async Task<IEnumerable<Book>> FindBookByTypeAsync(BookTypes bookType)
        {
            return await FindAsync(b => b.Type == bookType);
        }

        public async Task<IEnumerable<Book>> FindBookByGenreAsync(GenreTypes genre)
        {
            return await FindAsync(g => g.Genre == genre);
        }

        public async Task<IEnumerable<Book>> FilterBookByAuthorAsync()
        {
            return await libraryContext.Books.OrderBy(a => a.Author).ToListAsync();
        }

        public async Task<IEnumerable<Book>> FilterBookByYearAsync()
        {
            return await libraryContext.Books.OrderBy(y =>  y.Year).ToListAsync();
        }

        public async Task<IEnumerable<Book>> FilterBookByAvailability()
        {
            return await libraryContext.Books.OrderBy(b => b.IsAvaliable).ToListAsync();
        }
    }   
}
