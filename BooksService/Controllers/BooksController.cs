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

        // ������ �� ����� � ��������� ������, ���������� �� ����������
        /// <response code="200">������ ���������� ������ ����</response>
        /// <response code="500">������� �� ������</response>
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

                // ���������� ����� �� �������
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

                // ����������
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
                return StatusCode(500, new { message = $"�������� ������� �������: {ex.Message}" });
            }
        }

        // ������ ����� ����� �� ���������������
        /// <response code="200">����� ��������</response>
        /// <response code="404">����� �� ��������</response>
        /// <response code="500">������� �� ������</response>
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
                return NotFound(new { message = $"����� � ID {id} �� ��������" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving book with ID: {id}");
                return StatusCode(500, new { message = $"�������� ������� �������: {ex.Message}" });
            }
        }

        // ������� ���� �����
        /// <response code="201">����� ������ ��������</response>
        /// <response code="400">���������� ��� � �����</response>
        /// <response code="401">���������� �� �������������</response>
        /// <response code="403">������ ����������</response>
        /// <response code="500">������� �� ������</response>
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
                book.IsAvailable = true; // �� ������������� ���� ����� ��������
                var createdBook = await _bookService.AddBookAsync(book);
                _logger.LogInformation($"Book created successfully: {createdBook.Id}");
                return CreatedAtAction(nameof(GetBook), new { id = createdBook.Id }, createdBook);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating book");
                return StatusCode(500, new { message = $"�������� ������� �������: {ex.Message}" });
            }
        }

        // ������� ������� �����
        /// <response code="204">����� ������ ��������</response>
        /// <response code="400">���������� ��� � �����</response>
        /// <response code="401">���������� �� �������������</response>
        /// <response code="403">������ ����������</response>
        /// <response code="404">����� �� ��������</response>
        /// <response code="500">������� �� ������</response>
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
                return BadRequest(new { message = "������������� ����� � URL ������� ��������� � ��������������� � �� ������" });
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
                return NotFound(new { message = $"����� � ID {id} �� ��������" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating book with ID: {id}");
                return StatusCode(500, new { message = $"�������� ������� �������: {ex.Message}" });
            }
        }

        // ������� ����� �� ���������������
        /// <response code="204">����� ������ ��������</response>
        /// <response code="400">����� �� ���� ���� ��������</response>
        /// <response code="401">���������� �� �������������</response>
        /// <response code="403">������ ����������</response>
        /// <response code="404">����� �� ��������</response>
        /// <response code="500">������� �� ������</response>
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
                return NotFound(new { message = $"����� � ID {id} �� ��������" });
            }
            catch (BookDeleteException ex)
            {
                _logger.LogWarning(ex, $"Cannot delete book: {id}. {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting book with ID: {id}");
                return StatusCode(500, new { message = $"�������� ������� �������: {ex.Message}" });
            }
        }

        // �������� ����� ��� ��������� �����������
        /// <response code="200">����� ������ ���������</response>
        /// <response code="400">����� ���������� ��� ����������</response>
        /// <response code="401">���������� �� �������������</response>
        /// <response code="403">������ ����������</response>
        /// <response code="404">����� �� ��������</response>
        /// <response code="500">������� �� ������</response>
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
                // ��� ���������� ������������� ���������� ID �����������
                string userId = "1"; // ����������, �� ���������� � ID=1 ���� � ��� �����

                // ��������� ����������� ��� ��������� �����������
                //var user = await _userManager.GetUserAsync(User);
                //if (user == null)
                //{
                //    return Unauthorized(new { message = "���������� �� �������������" });
                //}

                // ������������� ���������� ID ������ user.Id
                var order = await _bookService.OrderBookAsync(id, userId);
                _logger.LogInformation($"Book ordered successfully: Book ID: {id}, User ID: {userId}");
                return Ok(order);
            }
            catch (BookNotFoundException ex)
            {
                _logger.LogWarning(ex, $"Book not found: {id}");
                return NotFound(new { message = $"����� � ID {id} �� ��������" });
            }
            catch (BookNotAvailableException ex)
            {
                _logger.LogWarning(ex, $"Book not available: {id}");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error ordering book with ID: {id}");
                return StatusCode(500, new { message = $"�������� ������� �������: {ex.Message}" });
            }
        }

        // ������ �� �����, �������� �������� ������������
        /// <response code="200">������ ������ ���������</response>
        /// <response code="401">���������� �� �������������</response>
        /// <response code="403">������ ����������</response>
        /// <response code="500">������� �� ������</response>
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
                // ��� ���������� ������������� ���������� ID �����������
                string userId = "1"; // ����������, �� ���������� � ID=1 ���� � ��� �����

                //var user = await _userManager.GetUserAsync(User);
                //if (user == null)
                //{
                //    return Unauthorized(new { message = "���������� �� �������������" });
                //}

                // ������������� ���������� userId ������ user.Id
                // ����� ������� �������� id, ����� ���� � ������������ �����
                var books = await _bookService.GetUserOrderedBooksAsync(userId);
                _logger.LogInformation($"Retrieved orders for user: {userId}");
                return Ok(books);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user's ordered books");
                return StatusCode(500, new { message = $"�������� ������� �������: {ex.Message}" });
            }
        }

        // ������ �� ����� �� �������
        /// <response code="200">������ ���������� ������ ����</response>
        /// <response code="400">������������ �������� ������</response>
        /// <response code="500">������� �� ������</response>
        [HttpGet("byauthor/{author}")]
        [ProducesResponseType(typeof(IEnumerable<BookDTO>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<BookDTO>>> GetBooksByAuthor(string author)
        {
            if (string.IsNullOrWhiteSpace(author))
            {
                _logger.LogWarning("Empty author parameter provided");
                return BadRequest(new { message = "��'� ������ �� ���� ���� �������" });
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
                return StatusCode(500, new { message = $"�������� ������� �������: {ex.Message}" });
            }
        }

        // �������� ���������� �����
        /// <response code="200">������ ���������� ������ ���������</response>
        /// <response code="404">����� �� ��������</response>
        /// <response code="500">������� �� ������</response>
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
                return NotFound(new { message = $"����� � ID {id} �� ��������" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking availability for book with ID: {id}");
                return StatusCode(500, new { message = $"�������� ������� �������: {ex.Message}" });
            }
        }

        // ���������� ���������� �����
        /// <response code="204">������ ���������� ������ ���������</response>
        /// <response code="400">���������� ��� � �����</response>
        /// <response code="401">���������� �� �������������</response>
        /// <response code="403">������ ����������</response>
        /// <response code="404">����� �� ��������</response>
        /// <response code="500">������� �� ������</response>
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
                return NotFound(new { message = $"����� � ID {id} �� ��������" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating availability for book with ID: {id}");
                return StatusCode(500, new { message = $"�������� ������� �������: {ex.Message}" });
            }
        }
    }
}