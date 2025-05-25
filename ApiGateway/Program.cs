// Оновіть Program.cs в Ocelot проекті

using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Налаштування Data Protection для спільного використання cookies
builder.Services.AddDataProtection()
    .SetApplicationName("LibraryApp") // Однакова назва для всіх сервісів
    .PersistKeysToFileSystem(new DirectoryInfo(@"C:\temp\keys")) // Спільна папка для ключів
    .SetDefaultKeyLifetime(TimeSpan.FromDays(30));

// Налаштування Cookie Authentication (як в Account Service)
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(1);
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.Name = "YourApp.AuthCookie"; // Точно така ж назва як в Account Service

        // Важливо для API Gateway
        options.Events.OnRedirectToLogin = context => {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = 401;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMvcClient", policy =>
    {
        policy.WithOrigins(      
                "https://localhost:7280",  // MVC клієнт
                "http://localhost:5018",   // MVC клієнт HTTP
                "https://localhost:5003",  // Ocelot Gateway
                "http://localhost:5003"    // Ocelot Gateway HTTP
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Services.AddOcelot(builder.Configuration);

// Додайте middleware для forwarding cookies

var app = builder.Build();

app.UseCors("AllowMvcClient");
app.UseMiddleware<CookieForwardingMiddleware>();
// Cookie policy
app.UseCookiePolicy(new CookiePolicyOptions
{
    MinimumSameSitePolicy = SameSiteMode.Lax,
    Secure = CookieSecurePolicy.SameAsRequest
});

app.UseAuthentication();
app.UseAuthorization();

// Додайте middleware для forwarding cookies
app.UseMiddleware<CookieForwardingMiddleware>();

await app.UseOcelot();

app.Run();

// Middleware для forwarding cookies до мікросервісів
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
        // Логуємо cookies для діагностики
        _logger.LogInformation("Request to: {Path}", context.Request.Path);
        _logger.LogInformation("Cookies count: {Count}", context.Request.Cookies.Count);

        foreach (var cookie in context.Request.Cookies)
        {
            _logger.LogInformation("Cookie: {Name} = {Value}", cookie.Key, cookie.Value);
        }

        await _next(context);
    }
}