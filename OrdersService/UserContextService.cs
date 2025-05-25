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
        void LogCurrentUserInfo();
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
            _logger.LogInformation("AdminUsers Service - IsAuthenticated: {IsAuth}, User: {User}",
                isAuth, _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "Anonymous");
            return isAuth;
        }

        public bool IsAdministrator()
        {
            var hasRole = _httpContextAccessor.HttpContext?.User?.IsInRole("Administrator") ?? false;
            _logger.LogInformation("AdminUsers Service - IsAdministrator: {HasRole}", hasRole);
            return hasRole;
        }

        public bool IsManager()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var hasRole = user?.IsInRole("Manager") == true || user?.IsInRole("Administrator") == true;
            _logger.LogInformation("AdminUsers Service - IsManager: {HasRole}", hasRole);
            return hasRole;
        }

        public string GetCurrentUserEmail()
        {
            var email = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value ??
                       _httpContextAccessor.HttpContext?.User?.Identity?.Name;
            _logger.LogInformation("AdminUsers Service - GetCurrentUserEmail: {Email}", email ?? "null");
            return email;
        }

        public string GetCurrentUserId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                        user?.FindFirst("sub")?.Value ??
                        user?.FindFirst("userId")?.Value;
            _logger.LogInformation("AdminUsers Service - GetCurrentUserId: {UserId}", userId ?? "null");
            return userId;
        }

        public List<string> GetCurrentUserRoles()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
                _logger.LogInformation("AdminUsers Service - User roles: {Roles}", string.Join(", ", roles));
                return roles;
            }
            return new List<string>();
        }

        public void LogCurrentUserInfo()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var request = _httpContextAccessor.HttpContext?.Request;

            _logger.LogInformation("=== DIAGNOSTIC INFO START ===");
            _logger.LogInformation("Service: {ServiceName}", GetType().Assembly.GetName().Name);
            _logger.LogInformation("Request Path: {Path}", request?.Path ?? "null");
            _logger.LogInformation("IsAuthenticated: {IsAuth}", user?.Identity?.IsAuthenticated ?? false);
            _logger.LogInformation("Identity Name: {Name}", user?.Identity?.Name ?? "null");
            _logger.LogInformation("Identity Type: {Type}", user?.Identity?.GetType().Name ?? "null");

            if (user?.Claims != null)
            {
                _logger.LogInformation("Claims count: {Count}", user.Claims.Count());
                foreach (var claim in user.Claims)
                {
                    _logger.LogInformation("  Claim: {Type} = {Value}", claim.Type, claim.Value);
                }
            }

            if (request?.Cookies != null)
            {
                _logger.LogInformation("Cookies count: {Count}", request.Cookies.Count);
                foreach (var cookie in request.Cookies)
                {
                    var value = cookie.Value.Length > 50 ? cookie.Value.Substring(0, 50) + "..." : cookie.Value;
                    _logger.LogInformation("  Cookie: {Name} = {Value}", cookie.Key, value);
                }
            }

            if (request?.Headers != null && request.Headers.ContainsKey("Cookie"))
            {
                _logger.LogInformation("Cookie header present: {HasCookieHeader}", true);
            }

            _logger.LogInformation("=== DIAGNOSTIC INFO END ===");
        }
    }
}
