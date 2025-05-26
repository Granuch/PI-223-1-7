using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using UI.Models.DTOs;
using UI.Services;

namespace UI.Controllers
{
    public class BooksController : BaseController
    {
        private readonly ILogger<BooksController> _logger;

        public BooksController(IApiService apiService, ILogger<BooksController> logger)
            : base(apiService)
        {
            _logger = logger;
        }

        public async Task<IActionResult> Index(string sortOrder, string searchString, string genre, string type)
        {
            _logger.LogInformation("Books Index called with params: sortOrder={SortOrder}, searchString={SearchString}, genre={Genre}, type={Type}",
                sortOrder, searchString, genre, type);

            ViewBag.SortOrder = sortOrder;
            ViewBag.SearchString = searchString;
            ViewBag.Genre = genre;
            ViewBag.Type = type;

            var result = await _apiService.GetBooksWithFilteringAsync(sortOrder, searchString, genre, type);

            if (result.Success)
            {
                _logger.LogInformation("Successfully retrieved {Count} books", result.Data?.Count() ?? 0);
                return View(result.Data);
            }

            _logger.LogError("Failed to get books: {Message}", result.Message);
            TempData["ErrorMessage"] = result.Message;
            return View(new List<BookDTO>());
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var result = await _apiService.GetBookByIdAsync(id);

            if (result.Success)
            {
                return View(result.Data);
            }

            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookDTO book)
        {
            _logger.LogInformation("Attempting to create book: {@Book}", book);
            _logger.LogInformation("Book details: Title={Title}, Author={Author}, Genre={Genre}, Type={Type}, Description={Description}",
                book.Title, book.Author, book.Genre, book.Type, book.Description);

            ModelState.Remove("OrderId");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model state is invalid");
                foreach (var error in ModelState)
                {
                    _logger.LogWarning("Field: {Field}, Errors: {Errors}",
                        error.Key, string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage)));
                }
                return View(book);
            }

            book.IsAvailable = true;
            book.OrderId = 0;

            var result = await _apiService.CreateBookAsync(book);

            if (result.Success)
            {
                TempData["SuccessMessage"] = "Книга успішно створена!";
                return RedirectToAction("Index");
            }

            _logger.LogError("Failed to create book: {Message}", result.Message);
            ModelState.AddModelError("", result.Message);
            return View(book);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var result = await _apiService.GetBookByIdAsync(id);

            if (result.Success)
            {
                return View(result.Data);
            }

            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BookDTO book)
        {
            _logger.LogInformation("Attempting to edit book with ID: {BookId}", id);
            _logger.LogInformation("Book data received: Title={Title}, Author={Author}, Genre={Genre}, Type={Type}, Year={Year}, IsAvailable={IsAvailable}, Description={Description}",
                book.Title, book.Author, book.Genre, book.Type, book.Year, book.IsAvailable, book.Description);

            if (id != book.Id)
            {
                _logger.LogWarning("Book ID mismatch: URL ID {UrlId} vs Model ID {ModelId}", id, book.Id);
                ModelState.AddModelError("", "ID does not match");
                return View(book);
            }

            ModelState.Remove("OrderId");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model state is invalid for book {BookId}", id);
                foreach (var error in ModelState)
                {
                    _logger.LogWarning("Field: {Field}, Errors: {Errors}",
                        error.Key, string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage)));
                }
                return View(book);
            }

            _logger.LogInformation("Calling UpdateBookAsync for book {BookId}", id);
            var result = await _apiService.UpdateBookAsync(id, book);
            _logger.LogInformation("UpdateBookAsync result: Success={Success}, Message={Message}", result.Success, result.Message);

            if (result.Success)
            {
                _logger.LogInformation("Book {BookId} successfully updated", id);
                TempData["SuccessMessage"] = "Book updated successfully!";
                return RedirectToAction("Index");
            }

            _logger.LogError("Failed to update book {BookId}: {Message}", id, result.Message);
            ModelState.AddModelError("", result.Message);
            TempData["ErrorMessage"] = $"Error updating book: {result.Message}";
            return View(book);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _apiService.GetBookByIdAsync(id);

            if (result.Success)
            {
                return View(result.Data);
            }

            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction("Index");
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await _apiService.DeleteBookAsync(id);

            if (result.Success)
            {
                TempData["SuccessMessage"] = "Book deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> MyOrders()
        {
            var result = await _apiService.GetUserOrdersAsync();

            if (result.Success)
            {
                return View(result.Data);
            }

            TempData["ErrorMessage"] = result.Message;
            return View(new List<BookDTO>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OrderBook(int bookId)
        {
            _logger.LogInformation("User attempting to order book {BookId}", bookId);

            var userDataJson = HttpContext.Session.GetString("UserData");
            if (string.IsNullOrEmpty(userDataJson))
            {
                TempData["ErrorMessage"] = "You need to log in to order a book.";
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Details", new { id = bookId }) });
            }

            try
            {
                _logger.LogInformation("User session data: {UserDataJson}", userDataJson);

                var userData = JsonConvert.DeserializeObject<UserInfo>(userDataJson);
                string userId = userData.UserId ?? userData.Id ?? userData.Email;

                _logger.LogInformation("Extracted UserId: {UserId} from UserInfo object", userId);

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogError("Cannot get UserId from session data: {UserData}", userDataJson);
                    TempData["ErrorMessage"] = "User identification error.";
                    return RedirectToAction("Details", new { id = bookId });
                }

                var bookResult = await _apiService.GetBookByIdAsync(bookId);
                if (!bookResult.Success)
                {
                    TempData["ErrorMessage"] = "Error retrieving book information.";
                    return RedirectToAction("Details", new { id = bookId });
                }

                if (!bookResult.Data.IsAvailable)
                {
                    TempData["ErrorMessage"] = "This book is currently unavailable for ordering.";
                    return RedirectToAction("Details", new { id = bookId });
                }

                var order = new OrderDTO
                {
                    UserId = userId,
                    BookId = bookId,
                    OrderDate = DateTime.Now,
                    Type = 1,
                    ReturnDate = null
                };

                var result = await _apiService.CreateOrderAsync(order);

                if (result.Success)
                {
                    _logger.LogInformation("User {UserId} successfully ordered book {BookId}", userId, bookId);
                    TempData["SuccessMessage"] = $"The book '{bookResult.Data.Title}' has been successfully ordered! Please check the 'My Orders' section.";
                    return RedirectToAction("MyOrders");
                }
                else
                {
                    _logger.LogError("Failed to create order for user {UserId} and book {BookId}: {Message}",
                        userId, bookId, result.Message);
                    TempData["ErrorMessage"] = $"Order creation error: {result.Message}";
                    return RedirectToAction("Details", new { id = bookId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing book order for book {BookId}", bookId);
                TempData["ErrorMessage"] = "An error occurred while processing the order";
                return RedirectToAction("Details", new { id = bookId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            _logger.LogInformation("User attempting to cancel order {OrderId}", orderId);
            var userDataJson = HttpContext.Session.GetString("UserData");
            if (string.IsNullOrEmpty(userDataJson))
            {
                TempData["ErrorMessage"] = "Please log in to cancel an order";
                return RedirectToAction("Login", "Account");
            }
            try
            {
                var userData = JsonConvert.DeserializeObject<UserInfo>(userDataJson);
                string userId = userData.UserId ?? userData.Id ?? userData.Email;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogError("Cannot get UserId from session data");
                    TempData["ErrorMessage"] = "User identification error";
                    return RedirectToAction("MyOrders");
                }
                var result = await _apiService.CancelOrderAsync(orderId, userId);
                if (result.Success)
                {
                    _logger.LogInformation("User {UserId} successfully cancelled order {OrderId}", userId, orderId);
                    TempData["SuccessMessage"] = "Order successfully cancelled! The book is now available for ordering again";
                }
                else
                {
                    _logger.LogError("Failed to cancel order {OrderId} for user {UserId}: {Message}",
                        orderId, userId, result.Message);
                    TempData["ErrorMessage"] = $"Error cancelling order: {result.Message}";
                }
                return RedirectToAction("MyOrders");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order cancellation for order {OrderId}", orderId);
                TempData["ErrorMessage"] = "An error occurred while cancelling the order";
                return RedirectToAction("MyOrders");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnBook(int orderId)
        {
            _logger.LogInformation("User attempting to return book for order {OrderId}", orderId);
            if (orderId <= 0)
            {
                TempData["ErrorMessage"] = "Invalid order identifier";
                return RedirectToAction("MyOrders");
            }
            try
            {
                var result = await _apiService.DeleteOrderAsync(orderId);
                if (result.Success)
                {
                    _logger.LogInformation("Successfully returned book for order {OrderId}", orderId);
                    TempData["SuccessMessage"] = "Book successfully returned! Thank you for using our service";
                }
                else
                {
                    _logger.LogError("Failed to return book for order {OrderId}: {Message}", orderId, result.Message);
                    TempData["ErrorMessage"] = $"Error returning book: {result.Message}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error returning book for order {OrderId}", orderId);
                TempData["ErrorMessage"] = "An error occurred while returning the book";
            }
            return RedirectToAction("MyOrders");
        }
    }
}
