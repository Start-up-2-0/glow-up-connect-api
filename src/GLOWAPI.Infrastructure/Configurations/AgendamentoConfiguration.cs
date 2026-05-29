using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class AgendamentoConfiguration : IEntityTypeConfiguration<Agendamento>
{
    public void Configure(EntityTypeBuilder<Agendamento> builder)
    {
        builder.ToTable("Agendamentos");

        builder.HasKey(agendamento => agendamento.Id);

        builder.Property(agendamento => agendamento.Status)
            .HasConversion(
                status => status.ToString(),
                status => Enum.Parse<AgendamentoStatus>(status))
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(agendamento => agendamento.ValorTotal)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(agendamento => agendamento.Observacao)
            .HasMaxLength(500);

        builder.Property(agendamento => agendamento.CreateAd)
            .IsRequired();

        builder.Property(agendamento => agendamento.UpdatedAt);

        builder.Property(agendamento => agendamento.CanceladoEm);

        builder.HasOne(agendamento => agendamento.UsuarioCliente)
            .WithMany(usuario => usuario.Agendamentos)
            .HasForeignKey(agendamento => agendamento.UsuarioClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(agendamento => agendamento.Estabelecimento)
            .WithMany(estabelecimento => estabelecimento.Agendamentos)
            .HasForeignKey(agendamento => agendamento.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(agendamento => agendamento.UsuarioClienteId);

        builder.HasIndex(agendamento => agendamento.EstabelecimentoId);
    }
}
