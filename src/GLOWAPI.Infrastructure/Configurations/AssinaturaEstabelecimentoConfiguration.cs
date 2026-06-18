using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class AssinaturaEstabelecimentoConfiguration : IEntityTypeConfiguration<AssinaturaEstabelecimento>
{
    public void Configure(EntityTypeBuilder<AssinaturaEstabelecimento> builder)
    {
        builder.ToTable("AssinaturaEstabelecimentos");

        builder.HasKey(vinculo => vinculo.Id);

        builder.Property(vinculo => vinculo.EhMatriz)
            .HasDefaultValue(false);

        builder.Property(vinculo => vinculo.CreateAd)
            .IsRequired();

        builder.HasIndex(vinculo => vinculo.EstabelecimentoId)
            .IsUnique();

        builder.HasOne(vinculo => vinculo.Assinatura)
            .WithMany()
            .HasForeignKey(vinculo => vinculo.AssinaturaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(vinculo => vinculo.Estabelecimento)
            .WithMany()
            .HasForeignKey(vinculo => vinculo.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
