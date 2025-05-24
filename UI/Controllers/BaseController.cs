using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using UI.Services;

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
            // Логування для діагностики
            var logger = HttpContext.RequestServices.GetRequiredService<ILogger<BaseController>>();
            logger.LogInformation("BaseController: Перевірка сесії для {Controller}/{Action}",
                context.RouteData.Values["controller"], context.RouteData.Values["action"]);

            // Перевіряємо сесію
            var isAuthenticated = HttpContext.Session.GetString("IsAuthenticated") == "true";
            var userDataJson = HttpContext.Session.GetString("UserData");

            logger.LogInformation("BaseController: IsAuthenticated = {IsAuthenticated}, UserDataExists = {UserDataExists}",
                isAuthenticated, !string.IsNullOrEmpty(userDataJson));

            ViewBag.IsAuthenticated = isAuthenticated;

            if (isAuthenticated && !string.IsNullOrEmpty(userDataJson))
            {
                try
                {
                    var userData = JsonConvert.DeserializeObject<UserInfo>(userDataJson);
                    ViewBag.User = userData;

                    var roles = userData.Roles?.ToList() ?? new List<string>();
                    ViewBag.UserRoles = roles;
                    ViewBag.IsManager = roles.Contains("Manager");
                    ViewBag.IsAdministrator = roles.Contains("Administrator");

                    logger.LogInformation("BaseController: User loaded - {Email}, Roles: {Roles}",
                        userData.Email, string.Join(", ", roles));
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "BaseController: Помилка десеріалізації даних користувача");
                    // Якщо не можемо десеріалізувати, очищаємо сесію
                    HttpContext.Session.Clear();
                    ViewBag.IsAuthenticated = false;
                    ViewBag.User = null;
                    ViewBag.UserRoles = new List<string>();
                    ViewBag.IsManager = false;
                    ViewBag.IsAdministrator = false;
                }
            }
            else
            {
                ViewBag.User = null;
                ViewBag.UserRoles = new List<string>();
                ViewBag.IsManager = false;
                ViewBag.IsAdministrator = false;
            }

            await next();
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

