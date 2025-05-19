using PI_223_1_7.Enums;
using PI_223_1_7.Models;
using Mapping.DTOs;
namespace BLL.Interfaces
{
 
    public interface IBookService
    {
        // Book CRUD operations
        Task<IEnumerable<BookDTO>> GetAllBooksAsync();
        Task<BookDTO> GetBookByIdAsync(int id);
        Task<BookDTO> AddBookAsync(BookDTO book);
        Task UpdateBookAsync(BookDTO book);
        Task DeleteBookAsync(int id);

        // Book filtering and searching
        Task<IEnumerable<BookDTO>> GetBooksByTypeAsync(BookTypes bookType);
        Task<IEnumerable<BookDTO>> GetBooksByGenreAsync(GenreTypes genre);
        Task<IEnumerable<BookDTO>> GetBooksByAuthorAsync(string author);
        Task<IEnumerable<BookDTO>> SearchBooksAsync(string searchTerm);

        // Book sorting
        Task<IEnumerable<BookDTO>> GetBooksSortedByAuthorAsync();
        Task<IEnumerable<BookDTO>> GetBooksSortedByYearAsync();
        Task<IEnumerable<BookDTO>> GetBooksSortedByAvailabilityAsync();

        // Book availability management
        Task<bool> IsBookAvailableAsync(int id);
        Task SetBookAvailabilityAsync(int id, bool isAvailable);

        // Book ordering
        Task<OrderDTO> OrderBookAsync(int bookId, string userId);
        Task<IEnumerable<BookDTO>> GetUserOrderedBooksAsync(string userId);
    }
}