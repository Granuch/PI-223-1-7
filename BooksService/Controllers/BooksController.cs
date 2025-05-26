using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BLL.Interfaces;
using Mapping.DTOs;
using PI_223_1_7.Enums;
using BLL.Exceptions;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using PL.Services;

namespace PL.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    public class BooksController : ControllerBase
    {
        private readonly IBookService _bookService;
        private readonly ILogger<BooksController> _logger;
        private readonly IUserContextService _userContext; 

        public BooksController(
            IBookService bookService,
            ILogger<BooksController> logger,
            IUserContextService userContext)
        {
            _bookService = bookService ?? throw new ArgumentNullException(nameof(bookService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext)); 
        }

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
                _logger.LogInformation("GetBooks called with params: sortOrder={SortOrder}, searchString={SearchString}, genre={Genre}, type={Type}",
                    sortOrder, searchString, genre, type);

                IEnumerable<BookDTO> books;

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
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

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
                _logger.LogInformation("GetBooksWithFiltering called. User authenticated: {IsAuth}, User: {User}",
                    _userContext.IsAuthenticated(), _userContext.GetCurrentUserEmail() ?? "Anonymous");

                IEnumerable<BookDTO> books;

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
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpGet("GetUserOrders")]
        [Authorize] 
        [ProducesResponseType(typeof(IEnumerable<BookDTO>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<BookDTO>>> GetCurrentUserOrders()
        {
            try
            {
                _logger.LogInformation("GetCurrentUserOrders called");
                _logger.LogInformation("User authenticated: {IsAuth}, User: {User}",
                    _userContext.IsAuthenticated(), _userContext.GetCurrentUserEmail() ?? "Anonymous");

                if (!_userContext.IsAuthenticated())
                {
                    _logger.LogWarning("User not authenticated when trying to get orders");
                    return Unauthorized(new { message = "User is not autorized" });
                }

                var userId = _userContext.GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Cannot get UserId from context");
                    return BadRequest();
                }

                var books = await _bookService.GetUserOrderedBooksAsync(userId);
                _logger.LogInformation($"Retrieved orders for user: {userId}");
                return Ok(books);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user's ordered books");
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

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
                return NotFound(new { message = $"Book not found: {id}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving book with ID: {id}");
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpPost("CreateBook")]
        [Authorize]
        [ProducesResponseType(typeof(BookDTO), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<BookDTO>> CreateBook([FromBody] BookDTO book)
        {
            try
            {
                _logger.LogInformation("CreateBook called by user: {User}", _userContext.GetCurrentUserEmail());

                if (!_userContext.IsManager())
                {
                    _logger.LogWarning("Non-manager user tried to create book: {User}", _userContext.GetCurrentUserEmail());
                    return Forbid();
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state when creating book");
                    return BadRequest(ModelState);
                }

                book.IsAvailable = true;
                var createdBook = await _bookService.AddBookAsync(book);
                _logger.LogInformation($"Book created successfully: {createdBook.Id}");
                return CreatedAtAction(nameof(GetBook), new { id = createdBook.Id }, createdBook);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating book");
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpPut("UpdateBook/{id:int}")]
        [Authorize] 
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateBook(int id, [FromBody] BookDTO book)
        {
            try
            {
                _logger.LogInformation("UpdateBook called by user: {User}", _userContext.GetCurrentUserEmail());

                if (!_userContext.IsManager())
                {
                    _logger.LogWarning("Non-manager user tried to update book: {User}", _userContext.GetCurrentUserEmail());
                    return Forbid();
                }

                if (id != book.Id)
                {
                    _logger.LogWarning($"Mismatch between URL ID: {id} and body ID: {book.Id}");
                    return BadRequest(new { message = "The book ID in the URL must match the ID in the request body" });
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state when updating book");
                    return BadRequest(ModelState);
                }

                await _bookService.UpdateBookAsync(book);
                _logger.LogInformation($"Book successfully updated: {id}");
                return NoContent();
            }
            catch (BookNotFoundException ex)
            {
                _logger.LogWarning(ex, $"Book not found: {id}");
                return NotFound(new { message = $"Book not found: {id}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating book with ID: {id}");
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpDelete("DeleteBook/{id}")]
        [Authorize] 
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
                _logger.LogInformation("DeleteBook called by user: {User}", _userContext.GetCurrentUserEmail());

                if (!_userContext.IsAdministrator())
                {
                    _logger.LogWarning("Non-administrator user tried to delete book: {User}", _userContext.GetCurrentUserEmail());
                    return Forbid();
                }

                await _bookService.DeleteBookAsync(id);
                _logger.LogInformation($"Book successfully deleted: {id}");
                return NoContent();
            }
            catch (BookNotFoundException ex)
            {
                _logger.LogWarning(ex, $"Book not found: {id}");
                return NotFound(new { message = $"Book not found:  {id}" });
            }
            catch (BookDeleteException ex)
            {
                _logger.LogWarning(ex, $"Cannot delete book: {id}. {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting book with ID: {id}");
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpPost("OrderBook/{id:int}")]
        [Authorize] 
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
                _logger.LogInformation("OrderBook called by user: {User}", _userContext.GetCurrentUserEmail());

                if (!_userContext.IsAuthenticated())
                {
                    return Unauthorized(new { message = "Користувач не авторизований" });
                }

                var userId = _userContext.GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Cannot get UserId from context");
                    return BadRequest(new { message = "Помилка ідентифікації користувача" });
                }

                var order = await _bookService.OrderBookAsync(id, userId);
                _logger.LogInformation($"Book ordered successfully: Book ID: {id}, User ID: {userId}");
                return Ok(order);
            }
            catch (BookNotFoundException ex)
            {
                _logger.LogWarning(ex, $"Book not found: {id}");
                return NotFound(new { message = $"Book not found:  {id}" });
            }
            catch (BookNotAvailableException ex)
            {
                _logger.LogWarning(ex, $"Book not available: {id}");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error ordering book with ID: {id}");
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpGet("byauthor/{author}")]
        [ProducesResponseType(typeof(IEnumerable<BookDTO>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<BookDTO>>> GetBooksByAuthor(string author)
        {
            if (string.IsNullOrWhiteSpace(author))
            {
                _logger.LogWarning("Empty author parameter provided");
                return BadRequest(new { message = "Empty author parameter provided" });
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
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

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
                return NotFound(new { message = $"Book not found:  {id}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking availability for book with ID: {id}");
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpPut("SetAvailability/{id}")]
        [Authorize] 
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
                _logger.LogInformation("SetBookAvailability called by user: {User}", _userContext.GetCurrentUserEmail());

                if (!_userContext.IsManager())
                {
                    _logger.LogWarning("Non-manager user tried to set book availability: {User}", _userContext.GetCurrentUserEmail());
                    return Forbid();
                }

                await _bookService.SetBookAvailabilityAsync(id, isAvailable);
                _logger.LogInformation($"Book availability updated: {id}, set to: {isAvailable}");
                return NoContent();
            }
            catch (BookNotFoundException ex)
            {
                _logger.LogWarning(ex, $"Book not found: {id}");
                return NotFound(new { message = $"Book not found:   {id}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating availability for book with ID: {id}");
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }
    }
}