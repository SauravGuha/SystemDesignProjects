

using Microsoft.EntityFrameworkCore;
using UrlShortener.Data.Models;

namespace UrlShortener.Data;

public class ShortUrlDbContext : DbContext
{
    public ShortUrlDbContext(DbContextOptions<ShortUrlDbContext> dbContextOptions)
    : base(dbContextOptions)
    {

    }

    public DbSet<ShortUrl> ShortUrls { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<ShortUrl>()
        .ToTable(nameof(ShortUrl));
        modelBuilder.Entity<ShortUrl>()
        .HasIndex(e => e.ShortCode)
        .IsUnique();
    }
}
