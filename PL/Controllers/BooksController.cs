using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PI_223_1_7.Models;
using BLL.Interfaces;
using Mapping.DTOs;
using PI_223_1_7.Enums;
using BLL.Exceptions;

namespace PL.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class BooksController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IBookService _bookService;

        public BooksController(UserManager<ApplicationUser> userManager, IBookService bookService)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _bookService = bookService ?? throw new ArgumentNullException(nameof(bookService));
        }

        // Отримує всі книги з можливістю пошуку, фільтрації та сортування
        /// <response code="200">Успішне повернення списку книг</response>
        /// <response code="500">Помилка на сервері</response>
        [HttpGet]
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
                return StatusCode(500, new { message = $"Внутрішня помилка сервера: {ex.Message}" });
            }
        }

        // Отримує деталі книги за ідентифікатором
        /// <response code="200">Книга знайдена</response>
        /// <response code="404">Книга не знайдена</response>
        /// <response code="500">Помилка на сервері</response>
        [HttpGet("{id}")]
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
            catch (BookNotFoundException)
            {
                return NotFound(new { message = $"Книга з ID {id} не знайдена" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Внутрішня помилка сервера: {ex.Message}" });
            }
        }

        // Створює нову книгу
        /// <response code="201">Книга успішно створена</response>
        /// <response code="400">Неправильні дані у запиті</response>
        /// <response code="401">Користувач не авторизований</response>
        /// <response code="403">Доступ заборонено</response>
        /// <response code="500">Помилка на сервері</response>
        [HttpPost]
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
                return BadRequest(ModelState);
            }

            try
            {
                book.IsAvailable = true; // За замовчуванням нова книга доступна
                var createdBook = await _bookService.AddBookAsync(book);
                return CreatedAtAction(nameof(GetBook), new { id = createdBook.Id }, createdBook);
            }
            catch (Exception ex)
            {
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
        [HttpPut("{id}")]
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
                return BadRequest(new { message = "Ідентифікатор книги в URL повинен співпадати з ідентифікатором в тілі запиту" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _bookService.UpdateBookAsync(book);
                return NoContent();
            }
            catch (BookNotFoundException)
            {
                return NotFound(new { message = $"Книга з ID {id} не знайдена" });
            }
            catch (Exception ex)
            {
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
        [HttpDelete("{id}")]
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
                return NoContent();
            }
            catch (BookNotFoundException)
            {
                return NotFound(new { message = $"Книга з ID {id} не знайдена" });
            }
            catch (BookDeleteException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
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
        [HttpPost("{id}/order")]
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
                return Ok(order);
            }
            catch (BookNotFoundException)
            {
                return NotFound(new { message = $"Книга з ID {id} не знайдена" });
            }
            catch (BookNotAvailableException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Внутрішня помилка сервера: {ex.Message}" });
            }
        }

        // Отримує всі книги, замовлені поточним користувачем
        /// <response code="200">Список успішно отриманий</response>
        /// <response code="401">Користувач не авторизований</response>
        /// <response code="403">Доступ заборонено</response>
        /// <response code="500">Помилка на сервері</response>
        [HttpGet("myorders")]
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
                return Ok(books);
            }
            catch (Exception ex)
            {
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
                return BadRequest(new { message = "Ім'я автора не може бути порожнім" });
            }

            try
            {
                var books = await _bookService.GetBooksByAuthorAsync(author);
                return Ok(books);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Внутрішня помилка сервера: {ex.Message}" });
            }
        }

        // Перевіряє доступність книги
        /// <response code="200">Статус доступності успішно отриманий</response>
        /// <response code="404">Книга не знайдена</response>
        /// <response code="500">Помилка на сервері</response>
        [HttpGet("{id}/availability")]
        [ProducesResponseType(typeof(bool), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<bool>> IsBookAvailable(int id)
        {
            try
            {
                var isAvailable = await _bookService.IsBookAvailableAsync(id);
                return Ok(isAvailable);
            }
            catch (BookNotFoundException)
            {
                return NotFound(new { message = $"Книга з ID {id} не знайдена" });
            }
            catch (Exception ex)
            {
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
        [HttpPut("{id}/availability")]
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
                return NoContent();
            }
            catch (BookNotFoundException)
            {
                return NotFound(new { message = $"Книга з ID {id} не знайдена" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Внутрішня помилка сервера: {ex.Message}" });
            }
        }
    }
}