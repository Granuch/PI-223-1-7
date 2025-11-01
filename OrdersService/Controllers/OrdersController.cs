using BLL.Interfaces;
using Mapping.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace PL.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        [HttpGet("GetSpecific/{id}")]
        [Authorize]
        public async Task<IActionResult> GetOrder(int id)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isManagerOrAdmin = User.IsInRole("Manager") || User.IsInRole("Administrator");

                var order = await _orderService.GetSpecificOrder(id);

                // Users can only see their own orders unless they are Manager/Admin
                if (!isManagerOrAdmin && order.UserId != currentUserId)
                {
                    _logger.LogWarning("User {UserId} tried to access order {OrderId} belonging to another user",
                        currentUserId, id);
                    return Forbid();
                }

                return Ok(order);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, $"Order not found: {id}");
                return NotFound(ex.Message);
            }
        }

        [HttpGet("GetAll")]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> GetAllOrders()
        {
            try
            {
                var orders = await _orderService.GetAllWithDetails();
                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all orders");
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("GetMyOrders")]
        [Authorize]
        public async Task<IActionResult> GetMyOrders()
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized();
                }

                var allOrders = await _orderService.GetAllWithDetails();
                var myOrders = allOrders.Where(o => o.UserId == currentUserId);

                return Ok(myOrders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user orders");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("CreateNewOrder")]
        public async Task<IActionResult> CreateOrder([FromBody] OrderDTO orderDTO)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isManagerOrAdmin = User.IsInRole("Manager") || User.IsInRole("Administrator");

                // Regular users can only create orders for themselves
                if (!isManagerOrAdmin && orderDTO.UserId != currentUserId)
                {
                    _logger.LogWarning("User {UserId} tried to create order for another user", currentUserId);
                    return Forbid();
                }

                await _orderService.CreateOrder(orderDTO);
                return Ok(new { success = true, message = "Order created successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("Update")]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> UpdateOrder([FromBody] OrderDTO UpdatedOrder)
        {
            try
            {
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
                _logger.LogError(ex, "Error updating order");
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("Delete/{id:int}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isManagerOrAdmin = User.IsInRole("Manager") || User.IsInRole("Administrator");

                var order = await _orderService.GetSpecificOrder(id);

                // Users can only delete their own orders unless they are Manager/Admin
                if (!isManagerOrAdmin && order.UserId != currentUserId)
                {
                    _logger.LogWarning("User {UserId} tried to delete order {OrderId} belonging to another user",
                        currentUserId, id);
                    return Forbid();
                }

                await _orderService.DeleteOrderById(id);
                return Ok(new { success = true, message = "Order deleted successfully" });
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
                _logger.LogError(ex, "Error deleting order");
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("TestAuth")]
        [AllowAnonymous]
        public IActionResult TestAuth()
        {
            return Ok(new
            {
                IsAuthenticated = User?.Identity?.IsAuthenticated ?? false,
                UserName = User?.Identity?.Name,
                UserId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                Email = User?.FindFirst(ClaimTypes.Email)?.Value,
                Roles = User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList(),
                Claims = User?.Claims?.Select(c => new { c.Type, c.Value }).ToList()
            });
        }
    }
}