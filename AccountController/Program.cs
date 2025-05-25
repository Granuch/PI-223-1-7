using BLL.Interfaces;
using BLL.Services;
using Mapping.Mapping;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PI_223_1_7.DbContext;
using PI_223_1_7.Models;
using PI_223_1_7.Patterns.UnitOfWork;
using PL.Controllers;
using PL.Services; // Для UserContextService

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// DbContext
builder.Services.AddDbContext<LibraryDbContext>(options =>
    options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=LibratyDb;Trusted_Connection=True;"));

// AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);

// Ваші сервіси
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUserService, UserService>();

// Identity (ЗАЛИШАЄМО для UserService, але НЕ для автентифікації)
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<LibraryDbContext>()
.AddDefaultTokenProviders();

// КРИТИЧНО: HttpContextAccessor та UserContextService
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContextService, UserContextService>();

// Data Protection (ТОЧНО ОДНАКОВИЙ ДЛЯ ВСІХ СЕРВІСІВ)
builder.Services.AddDataProtection()
    .SetApplicationName("LibraryApp") // ТОЧНО ТА Ж НАЗВА
    .PersistKeysToFileSystem(new DirectoryInfo(@"C:\temp\keys")) // ТА Ж ПАПКА
    .SetDefaultKeyLifetime(TimeSpan.FromDays(30));

// Cookie Authentication (ПЕРЕВИЗНАЧАЄМО після Identity)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "LibraryApp.AuthCookie"; // ТА ЖЕ НАЗВА
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        options.Events.OnRedirectToLogin = context => {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = 401;
                return Task.CompletedTask;
            }
            return Task.CompletedTask;
        };
    });
// CORS (ВИПРАВЛЕНО)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMvcClient", policy =>
    {
        policy.WithOrigins(
                "https://localhost:7280",  // MVC клієнт HTTPS
                "http://localhost:5018",   // MVC клієнт HTTP
                "https://localhost:5003",  // Ocelot Gateway HTTPS
                "http://localhost:5003"    // Ocelot Gateway HTTP
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

// CORS ПЕРЕД аутентифікацією
app.UseCors("AllowMvcClient");

// Cookie policy
app.UseCookiePolicy(new CookiePolicyOptions
{
    MinimumSameSitePolicy = SameSiteMode.Lax,
    Secure = CookieSecurePolicy.SameAsRequest,
    CheckConsentNeeded = context => false
});

// ВАЖЛИВО: правильний порядок middleware
app.UseAuthentication(); // ПЕРЕД UseAuthorization
app.UseAuthorization();

app.MapControllers();

// Seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        await RoleInitializer.InitializeAsync(userManager, roleManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Помилка при ініціалізації ролей та користувачів.");
    }
}

app.Run();