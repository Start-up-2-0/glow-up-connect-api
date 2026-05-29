using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class CaixaConfiguration : IEntityTypeConfiguration<Caixa>
{
    public void Configure(EntityTypeBuilder<Caixa> builder)
    {
        builder.ToTable("Caixas");

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

        builder.HasIndex(caixa => caixa.EstabelecimentoId)
            .IsUnique();
    }
}
