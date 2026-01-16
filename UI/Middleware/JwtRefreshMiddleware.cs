using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace UI.Middleware
{
    public class JwtRefreshMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<JwtRefreshMiddleware> _logger;

        public JwtRefreshMiddleware(RequestDelegate next, ILogger<JwtRefreshMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var accessToken = context.Session.GetString("AccessToken");

            if (!string.IsNullOrEmpty(accessToken))
            {
                try
                {
                    // Парсимо JWT токен
                    var handler = new JwtSecurityTokenHandler();
                    var jwtToken = handler.ReadJwtToken(accessToken);

                    // Встановлюємо claims у HttpContext.User
                    var claims = jwtToken.Claims.ToList();
                    var identity = new ClaimsIdentity(claims, "jwt");
                    context.User = new ClaimsPrincipal(identity);

                    // Перевіряємо чи токен скоро закінчиться (за 5 хвилин до закінчення)
                    var expiryTime = jwtToken.ValidTo;
                    var timeUntilExpiry = expiryTime - DateTime.UtcNow;

                    if (timeUntilExpiry.TotalMinutes < 5 && timeUntilExpiry.TotalMinutes > 0)
                    {
                        _logger.LogInformation("Token expires soon ({Minutes} minutes). Attempting refresh.",
                            timeUntilExpiry.TotalMinutes);

                        // Отримуємо ApiService через DI
                        var apiService = context.RequestServices.GetRequiredService<Services.IApiService>();
                        var refreshResult = await apiService.RefreshTokenAsync();

                        if (refreshResult.Success)
                        {
                            _logger.LogInformation("Token refreshed successfully in middleware");
                            // Оновлюємо токен після refresh
                            accessToken = context.Session.GetString("AccessToken");
                            if (!string.IsNullOrEmpty(accessToken))
                            {
                                jwtToken = handler.ReadJwtToken(accessToken);
                                claims = jwtToken.Claims.ToList();
                                identity = new ClaimsIdentity(claims, "jwt");
                                context.User = new ClaimsPrincipal(identity);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Token refresh failed in middleware");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing JWT token in middleware");
                    // Якщо токен невалідний, очищаємо його
                    context.Session.Remove("AccessToken");
                    context.Session.Remove("RefreshToken");
                    context.Session.Remove("TokenExpiry");
                    context.User = new ClaimsPrincipal(new ClaimsIdentity());
                }
            }
            else
            {
                // Немає токену - анонімний користувач
                context.User = new ClaimsPrincipal(new ClaimsIdentity());
            }

            await _next(context);
        }
    }

    // Extension method для зручного використання
    public static class JwtRefreshMiddlewareExtensions
    {
        public static IApplicationBuilder UseJwtRefresh(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<JwtRefreshMiddleware>();
        }
    }
}