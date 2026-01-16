

using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace UI.Middleware
{
    public class TokenRefreshMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TokenRefreshMiddleware> _logger;
        private readonly IConfiguration _configuration;

        public TokenRefreshMiddleware(RequestDelegate next, ILogger<TokenRefreshMiddleware> logger, IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                var isAuthenticated = context.Session.GetString("IsAuthenticated") == "true";
                if (isAuthenticated)
                {
                    var token = context.Session.GetString("AccessToken");
                    var refreshToken = context.Session.GetString("RefreshToken");

                    if (!string.IsNullOrEmpty(token))
                    {
                        try
                        {
                            var jwtHandler = new JwtSecurityTokenHandler();
                            var jwtToken = jwtHandler.ReadJwtToken(token);
                            var timeUntilExpiry = jwtToken.ValidTo - DateTime.UtcNow;

                            // If token expires within 5 minutes, attempt automatic refresh
                            if (timeUntilExpiry < TimeSpan.FromMinutes(5) && !string.IsNullOrEmpty(refreshToken))
                            {
                                _logger.LogInformation("Token expires in {Minutes} minutes, attempting automatic refresh", timeUntilExpiry.TotalMinutes);
                                
                                var newTokens = await TryRefreshTokenAsync(token, refreshToken, context);
                                if (newTokens != null)
                                {
                                    context.Session.SetString("AccessToken", newTokens.Value.accessToken);
                                    context.Session.SetString("RefreshToken", newTokens.Value.refreshToken);
                                    _logger.LogInformation("Token automatically refreshed successfully");
                                }
                                else
                                {
                                    _logger.LogWarning("Automatic token refresh failed, user may need to re-login");
                                    context.Session.SetString("TokenWarning", "true");
                                }
                            }
                            else if (jwtToken.ValidTo < DateTime.UtcNow)
                            {
                                // Token is already expired
                                _logger.LogWarning("Token has expired for user session");
                                context.Session.SetString("TokenWarning", "expired");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error checking/refreshing token");
                            context.Session.Remove("AccessToken");
                            context.Session.Remove("RefreshToken");
                            context.Session.SetString("IsAuthenticated", "false");
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

        private async Task<(string accessToken, string refreshToken)?> TryRefreshTokenAsync(string token, string refreshToken, HttpContext context)
        {
            try
            {
                var baseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:5003";
                using var httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri(baseUrl);

                var refreshRequest = new
                {
                    Token = token,
                    RefreshToken = refreshToken
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(refreshRequest),
                    Encoding.UTF8,
                    "application/json");

                var response = await httpClient.PostAsync("/api/account/refresh-token", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<RefreshTokenResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (result?.Success == true && !string.IsNullOrEmpty(result.Token) && !string.IsNullOrEmpty(result.RefreshToken))
                    {
                        return (result.Token, result.RefreshToken);
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return null;
            }
        }

        private class RefreshTokenResponse
        {
            public bool Success { get; set; }
            public string? Token { get; set; }
            public string? RefreshToken { get; set; }
            public int ExpiresIn { get; set; }
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
