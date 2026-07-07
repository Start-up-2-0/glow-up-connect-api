using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class SessaoCaixaConfiguration : IEntityTypeConfiguration<SessaoCaixa>
{
    public void Configure(EntityTypeBuilder<SessaoCaixa> builder)
    {
        builder.ToTable("SessoesCaixa");

        builder.HasKey(sessao => sessao.Id);

        builder.Property(sessao => sessao.SaldoInicial).HasPrecision(12, 2);
        builder.Property(sessao => sessao.SaldoInformadoFechamento).HasPrecision(12, 2);
        builder.Property(sessao => sessao.Diferenca).HasPrecision(12, 2);

        builder.Property(sessao => sessao.Status)
            .HasConversion(status => status.ToString(), status => Enum.Parse<SessaoCaixaStatus>(status))
            .HasMaxLength(20);

        builder.HasOne(sessao => sessao.Caixa)
            .WithMany(caixa => caixa.Sessoes)
            .HasForeignKey(sessao => sessao.CaixaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sessao => sessao.Usuario)
            .WithMany()
            .HasForeignKey(sessao => sessao.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(sessao => new { sessao.CaixaId, sessao.Status });
    }
}
