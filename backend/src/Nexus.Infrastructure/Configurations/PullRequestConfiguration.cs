using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexus.Domain.Entities;

namespace Nexus.Infrastructure.Configurations;

public class PullRequestConfiguration : IEntityTypeConfiguration<PullRequest>
{
    public void Configure(EntityTypeBuilder<PullRequest> builder)
    {
        builder.ToTable("pull_requests");
        builder.HasKey(pr => pr.Id);

        builder.Property(pr => pr.ExternalId).IsRequired();
        builder.HasIndex(pr => pr.ExternalId).IsUnique();

        builder.Property(pr => pr.Title).IsRequired().HasMaxLength(255);
        builder.Property(pr => pr.State).IsRequired().HasMaxLength(20);
        builder.Property(pr => pr.RepositoryName).IsRequired().HasMaxLength(255);
        builder.Property(pr => pr.AuthorName).IsRequired().HasMaxLength(100);
        builder.Property(pr => pr.Url).IsRequired().HasMaxLength(500);
    }
}
