using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class CampanhaPromocionalConfiguration : IEntityTypeConfiguration<CampanhaPromocional>
{
    public void Configure(EntityTypeBuilder<CampanhaPromocional> builder)
    {
        builder.ToTable("CampanhasPromocionais");

        builder.HasKey(campanha => campanha.Id);

        builder.Property(campanha => campanha.Codigo)
            .IsRequired()
            .HasMaxLength(80);

        builder.Property(campanha => campanha.Limite)
            .IsRequired();

        builder.Property(campanha => campanha.Utilizados)
            .HasDefaultValue(0);

        builder.Property(campanha => campanha.DiasTrial)
            .IsRequired();

        builder.Property(campanha => campanha.PercentualDescontoMensalidade)
            .HasPrecision(5, 2)
            .HasDefaultValue(50m)
            .IsRequired();

        builder.Property(campanha => campanha.Ativa)
            .HasDefaultValue(true);

        builder.Property(campanha => campanha.CreateAd)
            .IsRequired();

        builder.Property(campanha => campanha.UpdatedAt);

        builder.HasIndex(campanha => campanha.Codigo)
            .IsUnique();
    }
}
