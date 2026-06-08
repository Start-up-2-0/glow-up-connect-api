using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class AgendamentoPropostaRemarcacaoConfiguration : IEntityTypeConfiguration<AgendamentoPropostaRemarcacao>
{
    public void Configure(EntityTypeBuilder<AgendamentoPropostaRemarcacao> builder)
    {
        builder.ToTable("AgendamentosPropostasRemarcacao");

        builder.HasKey(proposta => proposta.Id);

        builder.Property(proposta => proposta.Motivo)
            .HasMaxLength(500)
            .IsRequired();

        builder.HasIndex(proposta => proposta.TokenPublico)
            .IsUnique();

        builder.HasIndex(proposta => proposta.AgendamentoId);

        builder.HasOne(proposta => proposta.Agendamento)
            .WithMany()
            .HasForeignKey(proposta => proposta.AgendamentoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(proposta => proposta.UsuarioExecutor)
            .WithMany()
            .HasForeignKey(proposta => proposta.UsuarioExecutorId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
