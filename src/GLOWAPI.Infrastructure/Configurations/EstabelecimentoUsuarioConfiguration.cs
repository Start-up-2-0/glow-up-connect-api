using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class EstabelecimentoUsuarioConfiguration : IEntityTypeConfiguration<EstabelecimentoUsuario>
{
    public void Configure(EntityTypeBuilder<EstabelecimentoUsuario> builder)
    {
        builder.ToTable("EstabelecimentoUsuarios");

        builder.HasKey(estabelecimentoUsuario => estabelecimentoUsuario.Id);

        builder.Property(estabelecimentoUsuario => estabelecimentoUsuario.RoleNoEstabelecimento)
            .HasConversion(
                role => role.ToString(),
                role => Enum.Parse<EstablishmentUserRole>(role))
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(estabelecimentoUsuario => estabelecimentoUsuario.Ativo)
            .HasDefaultValue(true);

        builder.Property(estabelecimentoUsuario => estabelecimentoUsuario.CreateAd)
            .IsRequired();

        builder.Property(estabelecimentoUsuario => estabelecimentoUsuario.UpdatedAt);

        builder.HasOne(estabelecimentoUsuario => estabelecimentoUsuario.Estabelecimento)
            .WithMany(estabelecimento => estabelecimento.Usuarios)
            .HasForeignKey(estabelecimentoUsuario => estabelecimentoUsuario.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(estabelecimentoUsuario => estabelecimentoUsuario.Usuario)
            .WithMany(usuario => usuario.Estabelecimentos)
            .HasForeignKey(estabelecimentoUsuario => estabelecimentoUsuario.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(estabelecimentoUsuario => new
            {
                estabelecimentoUsuario.EstabelecimentoId,
                estabelecimentoUsuario.UsuarioId
            })
            .IsUnique();
    }
}
