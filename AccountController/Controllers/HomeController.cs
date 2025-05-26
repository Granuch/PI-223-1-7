using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PL.Controllers.Api
{
    [Route("api")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        [HttpGet]
        public IActionResult Index()
        {
            return Ok(new { message = "API працює" });
        }

        [HttpGet("secure")]
        [Authorize]
        public IActionResult SecurePage()
        {
            return Ok(new { message = "Ви маєте доступ до захищеного ресурсу" });
        }

        [HttpGet("admin")]
        [Authorize(Roles = "Administrator")]
        public IActionResult AdminPage()
        {
            return Ok(new { message = "Ви маєте доступ до адміністративного ресурсу" });
        }
    }
}