using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Domain.Entities;

namespace Nexus.Infrastructure.Configurations;

public class TicketStatusChangeConfiguration : IEntityTypeConfiguration<TicketStatusChange>
{
    public void Configure(EntityTypeBuilder<TicketStatusChange> builder)
    {
        builder.ToTable("ticket_status_changes");
        builder.HasKey(tsc => tsc.Id);

        builder.Property(tsc => tsc.OldStatus)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(tsc => tsc.NewStatus)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasOne<Ticket>()
            .WithMany()
            .HasForeignKey(tsc => tsc.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(tsc => tsc.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
