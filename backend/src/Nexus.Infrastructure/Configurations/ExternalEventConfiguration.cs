using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Domain.Entities;

namespace Nexus.Infrastructure.Configurations;

public class ExternalEventConfiguration : IEntityTypeConfiguration<ExternalEvent>
{
    public void Configure(EntityTypeBuilder<ExternalEvent> builder)
    {
        builder.ToTable("external_events");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Source).IsRequired().HasMaxLength(50);
        builder.Property(e => e.EventType).IsRequired().HasMaxLength(50);
        builder.Property(e => e.Payload).IsRequired();
    }
}
