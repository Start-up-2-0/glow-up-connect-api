using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class LogAutenticacaoConfiguration : IEntityTypeConfiguration<LogAutenticacao>
{
    public void Configure(EntityTypeBuilder<LogAutenticacao> builder)
    {
        builder.ToTable("LogsAutenticacao");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(l => l.Evento)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(l => l.Ip)
            .HasMaxLength(45);

        builder.Property(l => l.UserAgent)
            .HasMaxLength(512);

        builder.Property(l => l.Detalhes)
            .HasMaxLength(1000);

        builder.Property(l => l.CreatedAt).IsRequired();

        builder.HasIndex(l => l.UsuarioId);
        builder.HasIndex(l => l.Evento);
        builder.HasIndex(l => l.CreatedAt);
    }
}
