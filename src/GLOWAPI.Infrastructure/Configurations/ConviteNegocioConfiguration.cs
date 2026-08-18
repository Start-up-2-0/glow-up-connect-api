using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class ConviteNegocioConfiguration : IEntityTypeConfiguration<ConviteNegocio>
{
    public void Configure(EntityTypeBuilder<ConviteNegocio> builder)
    {
        builder.ToTable("ConvitesNegocio");

        builder.HasKey(convite => convite.Id);

        builder.Property(convite => convite.Email)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(convite => convite.Telefone)
            .HasMaxLength(50);

        builder.Property(convite => convite.NomePublico)
            .HasMaxLength(255);

        builder.Property(convite => convite.TipoConvite)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(convite => convite.RoleSugerida)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(convite => convite.Status)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(convite => convite.TokenHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(convite => convite.TokenProtegido)
            .HasMaxLength(256);

        builder.Property(convite => convite.LimiteUsuarios)
            .IsRequired();

        builder.Property(convite => convite.QuantidadeUtilizacoes)
            .IsRequired();

        builder.HasOne(convite => convite.Estabelecimento)
            .WithMany()
            .HasForeignKey(convite => convite.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(convite => convite.CriadoPorUsuario)
            .WithMany()
            .HasForeignKey(convite => convite.CriadoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(convite => convite.AceitoPorUsuario)
            .WithMany()
            .HasForeignKey(convite => convite.AceitoPorUsuarioId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(convite => convite.TokenHash).IsUnique();
        builder.HasIndex(convite => new { convite.EstabelecimentoId, convite.Status });
    }
}
