using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class MetaConfiguration : IEntityTypeConfiguration<Meta>
{
    public void Configure(EntityTypeBuilder<Meta> builder)
    {
        builder.ToTable("Metas");

        builder.HasKey(meta => meta.Id);

        builder.Property(meta => meta.Nome)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(meta => meta.TipoMeta)
            .HasConversion(tipo => tipo.ToString(), tipo => Enum.Parse<TipoMeta>(tipo))
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(meta => meta.ValorMeta)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(meta => meta.PercentualComissao)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(meta => meta.Ativa)
            .HasDefaultValue(true);

        builder.Property(meta => meta.CreateAd)
            .IsRequired();

        builder.Property(meta => meta.UpdatedAt);

        builder.HasOne(meta => meta.Estabelecimento)
            .WithMany(e => e.Metas)
            .HasForeignKey(meta => meta.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(meta => meta.EstabelecimentoId);
        builder.HasIndex(meta => meta.Ativa);
    }
}
