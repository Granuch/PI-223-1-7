using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PI_223_1_7.DbContext;
using PI_223_1_7.Models;
using System.Threading.Tasks;

namespace PL.Controllers
{
    [Authorize] // Базовий доступ для авторизованих
    public class OrdersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public OrdersController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // GET: Orders
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.Message = $"[Index] Доступ дозволено для: {user?.UserName}";
            return View();
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.Message = $"[Details] Доступ до замовлення #{id} для: {user?.UserName}";
            return View();
        }

        // GET: Orders/Create
        public async Task<IActionResult> Create(int? bookId)
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.Message = $"[Create] Створення замовлення для книги #{bookId} користувачем: {user?.UserName}";
            return View();
        }

        // POST: Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.Message = $"[POST Create] Замовлення створене користувачем: {user?.UserName}";
            return View("Create");
        }

        // GET: Orders/Delete/5 (тільки адміністратори)
        [Authorize(Roles = "Administrator")]
        public IActionResult Delete(int? id)
        {
            ViewBag.Message = $"[Delete] Адміністратор має доступ до видалення замовлення #{id}";
            return View();
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public IActionResult DeleteConfirmed(int id)
        {
            ViewBag.Message = $"[POST Delete] Замовлення #{id} видалено адміністратором";
            return RedirectToAction(nameof(Index));
        }
    }
}
