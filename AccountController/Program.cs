using BLL.Interfaces;
using BLL.Services;
using Mapping.Mapping;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PI_223_1_7.DbContext;
using PI_223_1_7.Models;
using PI_223_1_7.Patterns.UnitOfWork;
using PL.Controllers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddDbContext<LibraryDbContext>(options =>
                options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=LibratyDb;Trusted_Connection=True;"));
builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUserService, UserService>();

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

builder.Services.ConfigureApplicationCookie(options => {
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(1); // �������� ����� 䳿 ��� ����������

    // ������� ������������ ��� ��������� ������ � API
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax; // Lax �������� ���������� ���� ��� ������� �� ����������

    // ���� �� ������������� HTTPS:
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // ��� ��������

    // ������������ ������� ���� ��� cookie, ��� �������� �������� � ������ ������������
    options.Cookie.Name = "YourApp.AuthCookie";

    // ������� ��� API - �������� ��������� ������ 401 Unauthorized �� ������ �볺���
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

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMvcClient",
        builder => builder
            .WithOrigins("https://localhost:7280", "http://localhost:5018") // URL MVC �������
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => false;
    options.MinimumSameSitePolicy = SameSiteMode.None;
    options.Secure = CookieSecurePolicy.SameAsRequest;
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await SeedDemoData.SeedData(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "������� ��� ������ ����-�����.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
app.UseStaticFiles();
app.UseHttpsRedirection();

// ���������� ������� MIDDLEWARE!
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseCors("AllowMvcClient");
// �������� ��������������, ���� �����������
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

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
        logger.LogError(ex, "������� ��� ����������� ����� �� ������������.");
    }
}

app.Run();
        
    