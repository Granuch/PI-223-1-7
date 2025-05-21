using BLL.Interfaces;
using Mapping.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PI_223_1_7.DbContext;
using PI_223_1_7.Models;
using System.Threading.Tasks;

namespace PL.Controllers
{
    //[Authorize]// Базовий доступ для авторизованих 
    [ApiController]
    [Route("[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IOrderService _orderService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(UserManager<ApplicationUser> userManager, IOrderService orderService, ILogger<OrdersController> logger)
        {
            _userManager = userManager;
            _orderService = orderService;
            _logger = logger;
        }

        [HttpGet("GetSpecific/{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            try
            {
                var order = await _orderService.GetSpecificOrder(id);
                return Ok(order);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, $"Order not found: {id}");
                return NotFound(ex.Message);
            }
        }

        [HttpGet("Getall")]
        public async Task<IActionResult> GetAllOrders()
        {
            try
            {
                var orders = await _orderService.GetAllWithDetails();
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("CreateNewOrder")]
        public async Task<IActionResult> CreateOrder([FromBody]OrderDTO orderDTO)
        {
            try
            {
                await _orderService.CreateOrder(orderDTO);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("Update")]
        public async Task<IActionResult> UpdateOrder(int orderId, [FromBody]OrderDTO UpdatedOrder)
        {
            try
            {
                if (orderId != UpdatedOrder.Id)
                    return NotFound();

                var exist = await _orderService.GetSpecificOrder(orderId);
                if (exist == null)
                    return NotFound();

                await _orderService.UpdateOrder(UpdatedOrder);

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("Delete")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            try
            {
                await _orderService.DeleteOrderById(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Order with id: {id} not found");
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
