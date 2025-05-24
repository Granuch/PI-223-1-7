
using System.IdentityModel.Tokens.Jwt;

namespace UI.Middleware
{
    public class TokenRefreshMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TokenRefreshMiddleware> _logger;

        public TokenRefreshMiddleware(RequestDelegate next, ILogger<TokenRefreshMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Перевіряємо токен тільки для авторизованих користувачів
                var isAuthenticated = context.Session.GetString("IsAuthenticated") == "true";
                if (isAuthenticated)
                {
                    var token = context.Session.GetString("AuthToken");

                    if (!string.IsNullOrEmpty(token))
                    {
                        try
                        {
                            var jwtHandler = new JwtSecurityTokenHandler();
                            var jwtToken = jwtHandler.ReadJwtToken(token);

                            // Якщо токен прострочений або скоро прострочиться
                            if (jwtToken.ValidTo < DateTime.UtcNow.AddMinutes(10))
                            {
                                _logger.LogWarning("Token is expired or expires soon for user session");

                                // Можна додати логіку для оновлення токена
                                // Або показати повідомлення користувачу
                                context.Session.SetString("TokenWarning", "true");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error checking token expiration");
                            // Видаляємо невалідний токен
                            context.Session.Remove("AuthToken");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TokenRefreshMiddleware");
            }

            await _next(context);
        }
    }

    // Extension method для легкої реєстрації middleware
    public static class TokenRefreshMiddlewareExtensions
    {
        public static IApplicationBuilder UseTokenRefresh(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TokenRefreshMiddleware>();
        }
    }
}