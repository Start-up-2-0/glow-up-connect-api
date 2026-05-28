using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class PagamentoHistoricoConfiguration : IEntityTypeConfiguration<PagamentoHistorico>
{
    public void Configure(EntityTypeBuilder<PagamentoHistorico> builder)
    {
        builder.ToTable("PagamentosHistorico");
        builder.HasKey(historico => historico.Id);

        builder.Property(historico => historico.Evento)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(historico => historico.StatusAnterior)
            .HasConversion(
                status => status.HasValue ? status.Value.ToString() : null,
                status => string.IsNullOrWhiteSpace(status) ? null : Enum.Parse<PagamentoStatus>(status))
            .HasMaxLength(50);

        builder.Property(historico => historico.StatusNovo)
            .HasConversion(status => status.ToString(), status => Enum.Parse<PagamentoStatus>(status))
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(historico => historico.Gateway)
            .HasConversion(gateway => gateway.ToString(), gateway => Enum.Parse<GatewayPagamento>(gateway))
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(historico => historico.GatewayPaymentId)
            .HasMaxLength(150);

        builder.Property(historico => historico.MetodoPagamento)
            .HasMaxLength(80);

        builder.Property(historico => historico.Valor)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(historico => historico.Moeda)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(historico => historico.Observacao)
            .HasMaxLength(500);

        builder.Property(historico => historico.PayloadJson)
            .HasColumnType("text");

        builder.Property(historico => historico.CreateAd)
            .IsRequired();

        builder.HasOne(historico => historico.Pagamento)
            .WithMany(pagamento => pagamento.Historicos)
            .HasForeignKey(historico => historico.PagamentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(historico => historico.Assinatura)
            .WithMany()
            .HasForeignKey(historico => historico.AssinaturaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(historico => historico.PagamentoId);
        builder.HasIndex(historico => historico.AssinaturaId);
        builder.HasIndex(historico => historico.Evento);
    }
}
