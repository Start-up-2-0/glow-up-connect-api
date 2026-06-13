using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class HorarioFuncionamentoEstabelecimentoConfiguration : IEntityTypeConfiguration<HorarioFuncionamentoEstabelecimento>
{
    public void Configure(EntityTypeBuilder<HorarioFuncionamentoEstabelecimento> builder)
    {
        builder.ToTable("HorariosFuncionamentoEstabelecimento", table =>
        {
            table.HasCheckConstraint("CK_HorariosFuncionamentoEstabelecimento_Horario", "`HoraInicio` < `HoraFim`");
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

        builder.HasOne(horario => horario.Estabelecimento)
            .WithMany(estabelecimento => estabelecimento.HorariosFuncionamento)
            .HasForeignKey(horario => horario.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(horario => new
            {
                horario.EstabelecimentoId,
                horario.DiaSemana,
                horario.HoraInicio,
                horario.HoraFim
            })
            .IsUnique();
    }
}
