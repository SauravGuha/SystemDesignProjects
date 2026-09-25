
using System.ComponentModel.DataAnnotations;

namespace UrlShortener.Data.Models;

public class ShortUrl
{
    [Key]
    public Guid Id { get; private set; }

    [MaxLength(10)]
    [Required]
    public string ShortCode { get; private set; }

    [MaxLength(2500)]
    [Required]
    public string LongUrl { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    public int HitCount { get; private set; }

    public ShortUrl(string shortCode, string longUrl)
    {
        Id = new Guid();
        this.ShortCode = shortCode;
        this.LongUrl = longUrl;
        this.CreatedAt = DateTime.UtcNow;
        this.ExpiresAt = this.CreatedAt.AddDays(20);
    }

    public void UpdatehitCount()
    {
        this.HitCount++;
    }
}