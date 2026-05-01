using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class HorarioAtendimentoProfissionalConfiguration : IEntityTypeConfiguration<HorarioAtendimentoProfissional>
{
    public void Configure(EntityTypeBuilder<HorarioAtendimentoProfissional> builder)
    {
        builder.ToTable("HorariosAtendimentoProfissional", table =>
        {
            table.HasCheckConstraint("CK_HorariosAtendimentoProfissional_Horario", "\"HoraInicio\" < \"HoraFim\"");
        });

        builder.HasKey(horario => horario.Id);

        builder.Property(horario => horario.DiaSemana)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(horario => horario.HoraInicio)
            .IsRequired();

        builder.Property(horario => horario.HoraFim)
            .IsRequired();

        builder.Property(horario => horario.Ativo)
            .HasDefaultValue(true);

        builder.Property(horario => horario.CreateAd)
            .IsRequired();

        builder.Property(horario => horario.UpdatedAt);

        builder.HasOne(horario => horario.Profissional)
            .WithMany(profissional => profissional.HorariosAtendimento)
            .HasForeignKey(horario => horario.ProfissionalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(horario => horario.Estabelecimento)
            .WithMany(estabelecimento => estabelecimento.HorariosProfissionais)
            .HasForeignKey(horario => horario.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(horario => new
            {
                horario.ProfissionalId,
                horario.EstabelecimentoId,
                horario.DiaSemana,
                horario.HoraInicio,
                horario.HoraFim
            })
            .IsUnique();
    }
}
