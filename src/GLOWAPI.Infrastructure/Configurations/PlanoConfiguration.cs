using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class PlanoConfiguration : IEntityTypeConfiguration<Plano>
{
    public void Configure(EntityTypeBuilder<Plano> builder)
    {
        builder.ToTable("Planos");

        builder.HasKey(plano => plano.Id);

        builder.Property(plano => plano.Nome)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(plano => plano.Descricao)
            .HasMaxLength(500);

        builder.Property(plano => plano.Preco)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(plano => plano.Periodo)
            .HasConversion(
                periodo => periodo.ToString(),
                periodo => Enum.Parse<PlanoPeriodo>(periodo))
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(plano => plano.Ativo)
            .HasDefaultValue(true);

        builder.Property(plano => plano.CreateAd)
            .IsRequired();

        builder.Property(plano => plano.UpdatedAt);

        builder.HasIndex(plano => plano.Nome)
            .IsUnique();
    }
}
