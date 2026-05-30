using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class AgendamentoHistoricoConfiguration : IEntityTypeConfiguration<AgendamentoHistorico>
{
    public void Configure(EntityTypeBuilder<AgendamentoHistorico> builder)
    {
        builder.ToTable("AgendamentosHistorico");

        builder.HasKey(historico => historico.Id);

        builder.Property(historico => historico.StatusAnterior)
            .HasConversion(status => status.ToString(), status => Enum.Parse<AgendamentoStatus>(status))
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(historico => historico.StatusNovo)
            .HasConversion(status => status.ToString(), status => Enum.Parse<AgendamentoStatus>(status))
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(historico => historico.Motivo)
            .HasMaxLength(500);

        builder.Property(historico => historico.PayloadJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(historico => historico.CriadoEm)
            .IsRequired();

        builder.HasOne(historico => historico.Agendamento)
            .WithMany(agendamento => agendamento.Historico)
            .HasForeignKey(historico => historico.AgendamentoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(historico => historico.UsuarioExecutor)
            .WithMany()
            .HasForeignKey(historico => historico.UsuarioExecutorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(historico => historico.AgendamentoId);
        builder.HasIndex(historico => historico.CriadoEm);
    }
}
