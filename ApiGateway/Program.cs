using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(
                "https://localhost:7280",
                "http://localhost:5018",
                "https://localhost:5003",
                "http://localhost:5003"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .WithExposedHeaders("Authorization"); // Allow Authorization header
    });
});

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

app.UseCors("AllowAll");

// Middleware to log requests and forward JWT tokens
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("=== Ocelot Gateway Request ===");
    logger.LogInformation("Method: {Method}, Path: {Path}", context.Request.Method, context.Request.Path);

    // Log Authorization header
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        var authHeader = context.Request.Headers["Authorization"].ToString();
        logger.LogInformation("Authorization header present: {Header}",
            authHeader.Length > 50 ? authHeader.Substring(0, 50) + "..." : authHeader);
    }
    else
    {
        logger.LogWarning("No Authorization header in request to {Path}", context.Request.Path);
    }

    await next();

    logger.LogInformation("Response status: {Status}", context.Response.StatusCode);
});

await app.UseOcelot();

app.Run();