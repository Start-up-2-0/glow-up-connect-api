using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class CategoriaEstabelecimentoConfiguration : IEntityTypeConfiguration<CategoriaEstabelecimento>
{
    public void Configure(EntityTypeBuilder<CategoriaEstabelecimento> builder)
    {
        builder.ToTable("CategoriasEstabelecimento");

        builder.HasKey(categoria => categoria.Id);

        builder.Property(categoria => categoria.Nome)
            .IsRequired()
            .HasMaxLength(120);

        builder.HasIndex(categoria => categoria.Nome)
            .IsUnique();

        builder.Property(categoria => categoria.Ativo)
            .HasDefaultValue(true);

        builder.Property(categoria => categoria.CreateAd)
            .IsRequired();

        builder.Property(categoria => categoria.UpdatedAt);
    }
}