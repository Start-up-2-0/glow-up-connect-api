using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class EstabelecimentoConfiguration : IEntityTypeConfiguration<Estabelecimento>
{
    public void Configure(EntityTypeBuilder<Estabelecimento> builder)
    {
        builder.ToTable("Estabelecimentos");

        builder.HasKey(estabelecimento => estabelecimento.Id);

        builder.Property(estabelecimento => estabelecimento.PublicGuid)
            .IsRequired();

        builder.HasIndex(estabelecimento => estabelecimento.PublicGuid)
            .IsUnique();

        builder.Property(estabelecimento => estabelecimento.Nome)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(estabelecimento => estabelecimento.Descricao)
            .HasMaxLength(500);

        builder.Property(estabelecimento => estabelecimento.Logo)
            .HasColumnType("longtext");

        builder.Property(estabelecimento => estabelecimento.Telefone)
            .HasMaxLength(20);

        builder.Property(estabelecimento => estabelecimento.Email)
            .HasMaxLength(255);

        builder.Property(estabelecimento => estabelecimento.Ativo)
            .HasDefaultValue(true);

        builder.Property(estabelecimento => estabelecimento.VisivelPublicamente)
            .HasDefaultValue(true);

        builder.Property(estabelecimento => estabelecimento.WhatsAppConfirmadoEm);

        builder.Property(estabelecimento => estabelecimento.WhatsAppConfirmacaoTokenHash)
            .HasMaxLength(128);

        builder.HasIndex(estabelecimento => estabelecimento.WhatsAppConfirmacaoTokenHash);

        builder.Property(estabelecimento => estabelecimento.WhatsAppConfirmacaoCodigoHash)
            .HasMaxLength(128);

        builder.HasIndex(estabelecimento => estabelecimento.WhatsAppConfirmacaoCodigoHash);

        builder.Property(estabelecimento => estabelecimento.WhatsAppConfirmacaoExpiraEm);

        builder.Property(estabelecimento => estabelecimento.WhatsAppOptIn)
            .HasDefaultValue(false);

        builder.Property(estabelecimento => estabelecimento.CreateAd)
            .IsRequired();

        builder.Property(estabelecimento => estabelecimento.UpdatedAt);

        builder.Property(estabelecimento => estabelecimento.CategoriaEstabelecimentoId)
            .IsRequired(false);

        builder.HasOne(estabelecimento => estabelecimento.CategoriaEstabelecimento)
            .WithMany()
            .HasForeignKey(estabelecimento => estabelecimento.CategoriaEstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(estabelecimento => estabelecimento.CategoriaEstabelecimentoId);
    }
}
