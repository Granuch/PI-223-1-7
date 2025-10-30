
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
                var isAuthenticated = context.Session.GetString("IsAuthenticated") == "true";
                if (isAuthenticated)
                {
                    var token = context.Session.GetString("AccessToken");

                    if (!string.IsNullOrEmpty(token))
                    {
                        try
                        {
                            var jwtHandler = new JwtSecurityTokenHandler();
                            var jwtToken = jwtHandler.ReadJwtToken(token);

                            if (jwtToken.ValidTo < DateTime.UtcNow.AddMinutes(10))
                            {
                                _logger.LogWarning("Token is expired or expires soon for user session");

                                context.Session.SetString("TokenWarning", "true");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error checking token expiration");
                            context.Session.Remove("AccessToken");
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

    public static class TokenRefreshMiddlewareExtensions
    {
        public static IApplicationBuilder UseTokenRefresh(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TokenRefreshMiddleware>();
        }
    }
}