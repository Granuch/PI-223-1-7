using AutoMapper;
using BLL.Exceptions;
using BLL.Services;
using Tests.Mocks;
using Tests.TestHelpers;
using Mapping.DTOs;
using Moq;
using PI_223_1_7.Enums;
using PI_223_1_7.Models;

namespace Tests.Services
{
    [TestFixture]
    public class BookServiceTests
    {
        private Mocks.Mocks _mockUnitOfWork;
        private IMapper _mapper;
        private BookService _bookService;

        [SetUp]
        public void Setup()
        {
            _mockUnitOfWork = new Mocks.Mocks();
            _mapper = MapperHelper.CreateMapper();
            _bookService = new BookService(_mockUnitOfWork.UnitOfWorkMock.Object, _mapper);
        }

        // CRUD Operation Tests

        [Test]
        public async Task GetAllBooksAsync_ReturnsAllBooks()
        {
            // Arrange
            var books = TestDataFactory.CreateBookList();
            _mockUnitOfWork.BookRepositoryMock
                .Setup(repo => repo.GetAllAsync())
                .ReturnsAsync(books);

            // Act
            var result = await _bookService.GetAllBooksAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(books.Count));
        }

        [Test]
        public async Task GetBookByIdAsync_ExistingId_ReturnsBook()
        {
            // Arrange
            var bookId = 1;
            var book = TestDataFactory.CreateBook(bookId);
            _mockUnitOfWork.BookRepositoryMock
                .Setup(repo => repo.GetByIdAsync(bookId))
                .ReturnsAsync(book);

            // Act
            var result = await _bookService.GetBookByIdAsync(bookId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(bookId));
            Assert.That(result.Name, Is.EqualTo(book.Name));
        }

        [Test]
        public void GetBookByIdAsync_NonExistingId_ThrowsBookNotFoundException()
        {
            // Arrange
            var nonExistingId = 999;
            _mockUnitOfWork.BookRepositoryMock
                .Setup(repo => repo.GetByIdAsync(nonExistingId))
                .ReturnsAsync((Book)null);

            // Act & Assert
            Assert.ThrowsAsync<BookNotFoundException>(async () =>
                await _bookService.GetBookByIdAsync(nonExistingId));
        }

