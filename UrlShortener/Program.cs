
using Microsoft.EntityFrameworkCore;
using UrlShortener.Data;
using UrlShortener.Interfaces;
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
        builder.Services.AddDbContext<ShortUrlDbContext>(options =>
        {
            options.UseSqlServer(builder.Configuration.GetConnectionString("shorturl"));
        });
        builder.Services.AddScoped<IShortCodeGenerator, HashShortCodeGenerator>();


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

        app.UseAuthorization();
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
