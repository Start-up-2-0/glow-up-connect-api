using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class MetaProfissionalConfiguration : IEntityTypeConfiguration<MetaProfissional>
{
    public void Configure(EntityTypeBuilder<MetaProfissional> builder)
    {
        builder.ToTable("MetasProfissional");

        builder.HasKey(meta => meta.Id);

        builder.Property(meta => meta.TipoMeta)
            .HasConversion(tipo => tipo.ToString(), tipo => Enum.Parse<TipoMeta>(tipo))
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(meta => meta.QuantidadeAtendimentos);

        builder.Property(meta => meta.ValorFaturamento)
            .HasPrecision(12, 2);

        builder.Property(meta => meta.InicioPeriodo)
            .IsRequired();

        builder.Property(meta => meta.FimPeriodo)
            .IsRequired();

        builder.Property(meta => meta.Status)
            .HasConversion(status => status.ToString(), status => Enum.Parse<MetaProfissionalStatus>(status))
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(meta => meta.CreateAd)
            .IsRequired();

        builder.Property(meta => meta.UpdatedAt);

        builder.HasOne(meta => meta.ProfissionalEstabelecimento)
            .WithMany(vinculo => vinculo.Metas)
            .HasForeignKey(meta => meta.ProfissionalEstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(meta => meta.ProfissionalEstabelecimentoId);
        builder.HasIndex(meta => meta.Status);
    }
}
