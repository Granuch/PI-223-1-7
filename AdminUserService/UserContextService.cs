using System.Security.Claims;

namespace PL.Services
{
    public interface IUserContextService
    {
        string GetCurrentUserId();
        string GetCurrentUserEmail();
        List<string> GetCurrentUserRoles();
        bool IsAuthenticated();
        bool IsInRole(string role);
        bool IsAdministrator();
        bool IsManager();
    }

    public class UserContextService : IUserContextService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<UserContextService> _logger;

        public UserContextService(IHttpContextAccessor httpContextAccessor, ILogger<UserContextService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public string GetCurrentUserId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                return user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                       user.FindFirst("sub")?.Value ??
                       user.FindFirst("userId")?.Value;
            }
            return null;
        }

        public string GetCurrentUserEmail()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                return user.FindFirst(ClaimTypes.Email)?.Value ??
                       user.FindFirst(ClaimTypes.Name)?.Value;
            }
            return null;
        }

        public List<string> GetCurrentUserRoles()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                return user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            }
            return new List<string>();
        }

        public bool IsAuthenticated()
        {
            var isAuth = _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
            _logger.LogInformation("IsAuthenticated: {IsAuth}, User: {User}",
                isAuth, _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "Anonymous");
            return isAuth;
        }

        public bool IsInRole(string role)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var hasRole = user?.IsInRole(role) ?? false;
            _logger.LogInformation("User {User} has role {Role}: {HasRole}",
                GetCurrentUserEmail() ?? "Anonymous", role, hasRole);
            return hasRole;
        }

        public bool IsAdministrator()
        {
            return IsInRole("Administrator");
        }

        public bool IsManager()
        {
            return IsInRole("Manager") || IsInRole("Administrator");
        }
    }
}

// Зареєструйте сервіс у Program.cs кожного мікросервіса
// builder.Services.AddScoped<IUserContextService, UserContextService>();