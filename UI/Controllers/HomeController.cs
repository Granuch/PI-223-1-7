using Microsoft.AspNetCore.Mvc;
using UI.Services;

namespace UI.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(IApiService apiService, ILogger<HomeController> logger)
            : base(apiService)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}