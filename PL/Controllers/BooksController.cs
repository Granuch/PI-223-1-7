using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PI_223_1_7.Models;
using System.Threading.Tasks;

namespace PL.Controllers
{
    public class BooksController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public BooksController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // GET: Books - доступно всім
        public IActionResult Index()
        {
            ViewBag.Message = "[Index] Список книг (доступний для всіх)";
            return View();
        }

        // GET: Books/Details/5
        public IActionResult Details(int? id)
        {
            ViewBag.Message = $"[Details] Перегляд книги з ID = {id} (доступний для всіх)";
            return View();
        }

        // GET: Books/Create
        [Authorize(Roles = "Manager,Administrator")]
        public IActionResult Create()
        {
            ViewBag.Message = "[Create] Створення книги (доступно для менеджера або адміністратора)";
            return View();
        }

        // POST: Books/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager,Administrator")]
        public IActionResult CreateConfirmed()
        {
            ViewBag.Message = "[POST Create] Книга створена (заглушка)";
            return RedirectToAction(nameof(Index));
        }

        // GET: Books/Edit/5
        [Authorize(Roles = "Manager,Administrator")]
        public IActionResult Edit(int? id)
        {
            ViewBag.Message = $"[Edit] Редагування книги з ID = {id} (доступно для менеджера або адміністратора)";
            return View();
        }

        // POST: Books/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager,Administrator")]
        public IActionResult EditConfirmed(int id)
        {
            ViewBag.Message = $"[POST Edit] Книга з ID = {id} відредагована (заглушка)";
            return RedirectToAction(nameof(Index));
        }

        // GET: Books/Delete/5
        [Authorize(Roles = "Administrator")]
        public IActionResult Delete(int? id)
        {
            ViewBag.Message = $"[Delete] Видалення книги з ID = {id} (доступно тільки адміністратору)";
            return View();
        }

        // POST: Books/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public IActionResult DeleteConfirmed(int id)
        {
            ViewBag.Message = $"[POST Delete] Книга з ID = {id} видалена (заглушка)";
            return RedirectToAction(nameof(Index));
        }

        // GET: Books/Order/5
        [Authorize(Roles = "RegisteredUser,Manager,Administrator")]
        public IActionResult Order(int? id)
        {
            ViewBag.Message = $"[Order] Замовлення книги з ID = {id} (доступно зареєстрованим користувачам)";
            return View();
        }

        // POST: Books/Order/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "RegisteredUser,Manager,Administrator")]
        public IActionResult ConfirmOrder(int bookId, string orderType)
        {
            ViewBag.Message = $"[POST Order] Книга ID = {bookId} замовлена з типом '{orderType}' (заглушка)";
            return RedirectToAction(nameof(Index));
        }
    }
}
