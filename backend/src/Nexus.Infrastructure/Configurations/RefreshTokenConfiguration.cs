using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Domain.Entities;

namespace Nexus.Infrastructure.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(rt => rt.Id);

        // Hex-encoded HMAC-SHA256 = 64 chars. Cap at 128 for headroom if we ever
        // switch to SHA-512.
        builder.Property(rt => rt.TokenHash).IsRequired().HasMaxLength(128);
        builder.HasIndex(rt => rt.TokenHash).IsUnique();

        // IPv6 fits in 45 chars (with embedded-IPv4 notation); 64 leaves slack.
        builder.Property(rt => rt.CreatedByIp).HasMaxLength(64);
        builder.Property(rt => rt.UserAgent).HasMaxLength(512);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
