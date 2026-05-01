using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class WebhookPagamentoConfiguration : IEntityTypeConfiguration<WebhookPagamento>
{
    public void Configure(EntityTypeBuilder<WebhookPagamento> builder)
    {
        builder.ToTable("WebhookPagamentos");

        builder.HasKey(webhook => webhook.Id);

        builder.Property(webhook => webhook.Gateway)
            .HasConversion(gateway => gateway.ToString(), gateway => Enum.Parse<GatewayPagamento>(gateway))
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(webhook => webhook.EventId)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(webhook => webhook.EventType)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(webhook => webhook.Payload)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(webhook => webhook.Processado)
            .HasDefaultValue(false);

        builder.Property(webhook => webhook.ErroProcessamento)
            .HasMaxLength(1000);

        builder.Property(webhook => webhook.CreateAd)
            .IsRequired();

        builder.Property(webhook => webhook.ProcessadoEm);

        builder.HasIndex(webhook => new
            {
                webhook.Gateway,
                webhook.EventId
            })
            .IsUnique();
    }
}
