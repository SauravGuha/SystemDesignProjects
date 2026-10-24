using ChatApplication.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatApplication.Api.Persistence;

public static class ChatDbSeeder
{
    public static async Task SeedAsync(
        ChatApplicationDbContext dbContext)
    {
        if (await dbContext.Users.AnyAsync())
            return;

        dbContext.Users.AddRange(
            new User
            {
                Id = Guid.Parse(
                    "11111111-1111-1111-1111-111111111111"),
                DisplayName = "Alice"
            },
            new User
            {
                Id = Guid.Parse(
                    "22222222-2222-2222-2222-222222222222"),
                DisplayName = "Bob"
            },
            new User
            {
                Id = Guid.Parse(
                    "33333333-3333-3333-3333-333333333333"),
                DisplayName = "Charlie"
            });

        await dbContext.SaveChangesAsync();
    }
}