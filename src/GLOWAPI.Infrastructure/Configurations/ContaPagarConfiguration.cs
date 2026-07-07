using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class ContaPagarConfiguration : IEntityTypeConfiguration<ContaPagar>
{
    public void Configure(EntityTypeBuilder<ContaPagar> builder)
    {
        builder.ToTable("ContasPagar");

        builder.HasKey(conta => conta.Id);
        builder.Property(conta => conta.Valor).HasPrecision(12, 2);
        builder.Property(conta => conta.Fornecedor).HasMaxLength(200);
        builder.Property(conta => conta.Categoria).HasMaxLength(100);
        builder.Property(conta => conta.Descricao).HasMaxLength(500);

        builder.Property(conta => conta.Status)
            .HasConversion(status => status.ToString(), status => Enum.Parse<ContaFinanceiraStatus>(status))
            .HasMaxLength(20);

        builder.HasOne(conta => conta.Estabelecimento)
            .WithMany()
            .HasForeignKey(conta => conta.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(conta => conta.EstabelecimentoId);
    }
}
