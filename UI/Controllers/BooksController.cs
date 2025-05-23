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

            // ДОДАЙТЕ ЦЕ ЛОГУВАННЯ
            _logger.LogInformation("Book details: Title={Title}, Author={Author}, Genre={Genre}, Type={Type}, Description={Description}",
                book.Title, book.Author, book.Genre, book.Type, book.Description);

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
            _logger.LogInformation("Book data received: {@Book}", book);

            if (id != book.Id)
            {
                _logger.LogWarning("Book ID mismatch: URL ID {UrlId} vs Model ID {ModelId}", id, book.Id);
                ModelState.AddModelError("", "ID не співпадає");
                return View(book);
            }

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

            var result = await _apiService.UpdateBookAsync(id, book);

            if (result.Success)
            {
                _logger.LogInformation("Book {BookId} successfully updated", id);
                TempData["SuccessMessage"] = "Книга успішно оновлена!";
                return RedirectToAction("Index");
            }

            _logger.LogError("Failed to update book {BookId}: {Message}", id, result.Message);

            // Додаємо повідомлення про помилку до ModelState та TempData
            ModelState.AddModelError("", result.Message);
            TempData["ErrorMessage"] = $"Помилка оновлення книги: {result.Message}";

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
                TempData["SuccessMessage"] = "Книга успішно видалена!";
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

            // Перевіряємо, чи користувач авторизований
            var userDataJson = HttpContext.Session.GetString("UserData");
            if (string.IsNullOrEmpty(userDataJson))
            {
                TempData["ErrorMessage"] = "Для замовлення книги потрібно увійти в систему";
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Details", new { id = bookId }) });
            }

            try
            {
                _logger.LogInformation("User session data: {UserDataJson}", userDataJson);

                // Парсимо дані користувача як UserInfo (правильний тип)
                var userData = JsonConvert.DeserializeObject<UserInfo>(userDataJson);
                string userId = userData.UserId ?? userData.Id ?? userData.Email;

                _logger.LogInformation("Extracted UserId: {UserId} from UserInfo object", userId);

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogError("Cannot get UserId from session data: {UserData}", userDataJson);
                    TempData["ErrorMessage"] = "Помилка ідентифікації користувача";
                    return RedirectToAction("Details", new { id = bookId });
                }

                // Перевіряємо, чи книга доступна
                var bookResult = await _apiService.GetBookByIdAsync(bookId);
                if (!bookResult.Success)
                {
                    TempData["ErrorMessage"] = "Помилка отримання інформації про книгу";
                    return RedirectToAction("Details", new { id = bookId });
                }

                if (!bookResult.Data.IsAvailable)
                {
                    TempData["ErrorMessage"] = "Ця книга зараз недоступна для замовлення";
                    return RedirectToAction("Details", new { id = bookId });
                }

                // Створюємо замовлення
                var order = new OrderDTO
                {
                    UserId = userId,
                    BookId = bookId,
                    OrderDate = DateTime.Now,
                    Type = 1, // Активне замовлення
                    ReturnDate = null
                };

                var result = await _apiService.CreateOrderAsync(order);

                if (result.Success)
                {
                    _logger.LogInformation("User {UserId} successfully ordered book {BookId}", userId, bookId);
                    TempData["SuccessMessage"] = $"Книгу '{bookResult.Data.Title}' успішно замовлено! Перевірте розділ 'Мої замовлення'";
                    return RedirectToAction("MyOrders");
                }
                else
                {
                    _logger.LogError("Failed to create order for user {UserId} and book {BookId}: {Message}",
                        userId, bookId, result.Message);
                    TempData["ErrorMessage"] = $"Помилка створення замовлення: {result.Message}";
                    return RedirectToAction("Details", new { id = bookId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing book order for book {BookId}", bookId);
                TempData["ErrorMessage"] = "Сталася помилка при обробці замовлення";
                return RedirectToAction("Details", new { id = bookId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            _logger.LogInformation("User attempting to cancel order {OrderId}", orderId);

            // Перевіряємо, чи користувач авторизований
            var userDataJson = HttpContext.Session.GetString("UserData");
            if (string.IsNullOrEmpty(userDataJson))
            {
                TempData["ErrorMessage"] = "Для скасування замовлення потрібно увійти в систему";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // Отримуємо UserId з сесії
                var userData = JsonConvert.DeserializeObject<UserInfo>(userDataJson);
                string userId = userData.UserId ?? userData.Id ?? userData.Email;

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogError("Cannot get UserId from session data");
                    TempData["ErrorMessage"] = "Помилка ідентифікації користувача";
                    return RedirectToAction("MyOrders");
                }

                var result = await _apiService.CancelOrderAsync(orderId, userId);

                if (result.Success)
                {
                    _logger.LogInformation("User {UserId} successfully cancelled order {OrderId}", userId, orderId);
                    TempData["SuccessMessage"] = "Замовлення успішно скасовано! Книга знову доступна для замовлення";
                }
                else
                {
                    _logger.LogError("Failed to cancel order {OrderId} for user {UserId}: {Message}",
                        orderId, userId, result.Message);
                    TempData["ErrorMessage"] = $"Помилка скасування замовлення: {result.Message}";
                }

                return RedirectToAction("MyOrders");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order cancellation for order {OrderId}", orderId);
                TempData["ErrorMessage"] = "Сталася помилка при скасуванні замовлення";
                return RedirectToAction("MyOrders");
            }
        }

        // Додайте цей метод в BooksController
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnBook(int orderId)
        {
            _logger.LogInformation("User attempting to return book for order {OrderId}", orderId);

            if (orderId <= 0)
            {
                TempData["ErrorMessage"] = "Невірний ідентифікатор замовлення";
                return RedirectToAction("MyOrders");
            }

            try
            {
                // Використовуємо існуючий метод для видалення замовлення
                var result = await _apiService.DeleteOrderAsync(orderId);

                if (result.Success)
                {
                    _logger.LogInformation("Successfully returned book for order {OrderId}", orderId);
                    TempData["SuccessMessage"] = "Книгу успішно повернено! Дякуємо за користування";
                }
                else
                {
                    _logger.LogError("Failed to return book for order {OrderId}: {Message}", orderId, result.Message);
                    TempData["ErrorMessage"] = $"Помилка повернення книги: {result.Message}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error returning book for order {OrderId}", orderId);
                TempData["ErrorMessage"] = "Сталася помилка при поверненні книги";
            }

            return RedirectToAction("MyOrders");
        }
    }
}
