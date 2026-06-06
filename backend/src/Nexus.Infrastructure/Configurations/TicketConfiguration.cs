using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Domain.Entities;

namespace Nexus.Infrastructure.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("tickets");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Number).IsRequired();
        builder.Property(t => t.Title).IsRequired().HasMaxLength(255);
        builder.Property(t => t.Description).HasMaxLength(2000);

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(t => t.Priority)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(t => t.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.CreatorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.AssigneeUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(t => new { t.WorkspaceId, t.Number }).IsUnique();
    }
}
