using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class IpRateLimitBlockConfiguration : IEntityTypeConfiguration<IpRateLimitBlock>
{
    public void Configure(EntityTypeBuilder<IpRateLimitBlock> builder)
    {
        builder.ToTable("IpRateLimitBlocks");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Ip)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(b => b.Reason)
            .IsRequired()
            .HasMaxLength(120);

        builder.HasIndex(b => new { b.Ip, b.BlockedUntil });
    }
}
