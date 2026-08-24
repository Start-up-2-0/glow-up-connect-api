using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class FavoritoClienteConfiguration : IEntityTypeConfiguration<FavoritoCliente>
{
    public void Configure(EntityTypeBuilder<FavoritoCliente> builder)
    {
        builder.ToTable("FavoritosCliente");
        builder.HasKey(favorito => favorito.Id);
        builder.Property(favorito => favorito.CriadoEm).IsRequired();

        builder.HasIndex(favorito => new { favorito.UsuarioClienteId, favorito.EstabelecimentoId, favorito.ProfissionalChave })
            .IsUnique();

        builder.HasOne(favorito => favorito.UsuarioCliente)
            .WithMany(usuario => usuario.Favoritos)
            .HasForeignKey(favorito => favorito.UsuarioClienteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(favorito => favorito.Estabelecimento)
            .WithMany()
            .HasForeignKey(favorito => favorito.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(favorito => favorito.Profissional)
            .WithMany()
            .HasForeignKey(favorito => favorito.ProfissionalId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
