using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class AssinaturaConfiguration : IEntityTypeConfiguration<Assinatura>
{
    public void Configure(EntityTypeBuilder<Assinatura> builder)
    {
        builder.ToTable("Assinaturas");

        builder.HasKey(assinatura => assinatura.Id);

        builder.Property(assinatura => assinatura.Status)
            .HasConversion(status => status.ToString(), status => Enum.Parse<AssinaturaStatus>(status))
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(assinatura => assinatura.StatusAntesExclusao)
            .HasConversion(
                status => status.HasValue ? status.Value.ToString() : null,
                status => string.IsNullOrEmpty(status) ? null : Enum.Parse<AssinaturaStatus>(status))
            .HasMaxLength(50);

        builder.Property(assinatura => assinatura.TipoAssinatura)
            .HasConversion(tipo => tipo.ToString(), tipo => Enum.Parse<TipoAssinatura>(tipo))
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(assinatura => assinatura.Gateway)
            .HasConversion(gateway => gateway.ToString(), gateway => Enum.Parse<GatewayPagamento>(gateway))
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(assinatura => assinatura.GatewaySubscriptionId)
            .HasMaxLength(150);

        builder.Property(assinatura => assinatura.GatewayCustomerId)
            .HasMaxLength(150);

        builder.Property(assinatura => assinatura.RenovacaoAutomatica)
            .HasDefaultValue(true);

        builder.Property(assinatura => assinatura.Inicio)
            .IsRequired();

        builder.Property(assinatura => assinatura.DiaVencimento)
            .IsRequired();

        builder.Property(assinatura => assinatura.DataReferenciaCiclo)
            .IsRequired();

        builder.Property(assinatura => assinatura.PercentualDescontoPermanente)
            .HasPrecision(5, 2);

        builder.Property(assinatura => assinatura.ProximaDataVencimento);
        builder.Property(assinatura => assinatura.ProximaDataGeracaoCobranca);
        builder.Property(assinatura => assinatura.ProximaDataAlerta);
        builder.Property(assinatura => assinatura.UltimoAlertaFaturaEm);
        builder.Property(assinatura => assinatura.Fim);
        builder.Property(assinatura => assinatura.CanceladoEm);

        builder.Property(assinatura => assinatura.OnboardingPendenteJson)
            .HasColumnType("text");

        builder.Property(assinatura => assinatura.CreateAd)
            .IsRequired();

        builder.Property(assinatura => assinatura.UpdatedAt);

        builder.HasOne(assinatura => assinatura.Plano)
            .WithMany(plano => plano.Assinaturas)
            .HasForeignKey(assinatura => assinatura.PlanoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(assinatura => assinatura.PlanoAlteracaoPendente)
            .WithMany()
            .HasForeignKey(assinatura => assinatura.PlanoAlteracaoPendenteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(assinatura => assinatura.Estabelecimento)
            .WithMany(estabelecimento => estabelecimento.Assinaturas)
            .HasForeignKey(assinatura => assinatura.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(assinatura => assinatura.CampanhaPromocional)
            .WithMany(campanha => campanha.Assinaturas)
            .HasForeignKey(assinatura => assinatura.CampanhaPromocionalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(assinatura => assinatura.PlanoId);
        builder.HasIndex(assinatura => assinatura.CampanhaPromocionalId);
        builder.HasIndex(assinatura => assinatura.PlanoAlteracaoPendenteId);
        builder.HasIndex(assinatura => assinatura.EstabelecimentoId);
    }
}
