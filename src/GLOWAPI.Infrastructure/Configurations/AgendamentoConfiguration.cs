using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class AgendamentoConfiguration : IEntityTypeConfiguration<Agendamento>
{
    public void Configure(EntityTypeBuilder<Agendamento> builder)
    {
        builder.ToTable("Agendamentos", table =>
        {
            table.HasCheckConstraint("CK_Agendamentos_Horario", "`Inicio` < `Fim`");
        });

        builder.HasKey(agendamento => agendamento.Id);

        builder.Property(agendamento => agendamento.Status)
            .HasConversion(
                status => status.ToString(),
                status => Enum.Parse<AgendamentoStatus>(status))
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(agendamento => agendamento.Origem)
            .HasConversion(
                origem => origem.ToString(),
                origem => Enum.Parse<OrigemAgendamento>(origem))
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(agendamento => agendamento.ValorTotal)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(agendamento => agendamento.Inicio)
            .IsRequired();

        builder.Property(agendamento => agendamento.Fim)
            .IsRequired();

        builder.Property(agendamento => agendamento.ClienteNome)
            .HasMaxLength(200);

        builder.Property(agendamento => agendamento.ClienteEmail)
            .HasMaxLength(255);

        builder.Property(agendamento => agendamento.ClienteTelefone)
            .HasMaxLength(30);

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

        builder.HasIndex(agendamento => agendamento.Inicio);
    }
}
