﻿using Microsoft.AspNetCore.Authorization;
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
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class BooksController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IBookService _bookService;
        private readonly ILogger<BooksController> _logger;

        public BooksController(UserManager<ApplicationUser> userManager, IBookService bookService, ILogger<BooksController> logger)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _bookService = bookService ?? throw new ArgumentNullException(nameof(bookService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Отримує всі книги без фільтрації
        /// </summary>
        /// <response code="200">Успішне повернення списку всіх книг</response>
        /// <response code="500">Помилка на сервері</response>
        [HttpGet("GetAll")]
        [ProducesResponseType(typeof(IEnumerable<BookDTO>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<BookDTO>>> GetAllBooks()
        {
            try
            {
                var books = await _bookService.GetAllBooksAsync();
                _logger.LogInformation("Retrieved all books successfully");
                return Ok(books);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all books");
                return StatusCode(500, new { message = $"Внутрішня помилка сервера: {ex.Message}" });
            }
        }

        /// <summary>
        /// Отримує книги з фільтрацією за різними параметрами
        /// </summary>
        /// <response code="200">Успішне повернення списку книг</response>
        /// <response code="500">Помилка на сервері</response>
        [HttpGet("Filter")]
        [ProducesResponseType(typeof(IEnumerable<BookDTO>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<BookDTO>>> GetBooksWithFiltering(
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
                    _logger.LogInformation($"Searching books with query: {searchString}");
                }
                else if (genre.HasValue)
                {
                    books = await _bookService.GetBooksByGenreAsync(genre.Value);
                    _logger.LogInformation($"Filtering books by genre: {genre.Value}");
                }
                else if (type.HasValue)
                {
                    books = await _bookService.GetBooksByTypeAsync(type.Value);
                    _logger.LogInformation($"Filtering books by type: {type.Value}");
                }
                else
                {
                    books = await _bookService.GetAllBooksAsync();
                    _logger.LogInformation("Getting all books (no filters applied)");
                }

                // Сортування
                if (!string.IsNullOrEmpty(sortOrder))
                {
                    switch (sortOrder.ToLower())
                    {
                        case "author":
                            books = await _bookService.GetBooksSortedByAuthorAsync();
                            _logger.LogInformation("Sorting books by author");
                            break;
                        case "year":
                            books = await _bookService.GetBooksSortedByYearAsync();
                            _logger.LogInformation("Sorting books by year");
                            break;
                        case "availability":
                            books = await _bookService.GetBooksSortedByAvailabilityAsync();
                            _logger.LogInformation("Sorting books by availability");
                            break;
                    }
                }

                return Ok(books);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving books with filtering");
                return StatusCode(500, new { message = $"Внутрішня помилка сервера: {ex.Message}" });
            }
        }

        /// <summary>
        /// Отримує книгу за ідентифікатором
        /// </summary>
        /// <response code="200">Книга знайдена</response>
        /// <response code="404">Книга не знайдена</response>
        /// <response code="500">Помилка на сервері</response>
        [HttpGet("GetById/{id}")]
        [ProducesResponseType(typeof(BookDTO), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<BookDTO>> GetBookById(int id)
        {
            try
            {
                var book = await _bookService.GetBookByIdAsync(id);
                _logger.LogInformation($"Retrieved book with ID: {id}");
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

        /// <summary>
        /// Створює нову книгу
        /// </summary>
        /// <response code="201">Книга успішно створена</response>
        /// <response code="400">Неправильні дані у запиті</response>
        /// <response code="401">Користувач не авторизований</response>
        /// <response code="403">Доступ заборонено</response>
        /// <response code="500">Помилка на сервері</response>
        [HttpPost("Create")]
        [Authorize(Roles = "Manager,Administrator")]
        [ProducesResponseType(typeof(BookDTO), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<BookDTO>> CreateNewBook([FromBody] BookDTO book)
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
                return CreatedAtAction(nameof(GetBookById), new { id = createdBook.Id }, createdBook);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating book");
                return StatusCode(500, new { message = $"Внутрішня помилка сервера: {ex.Message}" });
            }
        }

        /// <summary>
        /// Оновлює існуючу книгу
        /// </summary>
        /// <response code="204">Книга успішно оновлена</response>
        /// <response code="400">Неправильні дані у запиті</response>
        /// <response code="401">Користувач не авторизований</response>
        /// <response code="403">Доступ заборонено</response>
        /// <response code="404">Книга не знайдена</response>
        /// <response code="500">Помилка на сервері</response>
        [HttpPut("Update/{id}")]
        [Authorize(Roles = "Manager,Administrator")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateExistingBook(int id, [FromBody] BookDTO book)
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

        /// <summary>
        /// Видаляє книгу за ідентифікатором
        /// </summary>
        /// <response code="204">Книга успішно видалена</response>
        /// <response code="400">Книга не може бути видалена</response>
        /// <response code="401">Користувач не авторизований</response>
        /// <response code="403">Доступ заборонено</response>
        /// <response code="404">Книга не знайдена</response>
        /// <response code="500">Помилка на сервері</response>
        [HttpDelete("Delete/{id}")]
        [Authorize(Roles = "Administrator")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteBookById(int id)
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


        /// <summary>
        /// Отримує всі книги, замовлені поточним користувачем
        /// </summary>
        /// <response code="200">Список успішно отриманий</response>
        /// <response code="401">Користувач не авторизований</response>
        /// <response code="403">Доступ заборонено</response>
        /// <response code="500">Помилка на сервері</response>
        [HttpGet("GetUserOrders")]
        [Authorize(Roles = "RegisteredUser,Manager,Administrator")]
        [ProducesResponseType(typeof(IEnumerable<BookDTO>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<BookDTO>>> GetCurrentUserOrders()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                if (user == null)
                {
                    _logger.LogWarning("User not authenticated when trying to get orders");
                    return Unauthorized(new { message = "Користувач не авторизований" });
                }

                var books = await _bookService.GetUserOrderedBooksAsync(user.Id);
                _logger.LogInformation($"Retrieved orders for user: {user.Id}");
                return Ok(books);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user's ordered books");
                return StatusCode(500, new { message = $"Внутрішня помилка сервера: {ex.Message}" });
            }
        }

        /// <summary>
        /// Отримує всі книги за заданим автором
        /// </summary>
        /// <response code="200">Успішне повернення списку книг</response>
        /// <response code="400">Неправильний параметр автора</response>
        /// <response code="500">Помилка на сервері</response>
        [HttpGet("GetByAuthor/{author}")]
        [ProducesResponseType(typeof(IEnumerable<BookDTO>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<BookDTO>>> GetBooksByAuthorName(string author)
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

        /// <summary>
        /// Перевіряє доступність книги за ідентифікатором
        /// </summary>
        /// <response code="200">Статус доступності успішно отриманий</response>
        /// <response code="404">Книга не знайдена</response>
        /// <response code="500">Помилка на сервері</response>
        [HttpGet("CheckAvailability/{id}")]
        [ProducesResponseType(typeof(bool), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<bool>> CheckBookAvailability(int id)
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

        /// <summary>
        /// Встановлює доступність книги за ідентифікатором
        /// </summary>
        /// <response code="204">Статус доступності успішно оновлений</response>
        /// <response code="400">Неправильні дані у запиті</response>
        /// <response code="401">Користувач не авторизований</response>
        /// <response code="403">Доступ заборонено</response>
        /// <response code="404">Книга не знайдена</response>
        /// <response code="500">Помилка на сервері</response>
        [HttpPut("SetAvailability/{id}")]
        [Authorize(Roles = "Manager,Administrator")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateBookAvailability(int id, [FromBody] bool isAvailable)
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