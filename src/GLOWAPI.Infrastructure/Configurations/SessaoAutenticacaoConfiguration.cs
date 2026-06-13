using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class SessaoAutenticacaoConfiguration : IEntityTypeConfiguration<SessaoAutenticacao>
{
    public void Configure(EntityTypeBuilder<SessaoAutenticacao> builder)
    {
        builder.ToTable("SessoesAutenticacao");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.AccessTokenHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasIndex(s => s.AccessTokenHash);

        builder.Property(s => s.RefreshTokenHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasIndex(s => s.RefreshTokenHash)
            .IsUnique();

        builder.Property(s => s.Ip)
            .HasMaxLength(45);

        builder.Property(s => s.UserAgent)
            .HasMaxLength(512);

        builder.Property(s => s.MetadataJson)
            .IsRequired()
            .HasColumnType("json");

        builder.Property(s => s.LoginEm).IsRequired();
        builder.Property(s => s.AccessTokenExpiraEm).IsRequired();
        builder.Property(s => s.ExpiraEm).IsRequired();
        builder.Property(s => s.CreatedAt).IsRequired();

        builder.HasOne(s => s.Usuario)
            .WithMany(u => u.Sessoes)
            .HasForeignKey(s => s.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.UsuarioId);
        builder.HasIndex(s => s.ExpiraEm);
    }
}
