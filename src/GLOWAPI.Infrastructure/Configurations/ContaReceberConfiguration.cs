using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class ContaReceberConfiguration : IEntityTypeConfiguration<ContaReceber>
{
    public void Configure(EntityTypeBuilder<ContaReceber> builder)
    {
        builder.ToTable("ContasReceber");

        builder.HasKey(conta => conta.Id);
        builder.Property(conta => conta.Valor).HasPrecision(12, 2);
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
