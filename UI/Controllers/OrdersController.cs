using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI.Models.DTOs;
using UI.Services;

namespace UI.Controllers
{
    public class OrdersController : BaseController
    {
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IApiService apiService, ILogger<OrdersController> logger)
            : base(apiService)
        {
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var result = await _apiService.GetAllOrdersAsync();

            if (result.Success)
            {
                return View(result.Data);
            }

            TempData["ErrorMessage"] = result.Message;
            return View(new List<OrderDTO>());
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var result = await _apiService.GetOrderByIdAsync(id);

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
        public async Task<IActionResult> Create(OrderDTO order)
        {
            ModelState.Remove("Book");
            ModelState.Remove("UserEmail");
            
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState)
                {
                    foreach (var e in error.Value.Errors)
                    {
                        _logger.LogWarning("Validation error for {Field}: {Error}", error.Key, e.ErrorMessage);
                    }
                }
                return View(order);
            }

            var result = await _apiService.CreateOrderAsync(order);

            if (result.Success)
            {
                TempData["SuccessMessage"] = "Order successfully created!";
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", result.Message);
            return View(order);
        }

        [HttpGet]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> Edit(int id)
        {
            var result = await _apiService.GetOrderByIdAsync(id);

            if (result.Success)
            {
                var editModel = new EditOrderDTO
                {
                    Id = result.Data.Id,
                    UserId = result.Data.UserId,
                    BookId = result.Data.BookId,
                    OrderDate = result.Data.OrderDate,
                    ReturnDate = result.Data.ReturnDate,
                    Type = result.Data.Type,
                    Book = result.Data.Book,
                    UserEmail = result.Data.UserEmail
                };

                return View(editModel);
            }

            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> Edit(int id, EditOrderDTO editOrder)
        {
            _logger.LogInformation("Edit POST called: id={Id}, editOrder.Id={EditOrderId}", id, editOrder.Id);
            
            if (id != editOrder.Id)
            {
                _logger.LogWarning("ID mismatch: route id={Id}, form id={EditOrderId}", id, editOrder.Id);
                ModelState.AddModelError("", "ID don't match");
                return View(editOrder);
            }

            // Удаляем валидацию навигационных свойств
            ModelState.Remove("Book");
            ModelState.Remove("UserEmail");

            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState)
                {
                    foreach (var e in error.Value.Errors)
                    {
                        _logger.LogWarning("Validation error for {Field}: {Error}", error.Key, e.ErrorMessage);
                    }
                }
                
                var reloadResult = await _apiService.GetOrderByIdAsync(id);
                if (reloadResult.Success)
                {
                    editOrder.Book = reloadResult.Data.Book;
                    editOrder.UserEmail = reloadResult.Data.UserEmail;
                }
                return View(editOrder);
            }

            var orderDto = new OrderDTO
            {
                Id = editOrder.Id,
                UserId = editOrder.UserId,
                BookId = editOrder.BookId,
                OrderDate = editOrder.OrderDate,
                ReturnDate = editOrder.ReturnDate,
                Type = editOrder.Type
            };

            _logger.LogInformation("Calling UpdateOrderAsync with: {@OrderDto}", orderDto);
            var result = await _apiService.UpdateOrderAsync(id, orderDto);
            _logger.LogInformation("UpdateOrderAsync result: Success={Success}, Message={Message}", result.Success, result.Message);

            if (result.Success)
            {
                TempData["SuccessMessage"] = "Order successfully updated!";
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", result.Message ?? "An unexpected error occurred");
            return View(editOrder);
        }

        [HttpGet]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _apiService.GetOrderByIdAsync(id);

            if (result.Success)
            {
                return View(result.Data);
            }

            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction("Index");
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await _apiService.DeleteOrderAsync(id);

            if (result.Success)
            {
                TempData["SuccessMessage"] = "Order successfully deleted!";
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction("Index");
        }
    }
}