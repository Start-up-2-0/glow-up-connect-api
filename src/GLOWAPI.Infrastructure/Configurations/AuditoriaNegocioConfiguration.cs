using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class AuditoriaNegocioConfiguration : IEntityTypeConfiguration<AuditoriaNegocio>
{
    public void Configure(EntityTypeBuilder<AuditoriaNegocio> builder)
    {
        builder.ToTable("AuditoriasNegocio");

        builder.HasKey(auditoria => auditoria.Id);

        builder.Property(auditoria => auditoria.TipoAcao)
            .HasConversion<string>()
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(auditoria => auditoria.Entidade)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(auditoria => auditoria.PayloadJson)
            .HasColumnType("json")
            .IsRequired();

        builder.Property(auditoria => auditoria.CriadoEm)
            .IsRequired();

        builder.HasOne(auditoria => auditoria.Estabelecimento)
            .WithMany()
            .HasForeignKey(auditoria => auditoria.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(auditoria => auditoria.Usuario)
            .WithMany()
            .HasForeignKey(auditoria => auditoria.UsuarioId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(auditoria => auditoria.EstabelecimentoId);
        builder.HasIndex(auditoria => auditoria.UsuarioId);
        builder.HasIndex(auditoria => auditoria.TipoAcao);
        builder.HasIndex(auditoria => auditoria.CriadoEm);
    }
}
