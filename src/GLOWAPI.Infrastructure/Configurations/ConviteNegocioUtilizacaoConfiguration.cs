using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class ConviteNegocioUtilizacaoConfiguration : IEntityTypeConfiguration<ConviteNegocioUtilizacao>
{
    public void Configure(EntityTypeBuilder<ConviteNegocioUtilizacao> builder)
    {
        builder.ToTable("ConvitesNegocioUtilizacoes");

        builder.HasKey(utilizacao => utilizacao.Id);

        builder.HasOne(utilizacao => utilizacao.ConviteNegocio)
            .WithMany(convite => convite.Utilizacoes)
            .HasForeignKey(utilizacao => utilizacao.ConviteNegocioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(utilizacao => utilizacao.Usuario)
            .WithMany()
            .HasForeignKey(utilizacao => utilizacao.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(utilizacao => new { utilizacao.ConviteNegocioId, utilizacao.UsuarioId })
            .IsUnique();
    }
}
