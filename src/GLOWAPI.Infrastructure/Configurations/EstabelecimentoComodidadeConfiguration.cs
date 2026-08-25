using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class EstabelecimentoComodidadeConfiguration : IEntityTypeConfiguration<EstabelecimentoComodidade>
{
    public void Configure(EntityTypeBuilder<EstabelecimentoComodidade> builder)
    {
        builder.ToTable("EstabelecimentoComodidades");
        builder.HasKey(vinculo => new { vinculo.EstabelecimentoId, vinculo.ComodidadeId });

        builder.HasOne(vinculo => vinculo.Estabelecimento)
            .WithMany(estabelecimento => estabelecimento.Comodidades)
            .HasForeignKey(vinculo => vinculo.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(vinculo => vinculo.Comodidade)
            .WithMany(comodidade => comodidade.Estabelecimentos)
            .HasForeignKey(vinculo => vinculo.ComodidadeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
