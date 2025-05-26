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
            if (!ModelState.IsValid)
            {
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
        public async Task<IActionResult> Edit(int id, EditOrderDTO editOrder)
        {
            if (id != editOrder.Id)
            {
                ModelState.AddModelError("", "ID don't match");
                return View(editOrder);
            }

            if (!ModelState.IsValid)
            {
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

            var result = await _apiService.UpdateOrderAsync(id, orderDto);

            if (result.Success)
            {
                TempData["SuccessMessage"] = "Order successfully updated!";
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", result.Message);
            return View(editOrder);
        }

        [HttpGet]
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