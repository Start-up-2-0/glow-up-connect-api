using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class RecuperacaoSenhaConfiguration : IEntityTypeConfiguration<RecuperacaoSenha>
{
    public void Configure(EntityTypeBuilder<RecuperacaoSenha> builder)
    {
        builder.ToTable("RecuperacoesSenha");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.CodigoHash)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(r => r.CodigoExpiraEm)
            .IsRequired();

        builder.Property(r => r.CodigoVerificado)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(r => r.CodigoTentativas)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(r => r.ResetTokenHash)
            .HasMaxLength(256);

        builder.Property(r => r.IpSolicitacao)
            .HasMaxLength(45);

        builder.Property(r => r.CriadoEm)
            .IsRequired();

        // Índices
        builder.HasIndex(r => r.UsuarioId)
            .HasDatabaseName("IX_RecuperacoesSenha_UsuarioId");

        builder.HasIndex(r => r.ResetTokenHash)
            .HasDatabaseName("IX_RecuperacoesSenha_ResetTokenHash")
            .IsUnique()
            .HasFilter("\"ResetTokenHash\" IS NOT NULL");

        // Relacionamento: RecuperacaoSenha N:1 Usuario
        builder.HasOne(r => r.Usuario)
            .WithMany()
            .HasForeignKey(r => r.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}