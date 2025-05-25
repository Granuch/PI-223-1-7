using BLL.Interfaces;
using BLL.Services;
using Mapping.Mapping;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PI_223_1_7.DbContext;
using PI_223_1_7.Patterns.UnitOfWork;
using PL.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDataProtection()
    .SetApplicationName("LibraryApp")
    .PersistKeysToFileSystem(new DirectoryInfo(@"C:\temp\keys"))
    .SetDefaultKeyLifetime(TimeSpan.FromDays(30));

// Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "YourApp.AuthCookie";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromDays(1);
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

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMvcClient", policy => 
                 policy.WithOrigins(
                "https://localhost:7280",  // MVC êë³ºíò
                "http://localhost:5018",   // MVC êë³ºíò HTTP
                "https://localhost:5003",  // Ocelot Gateway
                "http://localhost:5003"    // Ocelot Gateway HTTP
            ).AllowAnyMethod().AllowAnyHeader().AllowCredentials());
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContextService, UserContextService>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<LibraryDbContext>(options =>
                options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=LibratyDb;Trusted_Connection=True;"));
builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddAuthentication("Identity.Application")
    .AddCookie("Identity.Application");
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V2");
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowMvcClient");
app.UseCookiePolicy();
app.UseAuthentication(); // ÏÅÐÅÄ UseAuthorization
app.UseAuthorization();

app.MapControllers();

app.Run();
