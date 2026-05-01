using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class ProfissionalEstabelecimentoConfiguration : IEntityTypeConfiguration<ProfissionalEstabelecimento>
{
    public void Configure(EntityTypeBuilder<ProfissionalEstabelecimento> builder)
    {
        builder.ToTable("ProfissionalEstabelecimentos");

        builder.HasKey(profissionalEstabelecimento => profissionalEstabelecimento.Id);

        builder.Property(profissionalEstabelecimento => profissionalEstabelecimento.Ativo)
            .HasDefaultValue(true);

        builder.Property(profissionalEstabelecimento => profissionalEstabelecimento.DataEntrada)
            .IsRequired();

        builder.Property(profissionalEstabelecimento => profissionalEstabelecimento.DataSaida);

        builder.Property(profissionalEstabelecimento => profissionalEstabelecimento.PodeReceberAgendamento)
            .HasDefaultValue(true);

        builder.Property(profissionalEstabelecimento => profissionalEstabelecimento.CreateAd)
            .IsRequired();

        builder.Property(profissionalEstabelecimento => profissionalEstabelecimento.UpdatedAt);

        builder.HasOne(profissionalEstabelecimento => profissionalEstabelecimento.Profissional)
            .WithMany(profissional => profissional.Estabelecimentos)
            .HasForeignKey(profissionalEstabelecimento => profissionalEstabelecimento.ProfissionalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(profissionalEstabelecimento => profissionalEstabelecimento.Estabelecimento)
            .WithMany(estabelecimento => estabelecimento.Profissionais)
            .HasForeignKey(profissionalEstabelecimento => profissionalEstabelecimento.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(profissionalEstabelecimento => profissionalEstabelecimento.ProfissionalId);

        builder.HasIndex(profissionalEstabelecimento => profissionalEstabelecimento.EstabelecimentoId);
    }
}
