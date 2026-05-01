using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class ComissaoProfissionalConfiguration : IEntityTypeConfiguration<ComissaoProfissional>
{
    public void Configure(EntityTypeBuilder<ComissaoProfissional> builder)
    {
        builder.ToTable("ComissoesProfissional");

        builder.HasKey(comissao => comissao.Id);

        builder.Property(comissao => comissao.TipoComissao)
            .HasConversion(tipo => tipo.ToString(), tipo => Enum.Parse<TipoComissao>(tipo))
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(comissao => comissao.Percentual)
            .HasPrecision(5, 2);

        builder.Property(comissao => comissao.ValorFixo)
            .HasPrecision(12, 2);

        builder.Property(comissao => comissao.Ativo)
            .HasDefaultValue(true);

        builder.Property(comissao => comissao.InicioVigencia)
            .IsRequired();

        builder.Property(comissao => comissao.FimVigencia);

        builder.Property(comissao => comissao.CreateAd)
            .IsRequired();

        builder.Property(comissao => comissao.UpdatedAt);

        builder.HasOne(comissao => comissao.ProfissionalEstabelecimento)
            .WithMany(vinculo => vinculo.Comissoes)
            .HasForeignKey(comissao => comissao.ProfissionalEstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(comissao => comissao.ProfissionalEstabelecimentoId);
    }
}
