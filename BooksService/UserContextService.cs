using System.Security.Claims;

namespace PL.Services
{
    public interface IUserContextService
    {
        bool IsAuthenticated();
        bool IsAdministrator();
        bool IsManager();
        string GetCurrentUserEmail();
        List<string> GetCurrentUserRoles();
        string GetCurrentUserId();
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

        public bool IsAuthenticated()
        {
            var isAuth = _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
            _logger.LogInformation("Books Service - IsAuthenticated: {IsAuth}, User: {User}",
                isAuth, _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "Anonymous");
            return isAuth;
        }

        public bool IsAdministrator()
        {
            var hasRole = _httpContextAccessor.HttpContext?.User?.IsInRole("Administrator") ?? false;
            _logger.LogInformation("Books Service - IsAdministrator: {HasRole}", hasRole);
            return hasRole;
        }

        public bool IsManager()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var hasRole = user?.IsInRole("Manager") == true || user?.IsInRole("Administrator") == true;
            _logger.LogInformation("Books Service - IsManager: {HasRole}", hasRole);
            return hasRole;
        }

        public string GetCurrentUserEmail()
        {
            var email = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value ??
                       _httpContextAccessor.HttpContext?.User?.Identity?.Name;
            _logger.LogInformation("Books Service - GetCurrentUserEmail: {Email}", email ?? "null");
            return email;
        }

        public string GetCurrentUserId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                        user?.FindFirst("sub")?.Value ??
                        user?.FindFirst("userId")?.Value;
            _logger.LogInformation("Books Service - GetCurrentUserId: {UserId}", userId ?? "null");
            return userId;
        }

        public List<string> GetCurrentUserRoles()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
                _logger.LogInformation("Books Service - User roles: {Roles}", string.Join(", ", roles));
                return roles;
            }
            return new List<string>();
        }
    }
}