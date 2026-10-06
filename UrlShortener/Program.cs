
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using UrlShortener.Data;
using UrlShortener.Interfaces;
using UrlShortener.Middleware;
using UrlShortener.Services;

namespace UrlShortener;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();
        builder.Services.AddHostedService<ConcurrencyStreamSubscriber>();
        builder.Services.AddDbContext<ShortUrlDbContext>(options =>
        {
            options.UseSqlServer(builder.Configuration.GetConnectionString("shorturl"));
        });
        builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var connectionString = builder.Configuration.GetConnectionString("redissocket");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException("redissocket connection not found");
            return ConnectionMultiplexer.Connect(connectionString);
        });
        builder.Services.AddScoped<IShortCodeGenerator, HashShortCodeGenerator>();
        builder.Services.AddSingleton<HeaderMiddleware>();
        builder.Services.AddSingleton<ExceptionMiddleware>();
        builder.Services.AddSingleton<IApplicationCache, ApplicationRedisCache>();
        builder.Services.AddSingleton<IConcurrencyPublisher, ApplicationConcurrencyPublisher>();


        var app = builder.Build();
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }
        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        app.UseMiddleware<ExceptionMiddleware>();
        app.UseAuthorization();
        app.UseMiddleware<HeaderMiddleware>();
        app.MapControllers();

        MigrateAsync(app.Services)
        .GetAwaiter();

        app.Run();
    }

    public static async Task MigrateAsync(IServiceProvider serviceProvider)
    {
        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ShortUrlDbContext>();
            await dbContext.Database.MigrateAsync();
        }
    }
}
