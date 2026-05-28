using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class AssinaturaHistoricoConfiguration : IEntityTypeConfiguration<AssinaturaHistorico>
{
    public void Configure(EntityTypeBuilder<AssinaturaHistorico> builder)
    {
        builder.ToTable("AssinaturasHistorico");
        builder.HasKey(historico => historico.Id);

        builder.Property(historico => historico.Evento)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(historico => historico.StatusAnterior)
            .HasConversion(
                status => status.HasValue ? status.Value.ToString() : null,
                status => string.IsNullOrWhiteSpace(status) ? null : Enum.Parse<AssinaturaStatus>(status))
            .HasMaxLength(50);

        builder.Property(historico => historico.StatusNovo)
            .HasConversion(status => status.ToString(), status => Enum.Parse<AssinaturaStatus>(status))
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(historico => historico.Observacao)
            .HasMaxLength(500);

        builder.Property(historico => historico.PayloadJson)
            .HasColumnType("text");

        builder.Property(historico => historico.CreateAd)
            .IsRequired();

        builder.HasOne(historico => historico.Assinatura)
            .WithMany(assinatura => assinatura.Historicos)
            .HasForeignKey(historico => historico.AssinaturaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(historico => historico.Pagamento)
            .WithMany(pagamento => pagamento.AssinaturasHistorico)
            .HasForeignKey(historico => historico.PagamentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(historico => historico.AssinaturaId);
        builder.HasIndex(historico => historico.PagamentoId);
        builder.HasIndex(historico => historico.Evento);
    }
}
