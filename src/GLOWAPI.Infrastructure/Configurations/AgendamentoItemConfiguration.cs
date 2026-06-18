using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class AgendamentoItemConfiguration : IEntityTypeConfiguration<AgendamentoItem>
{
    public void Configure(EntityTypeBuilder<AgendamentoItem> builder)
    {
        builder.ToTable("AgendamentoItens", table =>
        {
            table.HasCheckConstraint("CK_AgendamentoItens_Horario", "`Inicio` < `Fim`");
        });

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Inicio)
            .IsRequired();

        builder.Property(item => item.Fim)
            .IsRequired();

        builder.Property(item => item.Valor)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(item => item.Status)
            .HasConversion(
                status => status.ToString(),
                status => Enum.Parse<AgendamentoItemStatus>(status))
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(item => item.CreateAd)
            .IsRequired();

        builder.Property(item => item.UpdatedAt);

        builder.HasOne(item => item.Agendamento)
            .WithMany(agendamento => agendamento.Itens)
            .HasForeignKey(item => item.AgendamentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(item => item.Servico)
            .WithMany(servico => servico.AgendamentoItens)
            .HasForeignKey(item => item.ServicoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(item => item.Profissional)
            .WithMany(profissional => profissional.AgendamentoItens)
            .HasForeignKey(item => item.ProfissionalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(item => item.RepassadoDeProfissional)
            .WithMany(profissional => profissional.AgendamentoItensRepassados)
            .HasForeignKey(item => item.RepassadoDeProfissionalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(item => item.AgendamentoId);

        builder.HasIndex(item => item.ServicoId);

        builder.HasIndex(item => item.ProfissionalId);

        builder.HasIndex(item => item.RepassadoDeProfissionalId);
    }
}
