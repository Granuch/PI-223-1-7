using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PI_223_1_7.DbContext;
using PI_223_1_7.Models;
using System.Threading.Tasks;

namespace PL.Controllers
{
    [Authorize]// Базовий доступ для авторизованих 
    [ApiController]
    [Route("[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IOrderService _orderService;

        public OrdersController(UserManager<ApplicationUser> userManager, IOrderService orderService)
        {
            _userManager = userManager;
            _orderService = orderService;
        }

        [HttpGet("GetSpecific/{id}")]
        public async Task<IActionResult> GetOrder(int? id)
        {
            var order = await _orderService.GetAllWithDetails();
            return Ok(order);
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _orderService.GetAllWithoutDetails();
            return Ok(orders);
        }
    }
}
