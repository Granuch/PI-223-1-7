using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PI_223_1_7.Models;
using BLL.Interfaces;
using Mapping.DTOs;
using PI_223_1_7.Enums;
using BLL.Exceptions;
using Microsoft.Extensions.Logging;

namespace PL.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    public class BooksController : ControllerBase
    {
        private readonly IBookService _bookService;
        private readonly ILogger<BooksController> _logger;

        public BooksController(IBookService bookService, ILogger<BooksController> logger)
        {
            _bookService = bookService ?? throw new ArgumentNullException(nameof(bookService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Отримує всі книги з можливістю пошуку, фільтрації та сортування
        /// <response code="200">Успішне повернення списку книг</response>
        /// <response code="500">Помилка на сервері</response>
        [HttpGet("GetAll")]
        [ProducesResponseType(typeof(IEnumerable<BookDTO>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<BookDTO>>> GetBooks(
            [FromQuery] string sortOrder = null,
            [FromQuery] string searchString = null,
            [FromQuery] GenreTypes? genre = null,
            [FromQuery] BookTypes? type = null)
        {
            try
            {
                IEnumerable<BookDTO> books;

                // Перевіряємо пошук та фільтри
                if (!string.IsNullOrEmpty(searchString))
                {
                    books = await _bookService.SearchBooksAsync(searchString);
                }
                else if (genre.HasValue)
                {
                    books = await _bookService.GetBooksByGenreAsync(genre.Value);
                }
                else if (type.HasValue)
                {
                    books = await _bookService.GetBooksByTypeAsync(type.Value);
                }
                else
                {
                    books = await _bookService.GetAllBooksAsync();
                }

                // Сортування
                if (!string.IsNullOrEmpty(sortOrder))
                {
                    switch (sortOrder.ToLower())
                    {
                        case "author":
                            books = await _bookService.GetBooksSortedByAuthorAsync();
                            break;
                        case "year":
                            books = await _bookService.GetBooksSortedByYearAsync();
                            break;
                        case "availability":
                            books = await _bookService.GetBooksSortedByAvailabilityAsync();
                            break;
                    }
                }
                return Ok(books);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving books");
                return StatusCode(500, new { message = $"Внутрішня помилка сервера: {ex.Message}" });
            }
        }

        // Отримує деталі книги за ідентифікатором
        /// <response code="200">Книга знайдена</response>
        /// <response code="404">Книга не знайдена</response>
        /// <response code="500">Помилка на сервері</response>
        [HttpGet("GetById/{id}")]
        [ProducesResponseType(typeof(BookDTO), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<BookDTO>> GetBook(int id)
        {
            try
            {
                var book = await _bookService.GetBookByIdAsync(id);
                return Ok(book);
            }
            catch (BookNotFoundException ex)
            {
                _logger.LogWarning(ex, $"Book not found: {id}");
                return NotFound(new { message = $"Книга з ID {id} не знайдена" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving book with ID: {id}");
                return StatusCode(500, new { message = $"Внутрішня помилка сервера: {ex.Message}" });
            }
        }

        // Створює нову книгу
        /// <response code="201">Книга успішно створена</response>
        /// <response code="400">Неправильні дані у запиті</response>
        /// <response code="401">Користувач не авторизований</response>
        /// <response code="403">Доступ заборонено</response>
        /// <response code="500">Помилка на сервері</response>
        [HttpPost("CreateBook")]
        //[Authorize(Roles = "Manager,Administrator")]
        [ProducesResponseType(typeof(BookDTO), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<BookDTO>> CreateBook([FromBody] BookDTO book)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state when creating book");
                return BadRequest(ModelState);
            }

            try
            {
                book.IsAvailable = true; // За замовчуванням нова книга доступна
                var createdBook = await _bookService.AddBookAsync(book);
                _logger.LogInformation($"Book created successfully: {createdBook.Id}");
                return CreatedAtAction(nameof(GetBook), new { id = createdBook.Id }, createdBook);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating book");
                return StatusCode(500, new { message = $"Внутрішня помилка сервера: {ex.Message}" });
            }
        }

        // Оновлює існуючу книгу
        /// <response code="204">Книга успішно оновлена</response>
        /// <response code="400">Неправильні дані у запиті</response>
        /// <response code="401">Користувач не авторизований</response>
        /// <response code="403">Доступ заборонено</response>
        /// <response code="404">Книга не знайдена</response>
        /// <response code="500">Помилка на сервері</response>
        [HttpPut("UpdateBook/{id:int}")]
        //[Authorize(Roles = "Manager,Administrator")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateBook(int id, [FromBody] BookDTO book)
        {
            if (id != book.Id)
            {
                _logger.LogWarning($"Mismatch between URL ID: {id} and body ID: {book.Id}");
                return BadRequest(new { message = "Ідентифікатор книги в URL повинен співпадати з ідентифікатором в тілі запиту" });
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state when updating book");
                return BadRequest(ModelState);
            }

            try
            {
                await _bookService.UpdateBookAsync(book);
                _logger.LogInformation($"Book successfully updated: {id}");
                return NoContent();
            }
            catch (BookNotFoundException ex)
            {
                _logger.LogWarning(ex, $"Book not found: {id}");
                return NotFound(new { message = $"Книга з ID {id} не знайдена" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating book with ID: {id}");
                return StatusCode(500, new { message = $"Внутрішня помилка сервера: {ex.Message}" });
            }
        }

        // Видаляє книгу за ідентифікатором
        /// <response code="204">Книга успішно видалена</response>
        /// <response code="400">Книга не може бути видалена</response>
        /// <response code="401">Користувач не авторизований</response>
        /// <response code="403">Доступ заборонено</response>
        /// <response code="404">Книга не знайдена</response>
        /// <response code="500">Помилка на сервері</response>
        [HttpDelete("DeleteBook/{id}")]
        //[Authorize(Roles = "Administrator")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteBook(int id)
        {
            try
            {
                await _bookService.DeleteBookAsync(id);
                _logger.LogInformation($"Book successfully deleted: {id}");
                return NoContent();
            }
            catch (BookNotFoundException ex)
            {
                _logger.LogWarning(ex, $"Book not found: {id}");
                return NotFound(new { message = $"Книга з ID {id} не знайдена" });
            }
            catch (BookDeleteException ex)
            {
                _logger.LogWarning(ex, $"Cannot delete book: {id}. {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting book with ID: {id}");
                return StatusCode(500, new { message = $"Внутрішня помилка сервера: {ex.Message}" });
            }
        }

        // Замовляє книгу для поточного користувача
        /// <response code="200">Книга успішно замовлена</response>
        /// <response code="400">Книга недоступна для замовлення</response>
        /// <response code="401">Користувач не авторизований</response>
        /// <response code="403">Доступ заборонено</response>
        /// <response code="404">Книга не знайдена</response>
        /// <response code="500">Помилка на сервері</response>
        [HttpPost("OrderBook/{id:int}")]
        //[Authorize(Roles = "RegisteredUser,Manager,Administrator")]
        [ProducesResponseType(typeof(OrderDTO), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<OrderDTO>> OrderBook(int id)
        {
            try
            {
                // Для тестування використовуємо фіксований ID користувача
                string userId = "1"; // Припускаємо, що користувач з ID=1 існує в базі даних

                // Коментуємо оригінальний код отримання користувача
                //var user = await _userManager.GetUserAsync(User);
                //if (user == null)
                //{
                //    return Unauthorized(new { message = "Користувач не авторизований" });
                //}

                // Використовуємо фіксований ID замість user.Id
                var order = await _bookService.OrderBookAsync(id, userId);
                _logger.LogInformation($"Book ordered successfully: Book ID: {id}, User ID: {userId}");
                return Ok(order);
            }
            catch (BookNotFoundException ex)
            {
                _logger.LogWarning(ex, $"Book not found: {id}");
                return NotFound(new { message = $"Книга з ID {id} не знайдена" });
            }
            catch (BookNotAvailableException ex)
            {
                _logger.LogWarning(ex, $"Book not available: {id}");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error ordering book with ID: {id}");
                return StatusCode(500, new { message = $"Внутрішня помилка сервера: {ex.Message}" });
            }
        }

        // Отримує всі книги, замовлені поточним користувачем
        /// <response code="200">Список успішно отриманий</response>
        /// <response code="401">Користувач не авторизований</response>
        /// <response code="403">Доступ заборонено</response>
        /// <response code="500">Помилка на сервері</response>
        [HttpGet("MyOrders")]
        //[Authorize(Roles = "RegisteredUser,Manager,Administrator")]
        [ProducesResponseType(typeof(IEnumerable<BookDTO>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<BookDTO>>> GetMyOrders()
        {
            try
            {
                // Для тестування використовуємо фіксований ID користувача
                string userId = "1"; // Припускаємо, що користувач з ID=1 існує в базі даних

                //var user = await _userManager.GetUserAsync(User);
                //if (user == null)
                //{
                //    return Unauthorized(new { message = "Користувач не авторизований" });
                //}

                // Використовуємо фіксований userId замість user.Id
                // Також прибрав параметр id, якого немає в оригінальному методі
                var books = await _bookService.GetUserOrderedBooksAsync(userId);
                _logger.LogInformation($"Retrieved orders for user: {userId}");
                return Ok(books);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user's ordered books");
                return StatusCode(500, new { message = $"Внутрішня помилка сервера: {ex.Message}" });
            }
        }

        // Отримує всі книги за автором
        /// <response code="200">Успішне повернення списку книг</response>
        /// <response code="400">Неправильний параметр автора</response>
        /// <response code="500">Помилка на сервері</response>
        [HttpGet("byauthor/{author}")]
        [ProducesResponseType(typeof(IEnumerable<BookDTO>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<BookDTO>>> GetBooksByAuthor(string author)
        {
            if (string.IsNullOrWhiteSpace(author))
            {
                _logger.LogWarning("Empty author parameter provided");
                return BadRequest(new { message = "Ім'я автора не може бути порожнім" });
            }

            try
            {
                var books = await _bookService.GetBooksByAuthorAsync(author);
                _logger.LogInformation($"Retrieved books by author: {author}");
                return Ok(books);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving books by author: {author}");
                return StatusCode(500, new { message = $"Внутрішня помилка сервера: {ex.Message}" });
            }
        }

        // Перевіряє доступність книги
        /// <response code="200">Статус доступності успішно отриманий</response>
        /// <response code="404">Книга не знайдена</response>
        /// <response code="500">Помилка на сервері</response>
        [HttpGet("availability/{id}")]
        [ProducesResponseType(typeof(bool), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<bool>> IsBookAvailable(int id)
        {
            try
            {
                var isAvailable = await _bookService.IsBookAvailableAsync(id);
                _logger.LogInformation($"Checked availability for book: {id}, result: {isAvailable}");
                return Ok(isAvailable);
            }
            catch (BookNotFoundException ex)
            {
                _logger.LogWarning(ex, $"Book not found: {id}");
                return NotFound(new { message = $"Книга з ID {id} не знайдена" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking availability for book with ID: {id}");
                return StatusCode(500, new { message = $"Внутрішня помилка сервера: {ex.Message}" });
            }
        }

        // Встановлює доступність книги
        /// <response code="204">Статус доступності успішно оновлений</response>
        /// <response code="400">Неправильні дані у запиті</response>
        /// <response code="401">Користувач не авторизований</response>
        /// <response code="403">Доступ заборонено</response>
        /// <response code="404">Книга не знайдена</response>
        /// <response code="500">Помилка на сервері</response>
        [HttpPut("SetAvailability/{id}")]
        //[Authorize(Roles = "Manager,Administrator")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> SetBookAvailability(int id, [FromBody] bool isAvailable)
        {
            try
            {
                await _bookService.SetBookAvailabilityAsync(id, isAvailable);
                _logger.LogInformation($"Book availability updated: {id}, set to: {isAvailable}");
                return NoContent();
            }
            catch (BookNotFoundException ex)
            {
                _logger.LogWarning(ex, $"Book not found: {id}");
                return NotFound(new { message = $"Книга з ID {id} не знайдена" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating availability for book with ID: {id}");
                return StatusCode(500, new { message = $"Внутрішня помилка сервера: {ex.Message}" });
            }
        }
    }
}