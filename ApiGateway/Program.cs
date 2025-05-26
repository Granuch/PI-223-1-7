using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

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

app.Use(async (context, next) =>
{
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

app.UseMiddleware<CookieForwardingMiddleware>();

await app.UseOcelot();

app.Run();

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

            var cookies = context.Request.Cookies;
            _logger.LogInformation("Found {Count} cookies", cookies.Count);

            if (cookies.Count > 0)
            {
                var cookieHeader = string.Join("; ", cookies.Select(c => $"{c.Key}={c.Value}"));

                context.Request.Headers["Cookie"] = cookieHeader;

                _logger.LogInformation("Added Cookie header: {Header}",
                    cookieHeader.Length > 100 ? cookieHeader.Substring(0, 100) + "..." : cookieHeader);

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