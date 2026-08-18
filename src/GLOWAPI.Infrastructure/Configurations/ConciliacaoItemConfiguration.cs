using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class ConciliacaoItemConfiguration : IEntityTypeConfiguration<ConciliacaoItem>
{
    public void Configure(EntityTypeBuilder<ConciliacaoItem> builder)
    {
        builder.ToTable("ConciliacaoItens");

        builder.HasKey(item => item.Id);
        builder.Property(item => item.ValorExtrato).HasPrecision(12, 2);
        builder.Property(item => item.DescricaoExtrato).HasMaxLength(500);
        builder.Property(item => item.ReferenciaExtrato).HasMaxLength(200);

        builder.HasOne(item => item.Estabelecimento)
            .WithMany()
            .HasForeignKey(item => item.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(item => item.LancamentoCaixa)
            .WithMany()
            .HasForeignKey(item => item.LancamentoCaixaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(item => item.EstabelecimentoId);
    }
}
