using BLL.Interfaces;
using BLL.Services;
using Mapping.Mapping;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using PI_223_1_7.DbContext;
using PI_223_1_7.Patterns.UnitOfWork;
using PL.Services; // Для UserContextService

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DbContext
builder.Services.AddDbContext<LibraryDbContext>(options =>
    options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=LibratyDb;Trusted_Connection=True;"));

// AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);

// Ваші сервіси
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IBookService, BookService>();

// КРИТИЧНО: HttpContextAccessor та UserContextService
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContextService, UserContextService>();

// Data Protection (ТОЧНО ОДНАКОВИЙ ДЛЯ ВСІХ СЕРВІСІВ)
builder.Services.AddDataProtection()
    .SetApplicationName("LibraryApp") // ТОЧНО ТА Ж НАЗВА
    .PersistKeysToFileSystem(new DirectoryInfo(@"C:\temp\keys")) // ТА Ж ПАПКА
    .SetDefaultKeyLifetime(TimeSpan.FromDays(30));

// Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "YourApp.AuthCookie"; // ТОЧНО ТА Ж НАЗВА
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromDays(1);
        options.SlidingExpiration = true;
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";

        // Для API endpoints
        options.Events.OnRedirectToLogin = context => {
            if (context.Request.Path.StartsWithSegments("/api") && context.Response.StatusCode == 200)
            {
                context.Response.StatusCode = 401;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = context => {
            if (context.Request.Path.StartsWithSegments("/api") && context.Response.StatusCode == 200)
            {
                context.Response.StatusCode = 403;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
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
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Books API V1");
    });
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
app.Run();