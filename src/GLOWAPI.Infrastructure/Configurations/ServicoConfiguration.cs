using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class ServicoConfiguration : IEntityTypeConfiguration<Servico>
{
    public void Configure(EntityTypeBuilder<Servico> builder)
    {
        builder.ToTable("Servicos", table =>
        {
            table.HasCheckConstraint(
                "CK_Servicos_Titular",
                """(("EstabelecimentoId" IS NOT NULL AND "ProfissionalAutonomoId" IS NULL) OR ("EstabelecimentoId" IS NULL AND "ProfissionalAutonomoId" IS NOT NULL))""");
        });

        builder.HasKey(servico => servico.Id);

        builder.Property(servico => servico.Nome)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(servico => servico.Descricao)
            .HasMaxLength(500);

        builder.Property(servico => servico.PrecoBase)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(servico => servico.DuracaoMinutos)
            .IsRequired();

        builder.Property(servico => servico.Ativo)
            .HasDefaultValue(true);

        builder.Property(servico => servico.CreateAd)
            .IsRequired();

        builder.Property(servico => servico.UpdatedAt);

        builder.HasOne(servico => servico.Estabelecimento)
            .WithMany(estabelecimento => estabelecimento.Servicos)
            .HasForeignKey(servico => servico.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(servico => servico.ProfissionalAutonomo)
            .WithMany(profissional => profissional.ServicosAutonomo)
            .HasForeignKey(servico => servico.ProfissionalAutonomoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(servico => servico.EstabelecimentoId);

        builder.HasIndex(servico => servico.ProfissionalAutonomoId);
    }
}
