using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class AvaliacaoHistoricoConfiguration : IEntityTypeConfiguration<AvaliacaoHistorico>
{
    public void Configure(EntityTypeBuilder<AvaliacaoHistorico> builder)
    {
        builder.ToTable("AvaliacoesHistorico");

        builder.HasKey(historico => historico.Id);

        builder.Property(historico => historico.ComentarioEstabelecimentoAnterior)
            .HasMaxLength(1000);

        builder.Property(historico => historico.ComentarioProfissionalAnterior)
            .HasMaxLength(1000);

        builder.Property(historico => historico.Motivo)
            .HasMaxLength(500);

        builder.HasOne(historico => historico.AvaliacaoAtendimento)
            .WithMany()
            .HasForeignKey(historico => historico.AvaliacaoAtendimentoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(historico => historico.AlteradoPorUsuario)
            .WithMany()
            .HasForeignKey(historico => historico.AlteradoPorUsuarioId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
