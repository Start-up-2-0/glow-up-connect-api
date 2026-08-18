using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class AvaliacaoConviteConfiguration : IEntityTypeConfiguration<AvaliacaoConvite>
{
    public void Configure(EntityTypeBuilder<AvaliacaoConvite> builder)
    {
        builder.ToTable("AvaliacoesConvites");

        builder.HasKey(convite => convite.Id);

        builder.HasIndex(convite => convite.TokenPublico)
            .IsUnique();

        builder.HasIndex(convite => convite.AgendamentoId)
            .IsUnique();

        builder.HasOne(convite => convite.Agendamento)
            .WithMany()
            .HasForeignKey(convite => convite.AgendamentoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
