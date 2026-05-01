using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class ProfissionalServicoConfiguration : IEntityTypeConfiguration<ProfissionalServico>
{
    public void Configure(EntityTypeBuilder<ProfissionalServico> builder)
    {
        builder.ToTable("ProfissionalServicos");

        builder.HasKey(profissionalServico => profissionalServico.Id);

        builder.Property(profissionalServico => profissionalServico.Preco)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(profissionalServico => profissionalServico.DuracaoMinutos)
            .IsRequired();

        builder.Property(profissionalServico => profissionalServico.Ativo)
            .HasDefaultValue(true);

        builder.Property(profissionalServico => profissionalServico.CreateAd)
            .IsRequired();

        builder.Property(profissionalServico => profissionalServico.UpdatedAt);

        builder.HasOne(profissionalServico => profissionalServico.Profissional)
            .WithMany(profissional => profissional.Servicos)
            .HasForeignKey(profissionalServico => profissionalServico.ProfissionalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(profissionalServico => profissionalServico.Servico)
            .WithMany(servico => servico.Profissionais)
            .HasForeignKey(profissionalServico => profissionalServico.ServicoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(profissionalServico => new
            {
                profissionalServico.ProfissionalId,
                profissionalServico.ServicoId
            })
            .IsUnique();
    }
}
