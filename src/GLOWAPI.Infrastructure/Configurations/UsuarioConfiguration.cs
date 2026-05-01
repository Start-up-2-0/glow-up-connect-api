using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");

        builder.HasKey(usuario => usuario.Id);

        builder.Property(usuario => usuario.Nome)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(usuario => usuario.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(usuario => usuario.Email)
            .IsUnique();

        builder.Property(usuario => usuario.Telefone)
            .HasMaxLength(20);

        builder.Property(usuario => usuario.Senha)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(usuario => usuario.Role)
            .HasConversion(
                role => role.ToString(),
                role => Enum.Parse<UserRole>(role))
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(usuario => usuario.Tentivas)
            .HasDefaultValue(0);

        builder.Property(usuario => usuario.Ativo)
            .HasDefaultValue(true);

        builder.Property(usuario => usuario.CreateAd)
            .IsRequired();

        builder.Property(usuario => usuario.UpdatedAt);
    }
}
