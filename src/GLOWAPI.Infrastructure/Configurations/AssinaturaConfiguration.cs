using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class AssinaturaConfiguration : IEntityTypeConfiguration<Assinatura>
{
    public void Configure(EntityTypeBuilder<Assinatura> builder)
    {
        builder.ToTable("Assinaturas", table =>
        {
            table.HasCheckConstraint(
                "CK_Assinaturas_Titular",
                """(("EstabelecimentoId" IS NOT NULL AND "ProfissionalAutonomoId" IS NULL) OR ("EstabelecimentoId" IS NULL AND "ProfissionalAutonomoId" IS NOT NULL))""");
        });

        builder.HasKey(assinatura => assinatura.Id);

        builder.Property(assinatura => assinatura.Status)
            .HasConversion(status => status.ToString(), status => Enum.Parse<AssinaturaStatus>(status))
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

        builder.Property(assinatura => assinatura.Fim);
        builder.Property(assinatura => assinatura.CanceladoEm);

        builder.Property(assinatura => assinatura.CreateAd)
            .IsRequired();

        builder.Property(assinatura => assinatura.UpdatedAt);

        builder.HasOne(assinatura => assinatura.Plano)
            .WithMany(plano => plano.Assinaturas)
            .HasForeignKey(assinatura => assinatura.PlanoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(assinatura => assinatura.Estabelecimento)
            .WithMany(estabelecimento => estabelecimento.Assinaturas)
            .HasForeignKey(assinatura => assinatura.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(assinatura => assinatura.ProfissionalAutonomo)
            .WithMany(profissional => profissional.AssinaturasAutonomo)
            .HasForeignKey(assinatura => assinatura.ProfissionalAutonomoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(assinatura => assinatura.PlanoId);
        builder.HasIndex(assinatura => assinatura.EstabelecimentoId);
        builder.HasIndex(assinatura => assinatura.ProfissionalAutonomoId);
    }
}
