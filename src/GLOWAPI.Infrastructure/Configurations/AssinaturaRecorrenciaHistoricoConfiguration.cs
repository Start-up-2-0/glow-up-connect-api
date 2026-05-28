using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class AssinaturaRecorrenciaHistoricoConfiguration : IEntityTypeConfiguration<AssinaturaRecorrenciaHistorico>
{
    public void Configure(EntityTypeBuilder<AssinaturaRecorrenciaHistorico> builder)
    {
        builder.ToTable("AssinaturasRecorrenciasHistorico");
        builder.HasKey(historico => historico.Id);

        builder.Property(historico => historico.Evento)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(historico => historico.Status)
            .IsRequired()
            .HasMaxLength(80);

        builder.Property(historico => historico.Observacao)
            .HasMaxLength(500);

        builder.Property(historico => historico.PayloadJson)
            .HasColumnType("text");

        builder.Property(historico => historico.CreateAd)
            .IsRequired();

        builder.HasOne(historico => historico.Assinatura)
            .WithMany(assinatura => assinatura.RecorrenciasHistorico)
            .HasForeignKey(historico => historico.AssinaturaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(historico => historico.Pagamento)
            .WithMany(pagamento => pagamento.RecorrenciasHistorico)
            .HasForeignKey(historico => historico.PagamentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(historico => historico.AssinaturaId);
        builder.HasIndex(historico => historico.PagamentoId);
        builder.HasIndex(historico => historico.Evento);
    }
}
