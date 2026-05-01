using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class CaixaConfiguration : IEntityTypeConfiguration<Caixa>
{
    public void Configure(EntityTypeBuilder<Caixa> builder)
    {
        builder.ToTable("Caixas", table =>
        {
            table.HasCheckConstraint(
                "CK_Caixas_Titular",
                """(("EstabelecimentoId" IS NOT NULL AND "ProfissionalAutonomoId" IS NULL) OR ("EstabelecimentoId" IS NULL AND "ProfissionalAutonomoId" IS NOT NULL))""");
        });

        builder.HasKey(caixa => caixa.Id);

        builder.Property(caixa => caixa.SaldoTotal)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(caixa => caixa.SaldoDisponivel)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(caixa => caixa.SaldoRetido)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(caixa => caixa.CreateAd)
            .IsRequired();

        builder.Property(caixa => caixa.UpdatedAt);

        builder.HasOne(caixa => caixa.Estabelecimento)
            .WithOne(estabelecimento => estabelecimento.Caixa)
            .HasForeignKey<Caixa>(caixa => caixa.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(caixa => caixa.ProfissionalAutonomo)
            .WithOne(profissional => profissional.Caixa)
            .HasForeignKey<Caixa>(caixa => caixa.ProfissionalAutonomoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(caixa => caixa.EstabelecimentoId)
            .IsUnique();

        builder.HasIndex(caixa => caixa.ProfissionalAutonomoId)
            .IsUnique();
    }
}
