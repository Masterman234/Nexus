using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Domain.Entities;

namespace Nexus.Infrastructure.Configurations;

public class CommitConfiguration : IEntityTypeConfiguration<Commit>
{
    public void Configure(EntityTypeBuilder<Commit> builder)
    {
        builder.ToTable("commits");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Sha).IsRequired().HasMaxLength(40);
        builder.HasIndex(c => c.Sha).IsUnique();

        builder.Property(c => c.Message).IsRequired();
        builder.Property(c => c.AuthorName).IsRequired().HasMaxLength(100);
        builder.Property(c => c.AuthorEmail).IsRequired().HasMaxLength(255);
        builder.Property(c => c.RepositoryName).IsRequired().HasMaxLength(255);
        builder.Property(c => c.CommittedAt).IsRequired();
    }
}
