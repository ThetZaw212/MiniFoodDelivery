using Microsoft.EntityFrameworkCore;

namespace Notification.Api.Data;

public class NotificationDbContext(DbContextOptions<NotificationDbContext> options) : DbContext(options)
{
    public DbSet<NotificationMessage> Notifications => Set<NotificationMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationMessage>(message =>
        {
            message.HasKey(x => x.Id);
            message.Property(x => x.Type).HasMaxLength(80);
            message.Property(x => x.Summary).HasMaxLength(400);
            message.Property(x => x.N8nError).HasMaxLength(500);
        });
    }
}

public class NotificationMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = "";
    public string Summary { get; set; } = "";
    public string PayloadJson { get; set; } = "";
    public bool N8nDelivered { get; set; }
    public string? N8nError { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
