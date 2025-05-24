using UI.Middleware;
using UI.Services;

namespace UI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Додавання сервісів
            builder.Services.AddControllersWithViews();

            // ДОДАНО: Це потрібно для роботи IHttpContextAccessor
            builder.Services.AddHttpContextAccessor();

            // Додаємо сесії для збереження стану авторизації
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(8);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.Name = "LibrarySession";
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.SameSite = SameSiteMode.Lax; // Додано для кращої сумісності
            });

            // Налаштування HttpClient для API
            builder.Services.AddHttpClient<IApiService, ApiService>(client =>
            {
                var baseUrl = builder.Configuration["ApiSettings:BaseUrl"];
                if (string.IsNullOrEmpty(baseUrl))
                {
                    baseUrl = "https://localhost:7164";
                    Console.WriteLine($"Warning: ApiSettings:BaseUrl not found. Using default: {baseUrl}");
                }
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromMinutes(5); // Збільшено timeout
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });

            // Реєстрація сервісів (це можна прибрати, бо AddHttpClient вже реєструє)
            // builder.Services.AddScoped<IApiService, ApiService>(); // Закоментуйте цю лінію

            var app = builder.Build();

            // Налаштування pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            // ВАЖЛИВО: правильний порядок middleware
            app.UseSession();
            app.UseTokenRefresh();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}