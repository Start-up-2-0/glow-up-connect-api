using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class MensagemNotificacaoLogConfiguration : IEntityTypeConfiguration<MensagemNotificacaoLog>
{
    public void Configure(EntityTypeBuilder<MensagemNotificacaoLog> builder)
    {
        builder.ToTable("MensagensNotificacaoLogs");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.RequestPayload)
            .HasColumnType("text");

        builder.Property(l => l.ResponsePayload)
            .HasColumnType("text");

        builder.Property(l => l.RespostaProvedor)
            .HasMaxLength(2000);

        builder.Property(l => l.Status)
            .HasConversion(
                status => status.ToString(),
                status => Enum.Parse<StatusMensagemNotificacao>(status))
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(l => l.MensagemErro)
            .HasMaxLength(2000);

        builder.Property(l => l.CriadoEm).IsRequired();

        builder.HasOne(l => l.MensagemNotificacao)
            .WithMany(m => m.Logs)
            .HasForeignKey(l => l.MensagemNotificacaoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => l.MensagemNotificacaoId);
        builder.HasIndex(l => l.CriadoEm);
    }
}
