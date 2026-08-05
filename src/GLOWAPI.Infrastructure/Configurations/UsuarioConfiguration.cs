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

        builder.Property(usuario => usuario.Sexo)
            .HasConversion(
                sexo => sexo != null ? sexo.Value.ToString() : null,
                sexo => string.IsNullOrEmpty(sexo) ? (Sexo?)null : Enum.Parse<Sexo>(sexo))
            .HasMaxLength(20)
            .IsRequired(false);

        builder.Property(usuario => usuario.Tentativas)
            .HasDefaultValue(0);

        builder.Property(usuario => usuario.BloqueadoAte);

        builder.Property(usuario => usuario.Ativo)
            .HasDefaultValue(true);

        builder.Property(usuario => usuario.AvatarBase64)
            .HasColumnType("longtext");

        builder.Property(usuario => usuario.ConfirmacaoTokenHash)
            .HasMaxLength(128);

        builder.HasIndex(usuario => usuario.ConfirmacaoTokenHash);

        builder.Property(usuario => usuario.ConfirmacaoCodigoHash)
            .HasMaxLength(128);

        builder.HasIndex(usuario => usuario.ConfirmacaoCodigoHash);

        builder.Property(usuario => usuario.ConfirmacaoExpiraEm);

        builder.Property(usuario => usuario.WhatsAppConfirmadoEm);

        builder.Property(usuario => usuario.WhatsAppConfirmacaoTokenHash)
            .HasMaxLength(128);

        builder.HasIndex(usuario => usuario.WhatsAppConfirmacaoTokenHash);

        builder.Property(usuario => usuario.WhatsAppConfirmacaoCodigoHash)
            .HasMaxLength(128);

        builder.HasIndex(usuario => usuario.WhatsAppConfirmacaoCodigoHash);

        builder.Property(usuario => usuario.WhatsAppConfirmacaoExpiraEm);

        builder.Property(usuario => usuario.WhatsAppOptIn)
            .HasDefaultValue(false);

        builder.Property(usuario => usuario.CreatedAt)
            .IsRequired();

        builder.Property(usuario => usuario.UpdatedAt);
    }
}
