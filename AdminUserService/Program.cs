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
using PL.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// DbContext
builder.Services.AddDbContext<LibraryDbContext>(options =>
    options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=LibratyDb;Trusted_Connection=True;"));

// AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);

// Сервисы
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUserService, UserService>();

// КРИТИЧНО: HttpContextAccessor
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContextService, UserContextService>();

// КРИТИЧНО: Data Protection (ТОЧНО ТАКОЙ ЖЕ как везде)
builder.Services.AddDataProtection()
    .SetApplicationName("LibraryApp")
    .PersistKeysToFileSystem(new DirectoryInfo(@"C:\temp\keys"))
    .SetDefaultKeyLifetime(TimeSpan.FromDays(30));

// КРИТИЧНО: ТОЛЬКО Cookie Authentication (БЕЗ Identity для аутентификации)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = "LibraryApp.AuthCookie";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";

        // КРИТИЧНО: Для API endpoints - НЕ делать редиректы
        options.Events.OnRedirectToLogin = context => {
            if (context.Request.Path.StartsWithSegments("/api") ||
                context.Request.Path.StartsWithSegments("/AdminUsers"))
            {
                context.Response.StatusCode = 401;
                return Task.CompletedTask;
            }
            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = context => {
            if (context.Request.Path.StartsWithSegments("/api") ||
                context.Request.Path.StartsWithSegments("/AdminUsers"))
            {
                context.Response.StatusCode = 403;
                return Task.CompletedTask;
            }
            return Task.CompletedTask;
        };

        // КРИТИЧНО: Детальное логирование для диагностики
        options.Events.OnValidatePrincipal = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

            if (context.Principal?.Identity?.IsAuthenticated == true)
            {
                logger.LogInformation(" Cookie validation SUCCESS: User={User}, Claims={ClaimsCount}",
                    context.Principal.Identity.Name,
                    context.Principal.Claims.Count());
            }
            else
            {
                logger.LogWarning(" Cookie validation FAILED - User not authenticated");
            }

            return Task.CompletedTask;
        };

        options.Events.OnSigningIn = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("User signing in: {User}", context.Principal?.Identity?.Name);
            return Task.CompletedTask;
        };
    });

// Identity ТОЛЬКО для UserService (НЕ для аутентификации)
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

// КРИТИЧНО: Отключаем Identity cookie authentication
builder.Services.Configure<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme, options =>
{
    // Отключаем Identity cookies полностью
    options.Cookie.Name = "Identity.Application.Disabled";
    options.LoginPath = "/Account/Login";
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = 401;
        return Task.CompletedTask;
    };
});

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseCors("AllowMvcClient");

// Cookie policy
app.UseCookiePolicy(new CookiePolicyOptions
{
    MinimumSameSitePolicy = SameSiteMode.Lax,
    Secure = CookieSecurePolicy.SameAsRequest,
    CheckConsentNeeded = context => false
});

// КРИТИЧНО: Детальное логирование каждого запроса
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("=== ADMINUSERS REQUEST START ===");
    logger.LogInformation("Path: {Path}, Method: {Method}", context.Request.Path, context.Request.Method);

    // Проверяем cookie
    var authCookie = context.Request.Cookies["LibraryApp.AuthCookie"];
    if (!string.IsNullOrEmpty(authCookie))
    {
        logger.LogInformation("LibraryApp.AuthCookie received (length: {Length})", authCookie.Length);

        // Пробуем расшифровать cookie
        try
        {
            var dataProtectionProvider = context.RequestServices.GetRequiredService<IDataProtectionProvider>();
            var protector = dataProtectionProvider.CreateProtector(
                "Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationMiddleware",
                CookieAuthenticationDefaults.AuthenticationScheme,
                "v2");

            var decryptedBytes = protector.Unprotect(authCookie);
            logger.LogInformation("Cookie decryption SUCCESS - Data Protection working");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Cookie decryption FAILED - Data Protection issue");
        }
    }
    else
    {
        logger.LogWarning("LibraryApp.AuthCookie NOT found");
    }

    logger.LogInformation("User.Identity.IsAuthenticated (before): {IsAuth}",
        context.User?.Identity?.IsAuthenticated ?? false);

    await next();

    logger.LogInformation("User.Identity.IsAuthenticated (after): {IsAuth}",
        context.User?.Identity?.IsAuthenticated ?? false);
    logger.LogInformation("Response status: {Status}", context.Response.StatusCode);
    logger.LogInformation("=== ADMINUSERS REQUEST END ===");
});

// ВАЖЛИВО: правильний порядок middleware
app.UseAuthentication();
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