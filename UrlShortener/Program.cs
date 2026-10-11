
using System.Net;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using UrlShortener.Data;
using UrlShortener.Interfaces;
using UrlShortener.Middleware;
using UrlShortener.RateLimiters;
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
        // builder.Services.AddRateLimiter(options =>
        // {
        //     options.RejectionStatusCode = (int)HttpStatusCode.TooManyRequests;
        //     options.AddFixedWindowLimiter("fixed", f =>
        //     {
        //         f.Window = TimeSpan.FromSeconds(10);
        //         f.PermitLimit = 3;
        //         f.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
        //     });
        // });
        builder.Services.AddScoped<IShortCodeGenerator, HashShortCodeGenerator>();
        builder.Services.AddSingleton<HeaderMiddleware>();
        builder.Services.AddSingleton<ExceptionMiddleware>();
        builder.Services.AddSingleton(new FixedWindowRateLimiterOptions
        {
            PermitLimit = 3,
            Window = TimeSpan.FromSeconds(10)
        });
        builder.Services.AddSingleton<IRateLimiter, FixedWindowLimiter>();
        builder.Services.AddSingleton<AppRateLimiterMiddleware>();
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

        //app.UseRateLimiter();
        app.UseMiddleware<AppRateLimiterMiddleware>();
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
