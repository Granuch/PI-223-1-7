using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using UI.Services;
using System.Security.Claims;

namespace UI.Controllers
{
    public class BaseController : Controller
    {
        protected readonly IApiService _apiService;

        public BaseController(IApiService apiService)
        {
            _apiService = apiService;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var logger = HttpContext.RequestServices.GetRequiredService<ILogger<BaseController>>();
            logger.LogInformation("BaseController: Перевірка автентифікації для {Controller}/{Action}",
                context.RouteData.Values["controller"], context.RouteData.Values["action"]);

            // Перевіряємо Cookie Authentication
            var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
            ViewBag.IsAuthenticated = isAuthenticated;

            if (isAuthenticated)
            {
                try
                {
                    // Отримуємо дані з claims
                    var email = User.FindFirst(ClaimTypes.Email)?.Value;
                    var firstName = User.FindFirst("FirstName")?.Value;
                    var lastName = User.FindFirst("LastName")?.Value;
                    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var loginTime = User.FindFirst("LoginTime")?.Value;

                    var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

                    // Логування для діагностики
                    logger.LogInformation("BaseController: User authenticated - {Email}, UserId: {UserId}, Roles: {Roles}, LoginTime: {LoginTime}",
                        email, userId, string.Join(", ", roles), loginTime);

                    // Створюємо UserInfo для сумісності
                    var userData = new UserInfo
                    {
                        Id = userId,
                        UserId = userId,
                        Email = email,
                        FirstName = firstName,
                        LastName = lastName,
                        Roles = roles
                    };

                    ViewBag.User = userData;
                    ViewBag.UserRoles = roles;
                    ViewBag.IsManager = roles.Contains("Manager");
                    ViewBag.IsAdministrator = roles.Contains("Administrator");

                    // Також зберігаємо в сесії для сумісності з ApiService
                    if (HttpContext.Session.GetString("UserData") == null)
                    {
                        HttpContext.Session.SetString("IsAuthenticated", "true");
                        HttpContext.Session.SetString("UserData", JsonConvert.SerializeObject(userData));
                        logger.LogInformation("BaseController: Session data updated from claims");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "BaseController: Помилка обробки claims");
                    ClearAuthData();
                }
            }
            else
            {
                ClearAuthData();
                logger.LogInformation("BaseController: User not authenticated");
            }

            await next();
        }

        private void ClearAuthData()
        {
            ViewBag.IsAuthenticated = false;
            ViewBag.User = null;
            ViewBag.UserRoles = new List<string>();
            ViewBag.IsManager = false;
            ViewBag.IsAdministrator = false;
        }
    }

    public class UserInfo
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("userId")]
        public string UserId { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("firstName")]
        public string FirstName { get; set; }

        [JsonProperty("lastName")]
        public string LastName { get; set; }

        [JsonProperty("roles")]
        public IEnumerable<string> Roles { get; set; }

        [JsonIgnore]
        public string GetUserId => UserId ?? Id;
    }
}