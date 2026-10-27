
using ChatApplication.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatApplication.Api.Persistence;

public class ChatApplicationDbContext : DbContext
{
    public ChatApplicationDbContext(DbContextOptions<ChatApplicationDbContext> contextOptions)
    : base(contextOptions)
    {

    }

    public DbSet<User> Users { get; set; }

    public DbSet<Message> Messages { get; set; }

    public DbSet<Conversation> Conversations { get; set; }

    public DbSet<ConversationMember> ConversationMembers { get; set; }

    public DbSet<IdempotencyRequest> IdempotencyRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(u =>
        {
            u.ToTable(nameof(User));

            u.HasKey(e => e.Id);

            u.Property(e => e.DisplayName).IsRequired();

            u.HasIndex(e => e.DisplayName).IsUnique();

        });

        modelBuilder.Entity<Conversation>(c =>
        {
            c.ToTable(nameof(Conversation));

            c.HasKey(e => e.Id);

            c.Property(c => c.ConversationType).HasMaxLength(100)
            .HasConversion(e => e.ToString(), e => (ConversationType)Enum.Parse(typeof(ConversationType), e));

            c.Property(e => e.Name).HasMaxLength(500).IsRequired();

            c.Property(c => c.CreatedBy).HasMaxLength(500);

            c.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.DirectConversationUser1Id);

            c.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.DirectConversationUser2Id);

            c.HasIndex(e => new { e.DirectConversationUser1Id, e.DirectConversationUser2Id })
            .IsUnique()
            .HasFilter("[ConversationType] = N'Direct' " +
                        "AND [DirectConversationUser1Id] IS NOT NULL " +
                        "AND [DirectConversationUser2Id] IS NOT NULL");
        });

        modelBuilder.Entity<Message>(m =>
        {
            m.ToTable(nameof(Message));

            m.HasKey(e => e.Id);

            m.HasOne<Conversation>()
            .WithMany()
            .HasForeignKey(e => e.ConversationId)
            .IsRequired();

            m.HasOne(e => e.Sender)
            .WithMany()
            .HasForeignKey(e => e.SenderId)
            .IsRequired();

            m.Property(e => e.Content).HasMaxLength(2000).IsRequired();

            m.HasIndex(e => new { e.ClientMessageId, e.SenderId })
            .IsUnique();
        });

        modelBuilder.Entity<ConversationMember>(e =>
        {
            e.ToTable(nameof(ConversationMember));

            e.HasKey(e => new { e.UserId, e.ConversationId });

            e.HasOne<Conversation>()
            .WithMany()
            .HasForeignKey(cm => cm.ConversationId)
            .IsRequired();

            e.HasOne<User>()
            .WithMany()
            .HasForeignKey(cm => cm.UserId)
            .IsRequired();
        });

        modelBuilder.Entity<IdempotencyRequest>(ir =>
        {
            ir.ToTable(nameof(IdempotencyRequest));

            ir.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId);

            ir.Property(e => e.Operation)
            .HasMaxLength(500)
            .IsRequired();

            ir.Property(e => e.Key)
            .HasMaxLength(500)
            .IsRequired();

            ir.Property(e => e.RequestHash)
            .HasMaxLength(500)
            .IsRequired();

            ir.Property(e => e.ResponseBody)
            .HasMaxLength(500)
            .IsRequired();

            ir.HasIndex(e => new { e.UserId, e.Operation, e.Key })
            .IsUnique();
        });

        base.OnModelCreating(modelBuilder);
    }
}