using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Domain.Entities;

namespace Nexus.Infrastructure.Configurations;

public class IncidentConfiguration : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        builder.ToTable("incidents");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Title).IsRequired().HasMaxLength(255);
        builder.Property(i => i.Description).HasMaxLength(2000);

        builder.Property(i => i.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(i => i.Severity)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(i => i.PostmortemContent).HasColumnType("text");

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(i => i.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(i => i.DeclaredByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Channel>()
            .WithMany()
            .HasForeignKey(i => i.DedicatedChannelId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
