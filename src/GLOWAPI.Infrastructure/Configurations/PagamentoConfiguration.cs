using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class PagamentoConfiguration : IEntityTypeConfiguration<Pagamento>
{
    public void Configure(EntityTypeBuilder<Pagamento> builder)
    {
        builder.ToTable("Pagamentos", table =>
        {
            table.HasCheckConstraint(
                "CK_Pagamentos_Origem",
                """(("AgendamentoId" IS NOT NULL AND "AssinaturaId" IS NULL) OR ("AgendamentoId" IS NULL AND "AssinaturaId" IS NOT NULL))""");
        });

        builder.HasKey(pagamento => pagamento.Id);

        builder.Property(pagamento => pagamento.Gateway)
            .HasConversion(gateway => gateway.ToString(), gateway => Enum.Parse<GatewayPagamento>(gateway))
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(pagamento => pagamento.GatewayPaymentId)
            .HasMaxLength(150);

        builder.Property(pagamento => pagamento.MetodoPagamento)
            .HasMaxLength(80);

        builder.Property(pagamento => pagamento.Status)
            .HasConversion(status => status.ToString(), status => Enum.Parse<PagamentoStatus>(status))
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(pagamento => pagamento.Valor)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(pagamento => pagamento.Moeda)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(pagamento => pagamento.CreateAd)
            .IsRequired();

        builder.Property(pagamento => pagamento.TipoCobranca)
            .HasConversion(
                tipo => tipo.HasValue ? tipo.Value.ToString() : null,
                tipo => tipo == null ? null : Enum.Parse<TipoCobrancaAssinatura>(tipo))
            .HasMaxLength(30);

        builder.Property(pagamento => pagamento.NumeroCiclo)
            .HasDefaultValue(1);

        builder.Property(pagamento => pagamento.DataVencimento);
        builder.Property(pagamento => pagamento.DataGeracao);
        builder.Property(pagamento => pagamento.CicloInicio);
        builder.Property(pagamento => pagamento.CicloFim);
        builder.Property(pagamento => pagamento.UpdatedAt);
        builder.Property(pagamento => pagamento.PagoEm);
        builder.Property(pagamento => pagamento.ExpiraEm);

        builder.HasOne(pagamento => pagamento.Agendamento)
            .WithMany(agendamento => agendamento.Pagamentos)
            .HasForeignKey(pagamento => pagamento.AgendamentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pagamento => pagamento.Assinatura)
            .WithMany(assinatura => assinatura.Pagamentos)
            .HasForeignKey(pagamento => pagamento.AssinaturaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(pagamento => pagamento.AgendamentoId);
        builder.HasIndex(pagamento => pagamento.AssinaturaId);
        builder.HasIndex(pagamento => pagamento.GatewayPaymentId);
    }
}
