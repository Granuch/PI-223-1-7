using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// КРИТИЧНО: Data Protection с теми же настройками
builder.Services.AddDataProtection()
    .SetApplicationName("LibraryApp")
    .PersistKeysToFileSystem(new DirectoryInfo(@"C:\temp\keys"))
    .SetDefaultKeyLifetime(TimeSpan.FromDays(30));

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMvcClient", policy =>
    {
        policy.WithOrigins(
                "https://localhost:7280",
                "http://localhost:5018",
                "https://localhost:5003",
                "http://localhost:5003"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

app.UseCors("AllowMvcClient");

// ВАЖЛИВО: Правильний порядок middleware
app.Use(async (context, next) =>
{
    // Логування запитів для діагностики
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Ocelot Gateway: {Method} {Path}",
        context.Request.Method, context.Request.Path);

    if (context.Request.Cookies.Count > 0)
    {
        logger.LogInformation("Request has {Count} cookies", context.Request.Cookies.Count);
        foreach (var cookie in context.Request.Cookies)
        {
            logger.LogInformation("Cookie: {Name} = {Value}",
                cookie.Key, cookie.Value.Length > 50 ? cookie.Value.Substring(0, 50) + "..." : cookie.Value);
        }
    }
    else
    {
        logger.LogWarning("No cookies in request to {Path}", context.Request.Path);
    }

    await next();
});

// ВАЖЛИВО: Cookie forwarding middleware ПЕРЕД Ocelot
app.UseMiddleware<CookieForwardingMiddleware>();

await app.UseOcelot();

app.Run();

// Покращений CookieForwardingMiddleware
public class CookieForwardingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CookieForwardingMiddleware> _logger;

    public CookieForwardingMiddleware(RequestDelegate next, ILogger<CookieForwardingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            _logger.LogInformation("=== COOKIE FORWARDING ===");
            _logger.LogInformation("Processing: {Method} {Path}", context.Request.Method, context.Request.Path);

            // Получаем все cookies
            var cookies = context.Request.Cookies;
            _logger.LogInformation("Found {Count} cookies", cookies.Count);

            if (cookies.Count > 0)
            {
                // Создаем Cookie header для downstream
                var cookieHeader = string.Join("; ", cookies.Select(c => $"{c.Key}={c.Value}"));

                // КРИТИЧНО: Добавляем Cookie header в запрос
                context.Request.Headers["Cookie"] = cookieHeader;

                _logger.LogInformation("Added Cookie header: {Header}",
                    cookieHeader.Length > 100 ? cookieHeader.Substring(0, 100) + "..." : cookieHeader);

                // Проверяем наличие auth cookie
                var authCookie = cookies["LibraryApp.AuthCookie"];
                if (!string.IsNullOrEmpty(authCookie))
                {
                    _logger.LogInformation("Auth cookie forwarded (length: {Length})", authCookie.Length);
                }
                else
                {
                    _logger.LogWarning("No LibraryApp.AuthCookie found!");
                }
            }
            else
            {
                _logger.LogWarning("No cookies to forward for {Path}", context.Request.Path);
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CookieForwardingMiddleware");
            await _next(context);
        }
    }
}