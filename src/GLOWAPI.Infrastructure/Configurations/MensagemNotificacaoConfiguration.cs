using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class MensagemNotificacaoConfiguration : IEntityTypeConfiguration<MensagemNotificacao>
{
    public void Configure(EntityTypeBuilder<MensagemNotificacao> builder)
    {
        builder.ToTable("MensagensNotificacao");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Guid)
            .IsRequired();

        builder.HasIndex(m => m.Guid)
            .IsUnique();

        builder.Property(m => m.Canal)
            .HasConversion(
                canal => canal.ToString(),
                canal => Enum.Parse<CanalMensagemNotificacao>(canal))
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(m => m.Destinatario)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(m => m.Assunto)
            .HasMaxLength(500);

        builder.Property(m => m.Conteudo)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(m => m.PayloadJson)
            .IsRequired()
            .HasColumnType("json");

        builder.Property(m => m.Status)
            .HasConversion(
                status => status.ToString(),
                status => Enum.Parse<StatusMensagemNotificacao>(status))
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(m => m.Provedor)
            .HasMaxLength(100);

        builder.Property(m => m.MensagemErro)
            .HasMaxLength(2000);

        builder.Property(m => m.InstanciaWorker)
            .HasMaxLength(128);

        builder.Property(m => m.CriadoEm).IsRequired();

        builder.HasOne(m => m.Estabelecimento)
            .WithMany()
            .HasForeignKey(m => m.EstabelecimentoId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(m => m.Canal);
        builder.HasIndex(m => m.EstabelecimentoId);
        builder.HasIndex(m => new { m.Status, m.AgendadoPara, m.Prioridade, m.CriadoEm });
        builder.HasIndex(m => new { m.Status, m.ProcessamentoIniciadoEm });
    }
}