        [Test]
        public async Task AddBookAsync_ValidBook_ReturnsAddedBook()
        {
            // Arrange
            var bookDto = TestDataFactory.CreateBookDTO();
            var book = _mapper.Map<Book>(bookDto);

            _mockUnitOfWork.BookRepositoryMock
                .Setup(repo => repo.AddAsync(It.IsAny<Book>()))
                .Returns(Task.CompletedTask)
                .Callback<Book>(addedBook => addedBook.Id = 1);

            // Act
            var result = await _bookService.AddBookAsync(bookDto);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(1));
            Assert.That(result.Name, Is.EqualTo(bookDto.Name));
            _mockUnitOfWork.BookRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Book>()), Times.Once);
            _mockUnitOfWork.BookRepositoryMock.Verify(repo => repo.SaveAsync(), Times.Once);
        }

        [Test]
        public void AddBookAsync_NullBook_ThrowsArgumentNullException()
        {
            // Arrange
            BookDTO nullBook = null;

            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _bookService.AddBookAsync(nullBook));
        }

        [Test]
        public async Task UpdateBookAsync_ExistingBook_UpdatesBook()
        {
            // Arrange
            var bookId = 1;
            var existingBook = TestDataFactory.CreateBook(bookId);
            var updatedBookDto = TestDataFactory.CreateBookDTO(bookId);
            updatedBookDto.Name = "Updated Book Name";

            _mockUnitOfWork.BookRepositoryMock
                .Setup(repo => repo.GetByIdAsync(bookId))
                .ReturnsAsync(existingBook);

            // Act
            await _bookService.UpdateBookAsync(updatedBookDto);

            // Assert
            _mockUnitOfWork.BookRepositoryMock.Verify(repo => repo.Update(It.IsAny<Book>()), Times.Once);
            _mockUnitOfWork.BookRepositoryMock.Verify(repo => repo.SaveAsync(), Times.Once);
        }

        [Test]
        public void UpdateBookAsync_NonExistingBook_ThrowsBookNotFoundException()
        {
            // Arrange
            var nonExistingId = 999;
            var bookDto = TestDataFactory.CreateBookDTO(nonExistingId);

            _mockUnitOfWork.BookRepositoryMock
                .Setup(repo => repo.GetByIdAsync(nonExistingId))
                .ReturnsAsync((Book)null);

            // Act & Assert
            Assert.ThrowsAsync<BookNotFoundException>(async () =>
                await _bookService.UpdateBookAsync(bookDto));
        }

        [Test]
        public async Task DeleteBookAsync_ExistingBookWithNoOrders_DeletesBook()
        {
            // Arrange
            var bookId = 1;
            var book = TestDataFactory.CreateBook(bookId);

            _mockUnitOfWork.BookRepositoryMock
                .Setup(repo => repo.GetByIdAsync(bookId))
                .ReturnsAsync(book);

            _mockUnitOfWork.OrderRepositoryMock
                .Setup(repo => repo.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>()))
                .ReturnsAsync(new List<Order>());

            // Act
            await _bookService.DeleteBookAsync(bookId);

            // Assert
            _mockUnitOfWork.BookRepositoryMock.Verify(repo => repo.Delete(book), Times.Once);
            _mockUnitOfWork.BookRepositoryMock.Verify(repo => repo.SaveAsync(), Times.Once);
        }

        [Test]
        public void DeleteBookAsync_BookWithOrders_ThrowsBookDeleteException()
        {
            // Arrange
            var bookId = 1;
            var book = TestDataFactory.CreateBook(bookId);
            var orders = new List<Order> { TestDataFactory.CreateOrder(1, bookId) };

            _mockUnitOfWork.BookRepositoryMock
                .Setup(repo => repo.GetByIdAsync(bookId))
                .ReturnsAsync(book);

            _mockUnitOfWork.OrderRepositoryMock
                .Setup(repo => repo.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>()))
                .ReturnsAsync(orders);

            // Act & Assert
            Assert.ThrowsAsync<BookDeleteException>(async () =>
                await _bookService.DeleteBookAsync(bookId));
        }

        // Filtering and Searching Tests

        [Test]
        public async Task GetBooksByTypeAsync_ReturnsFilteredBooks()
        {
            // Arrange
            var bookType = BookTypes.Paper;
            var books = TestDataFactory.CreateBookList().Where(b => b.Type == bookType).ToList();

            _mockUnitOfWork.BookRepositoryMock
                .Setup(repo => repo.FindBookByTypeAsync(bookType))
                .ReturnsAsync(books);

            // Act
            var result = await _bookService.GetBooksByTypeAsync(bookType);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(books.Count));
        }

        [Test]
        public async Task GetBooksByGenreAsync_ReturnsFilteredBooks()
        {
            // Arrange
            var genre = GenreTypes.NonFiction;
            var books = TestDataFactory.CreateBookList().Where(b => b.Genre == genre).ToList();

            _mockUnitOfWork.BookRepositoryMock
                .Setup(repo => repo.FindBookByGenreAsync(genre))
                .ReturnsAsync(books);

            // Act
            var result = await _bookService.GetBooksByGenreAsync(genre);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(books.Count));
        }

        [Test]
        public async Task GetBooksByAuthorAsync_ReturnsFilteredBooks()
        {
            // Arrange
            var author = "Test Author";
            var books = TestDataFactory.CreateBookList().Where(b => b.Author.Contains(author)).ToList();

            _mockUnitOfWork.BookRepositoryMock
                .Setup(repo => repo.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Book, bool>>>()))
                .ReturnsAsync(books);

            // Act
            var result = await _bookService.GetBooksByAuthorAsync(author);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(books.Count));
        }

        [Test]
        public void GetBooksByAuthorAsync_EmptyAuthor_ThrowsArgumentException()
        {
            // Arrange
            string emptyAuthor = "";

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await _bookService.GetBooksByAuthorAsync(emptyAuthor));
        }

        [Test]
        public async Task SearchBooksAsync_ValidTerm_ReturnsMatchingBooks()
        {
            // Arrange
            var searchTerm = "Test";
            var books = TestDataFactory.CreateBookList();

            _mockUnitOfWork.BookRepositoryMock
                .Setup(repo => repo.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Book, bool>>>()))
                .ReturnsAsync(books);

            // Act
            var result = await _bookService.SearchBooksAsync(searchTerm);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(books.Count));
        }

        [Test]
        public async Task SearchBooksAsync_EmptyTerm_ReturnsAllBooks()
        {
            // Arrange
            var emptyTerm = "";
            var books = TestDataFactory.CreateBookList();

            _mockUnitOfWork.BookRepositoryMock
                .Setup(repo => repo.GetAllAsync())
                .ReturnsAsync(books);

            // Act
            var result = await _bookService.SearchBooksAsync(emptyTerm);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(books.Count));
        }

        // Sorting Tests

        [Test]
        public async Task GetBooksSortedByAuthorAsync_ReturnsSortedBooks()
        {
            // Arrange
            var books = TestDataFactory.CreateBookList().OrderBy(b => b.Author).ToList();

            _mockUnitOfWork.BookRepositoryMock
                .Setup(repo => repo.FilterBookByAuthorAsync())
                .ReturnsAsync(books);

            // Act
            var result = await _bookService.GetBooksSortedByAuthorAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(books.Count));
        }

        [Test]
        public async Task GetBooksSortedByYearAsync_ReturnsSortedBooks()
        {
            // Arrange
            var books = TestDataFactory.CreateBookList().OrderBy(b => b.Year).ToList();

            _mockUnitOfWork.BookRepositoryMock
                .Setup(repo => repo.FilterBookByYearAsync())
                .ReturnsAsync(books);

            // Act
            var result = await _bookService.GetBooksSortedByYearAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(books.Count));
            Assert.That(result.First().Year, Is.LessThanOrEqualTo(result.Last().Year));

        }

        [Test]
        public async Task GetBooksSortedByAvailabilityAsync_ReturnsSortedBooks()
        {
            // Arrange
            var books = TestDataFactory.CreateBookList().OrderBy(b => b.IsAvaliable).ToList();

            _mockUnitOfWork.BookRepositoryMock
                .Setup(repo => repo.FilterBookByAvailability())
                .ReturnsAsync(books);

            // Act
            var result = await _bookService.GetBooksSortedByAvailabilityAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(books.Count));
        }

        // Availability Management Tests

        [Test]
        public async Task IsBookAvailableAsync_AvailableBook_ReturnsTrue()
        {
            // Arrange
            var bookId = 1;
            var book = TestDataFactory.CreateBook(bookId, true);

            _mockUnitOfWork.BookRepositoryMock
                .Setup(repo => repo.GetByIdAsync(bookId))
                .ReturnsAsync(book);

            // Act
            var result = await _bookService.IsBookAvailableAsync(bookId);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task IsBookAvailableAsync_UnavailableBook_ReturnsFalse()
        {
            // Arrange
            var bookId = 1;
            var book = TestDataFactory.CreateBook(bookId, false);

            _mockUnitOfWork.BookRepositoryMock
                .Setup(repo => repo.GetByIdAsync(bookId))
                .ReturnsAsync(book);

            // Act
            var result = await _bookService.IsBookAvailableAsync(bookId);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task SetBookAvailabilityAsync_UpdatesAvailability()
        {
            // Arrange
            var bookId = 1;
            var book = TestDataFactory.CreateBook(bookId, false);

            _mockUnitOfWork.BookRepositoryMock
                .Setup(repo => repo.GetByIdAsync(bookId))
                .ReturnsAsync(book);

            // Act
            await _bookService.SetBookAvailabilityAsync(bookId, true);

            // Assert
            Assert.That(book.IsAvaliable, Is.True);
            _mockUnitOfWork.BookRepositoryMock.Verify(repo => repo.Update(book), Times.Once);
            _mockUnitOfWork.BookRepositoryMock.Verify(repo => repo.SaveAsync(), Times.Once);
        }

        // Ordering Tests

        [Test]
        public async Task OrderBookAsync_AvailableBook_CreatesOrder()
        {
            // Arrange
            var bookId = 1;
            var userId = "user1";
            var book = TestDataFactory.CreateBook(bookId, true);

            _mockUnitOfWork.BookRepositoryMock
                .Setup(repo => repo.GetByIdAsync(bookId))
                .ReturnsAsync(book);

            // Act
            var result = await _bookService.OrderBookAsync(bookId, userId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.BookId, Is.EqualTo(bookId));
            Assert.That(result.UserId, Is.EqualTo(userId));
            Assert.That(book.IsAvaliable, Is.False);

            _mockUnitOfWork.BookRepositoryMock.Verify(repo => repo.Update(book), Times.Once);
            _mockUnitOfWork.OrderRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Order>()), Times.Once);
            _mockUnitOfWork.OrderRepositoryMock.Verify(repo => repo.SaveAsync(), Times.Once);
        }

        [Test]
        public void OrderBookAsync_UnavailableBook_ThrowsBookNotAvailableException()
        {
            // Arrange
            var bookId = 1;
            var userId = "user1";
            var book = TestDataFactory.CreateBook(bookId, false);

            _mockUnitOfWork.BookRepositoryMock
                .Setup(repo => repo.GetByIdAsync(bookId))
                .ReturnsAsync(book);

            // Act & Assert
            Assert.ThrowsAsync<BookNotAvailableException>(async () =>
                await _bookService.OrderBookAsync(bookId, userId));
        }

        [Test]
        public async Task GetUserOrderedBooksAsync_ReturnsUserBooks()
        {
            // Arrange
            var userId = "user1";
            var orders = TestDataFactory.CreateOrderList().Where(o => o.UserId == userId).ToList();

            _mockUnitOfWork.OrderRepositoryMock
                .Setup(repo => repo.GetAllWithDetailsAsync())
                .ReturnsAsync(orders);

            // Act
            var result = await _bookService.GetUserOrderedBooksAsync(userId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(orders.Count));
        }

        [Test]
        public void GetUserOrderedBooksAsync_EmptyUserId_ThrowsArgumentException()
        {
            // Arrange
            string emptyUserId = "";

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await _bookService.GetUserOrderedBooksAsync(emptyUserId));
        }
    }
}