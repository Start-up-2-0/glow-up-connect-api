using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class ProfissionalConfiguration : IEntityTypeConfiguration<Profissional>
{
    public void Configure(EntityTypeBuilder<Profissional> builder)
    {
        builder.ToTable("Profissionais");

        builder.HasKey(profissional => profissional.Id);

        builder.Property(profissional => profissional.PublicGuid)
            .IsRequired();

        builder.HasIndex(profissional => profissional.PublicGuid)
            .IsUnique();

        builder.Property(profissional => profissional.NomePublico)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(profissional => profissional.Biografia)
            .HasMaxLength(1000);

        builder.Property(profissional => profissional.Logo)
            .HasColumnType("text");

        builder.Property(profissional => profissional.Telefone)
            .HasMaxLength(20);

        builder.Property(profissional => profissional.Email)
            .HasMaxLength(255);

        builder.Property(profissional => profissional.TipoProfissional)
            .HasConversion(
                tipo => tipo.ToString(),
                tipo => Enum.Parse<ProfessionalType>(tipo))
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(profissional => profissional.Ativo)
            .HasDefaultValue(true);

        builder.Property(profissional => profissional.CreateAd)
            .IsRequired();

        builder.Property(profissional => profissional.UpdatedAt);

        builder.HasOne(profissional => profissional.Usuario)
            .WithOne()
            .HasForeignKey<Profissional>(profissional => profissional.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasIndex(profissional => profissional.UsuarioId)
            .IsUnique()
            .HasFilter("`UsuarioId` IS NOT NULL");
    }
}
