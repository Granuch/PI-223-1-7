using UI.Middleware;
using UI.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// КРИТИЧНО: Data Protection (той самий що в мікросервісах)
builder.Services.AddDataProtection()
    .SetApplicationName("LibraryApp") // ТА Ж НАЗВА
    .PersistKeysToFileSystem(new DirectoryInfo(@"C:\temp\keys")) // ТА Ж ПАПКА
    .SetDefaultKeyLifetime(TimeSpan.FromDays(30));

// Cookie Authentication з ЗБІЛЬШЕНИМ часом життя
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "LibraryApp.AuthCookie"; // ЕДИНАЯ СИСТЕМА ИМЕН
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";

        // Автоматическое продление
        options.Events.OnValidatePrincipal = async context =>
        {
            var timeRemaining = context.Properties.ExpiresUtc.Value - DateTimeOffset.UtcNow;
            if (timeRemaining < TimeSpan.FromHours(2))
            {
                context.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8);
                context.ShouldRenew = true;
            }
        };
    });
// Сесії з ЗБІЛЬШЕНИМ часом життя
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8); // ЗБІЛЬШЕНО до 8 годин
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "LibrarySession";
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// HttpClient з підтримкою cookies
builder.Services.AddHttpClient<IApiService, ApiService>(client =>
{
    var baseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:5003";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
{
    UseCookies = true,
    CookieContainer = new CookieContainer()
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// ВАЖЛИВО: правильний порядок
app.UseSession();
app.UseAuthentication(); // ДОДАНО
app.UseTokenRefresh();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();