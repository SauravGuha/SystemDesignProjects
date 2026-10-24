
using ChatApplication.Api.Persistence;
using ChatApplication.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace ChatApplication.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();
        builder.Services.AddDbContext<ChatApplicationDbContext>(builderOptions =>
        {
            builderOptions.UseSqlServer(builder.Configuration.GetConnectionString("chatappdb"));
        });
        builder.Services.AddScoped<ConversationService>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ChatApplicationDbContext>();
            ChatDbSeeder.SeedAsync(dbContext)
            .GetAwaiter()
            .GetResult();
        }

        app.Run();
    }
}
