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

            // Додаємо сесії для збереження стану авторизації
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30); // Сесія діє 30 хвилин
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.Name = "LibrarySession";
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            });

            // Налаштування HttpClient для API (спрощена версія)
            builder.Services.AddHttpClient<IApiService, ApiService>(client =>
            {
                var baseUrl = builder.Configuration["ApiSettings:BaseUrl"];
                if (string.IsNullOrEmpty(baseUrl))
                {
                    baseUrl = "https://localhost:7164";
                    Console.WriteLine($"Warning: ApiSettings:BaseUrl not found. Using default: {baseUrl}");
                }
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            // Реєстрація сервісів
            builder.Services.AddScoped<IApiService, ApiService>();

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

            // ВАЖЛИВО: UseSession має бути перед MapControllerRoute
            app.UseSession();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}