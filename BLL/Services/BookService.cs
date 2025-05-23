using AutoMapper;
using BLL.Exceptions;
using BLL.Interfaces;
using PI_223_1_7.Enums;
using PI_223_1_7.Models;
using PI_223_1_7.Patterns.UnitOfWork;
using Mapping.DTOs;

namespace BLL.Services
{
    public class BookService : IBookService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public BookService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        // CRUD Operations
        public async Task<IEnumerable<BookDTO>> GetAllBooksAsync()
        {
            var books = await _unitOfWork.books.GetAllAsync();
            return _mapper.Map<IEnumerable<BookDTO>>(books);
        }

        public async Task<BookDTO> GetBookByIdAsync(int id)
        {
            var book = await _unitOfWork.books.GetByIdAsync(id);

            if (book == null)
                throw new BookNotFoundException($"Book with ID {id} not found");

            return _mapper.Map<BookDTO>(book);
        }

        public async Task<BookDTO> AddBookAsync(BookDTO bookDto)
        {
            if (bookDto == null)
                throw new ArgumentNullException(nameof(bookDto));

            var book = _mapper.Map<Book>(bookDto);

            await _unitOfWork.books.AddAsync(book);
            await _unitOfWork.books.SaveAsync();

            return _mapper.Map<BookDTO>(book);
        }

        public async Task UpdateBookAsync(BookDTO bookDto)
        {
            if (bookDto == null)
                throw new ArgumentNullException(nameof(bookDto));

            var existingBook = await _unitOfWork.books.GetByIdAsync(bookDto.Id);

            if (existingBook == null)
                throw new BookNotFoundException($"Book with ID {bookDto.Id} not found");

            // Map updated values
            _mapper.Map(bookDto, existingBook);

            _unitOfWork.books.Update(existingBook);
            await _unitOfWork.books.SaveAsync();
        }

        public async Task DeleteBookAsync(int id)
        {
            var book = await _unitOfWork.books.GetByIdAsync(id);

            if (book == null)
                throw new BookNotFoundException($"Book with ID {id} not found");

            // Check if the book has any orders
            var ordersWithBook = await _unitOfWork.orders.FindAsync(o => o.BookId == id);

            if (ordersWithBook.Any())
                throw new BookDeleteException("Cannot delete book with existing orders");

            _unitOfWork.books.Delete(book);
            await _unitOfWork.books.SaveAsync();
        }


        // Filtering and Searching

        public async Task<IEnumerable<BookDTO>> GetBooksByTypeAsync(BookTypes bookType)
        {
            var books = await _unitOfWork.books.FindBookByTypeAsync(bookType);
            return _mapper.Map<IEnumerable<BookDTO>>(books);
        }

        public async Task<IEnumerable<BookDTO>> GetBooksByGenreAsync(GenreTypes genre)
        {
            var books = await _unitOfWork.books.FindBookByGenreAsync(genre);
            return _mapper.Map<IEnumerable<BookDTO>>(books);
        }

        public async Task<IEnumerable<BookDTO>> GetBooksByAuthorAsync(string author)
        {
            if (string.IsNullOrWhiteSpace(author))
                throw new ArgumentException("Author name cannot be empty", nameof(author));

            var books = await _unitOfWork.books.FindAsync(b => b.Author.Contains(author));
            return _mapper.Map<IEnumerable<BookDTO>>(books);
        }

        public async Task<IEnumerable<BookDTO>> SearchBooksAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllBooksAsync();

            var books = await _unitOfWork.books.FindAsync(b =>
                b.Name.Contains(searchTerm) ||
                b.Author.Contains(searchTerm) ||
                b.Description.Contains(searchTerm));

            return _mapper.Map<IEnumerable<BookDTO>>(books);
        }


       // Sorting
        public async Task<IEnumerable<BookDTO>> GetBooksSortedByAuthorAsync()
        {
            var books = await _unitOfWork.books.FilterBookByAuthorAsync();
            return _mapper.Map<IEnumerable<BookDTO>>(books);
        }

        public async Task<IEnumerable<BookDTO>> GetBooksSortedByYearAsync()
        {
            var books = await _unitOfWork.books.FilterBookByYearAsync();
            return _mapper.Map<IEnumerable<BookDTO>>(books);
        }

        public async Task<IEnumerable<BookDTO>> GetBooksSortedByAvailabilityAsync()
        {
            var books = await _unitOfWork.books.FilterBookByAvailability();
            return _mapper.Map<IEnumerable<BookDTO>>(books);
        }


       // Availability Management
        public async Task<bool> IsBookAvailableAsync(int id)
        {
            var book = await _unitOfWork.books.GetByIdAsync(id);

            if (book == null)
                throw new BookNotFoundException($"Book with ID {id} not found");

            return book.IsAvaliable;
        }

        public async Task SetBookAvailabilityAsync(int id, bool isAvailable)
        {
            var book = await _unitOfWork.books.GetByIdAsync(id);

            if (book == null)
                throw new BookNotFoundException($"Book with ID {id} not found");

            book.IsAvaliable = isAvailable;
            _unitOfWork.books.Update(book);
            await _unitOfWork.books.SaveAsync();
        }


        // Ordering
        public async Task<OrderDTO> OrderBookAsync(int bookId, string userId)
        {
            var book = await _unitOfWork.books.GetByIdAsync(bookId);

            if (book == null)
                throw new BookNotFoundException($"Book with ID {bookId} not found");

            if (!book.IsAvaliable)
                throw new BookNotAvailableException($"Book with ID {bookId} is not available for ordering");

            var order = new Order
            {
                BookId = bookId,
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                Type = OrderStatusTypes.Pending,
                Book = book
            };

            // Update book availability
            book.IsAvaliable = false;
            _unitOfWork.books.Update(book);

            // Add the order
            await _unitOfWork.orders.AddAsync(order);
            await _unitOfWork.orders.SaveAsync();

            return _mapper.Map<OrderDTO>(order);
        }

        public async Task<IEnumerable<BookDTO>> GetUserOrderedBooksAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

            // Get all orders with details for the specific user
            var orders = await _unitOfWork.orders.GetAllWithDetailsAsync();
            var userOrders = orders.Where(o => o.UserId == userId);

            // Extract books from the orders
            var orderedBooks = userOrders.Select(o => o.Book).ToList();

            return _mapper.Map<IEnumerable<BookDTO>>(orderedBooks);
        }
    }
}