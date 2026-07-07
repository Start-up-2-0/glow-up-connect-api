using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class LancamentoCaixaConfiguration : IEntityTypeConfiguration<LancamentoCaixa>
{
    public void Configure(EntityTypeBuilder<LancamentoCaixa> builder)
    {
        builder.ToTable("LancamentosCaixa");

        builder.HasKey(lancamento => lancamento.Id);

        builder.Property(lancamento => lancamento.Tipo)
            .HasConversion(tipo => tipo.ToString(), tipo => Enum.Parse<LancamentoCaixaTipo>(tipo))
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(lancamento => lancamento.Valor)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(lancamento => lancamento.Descricao)
            .HasMaxLength(500);

        builder.Property(lancamento => lancamento.ConciliacaoStatus)
            .HasConversion(
                status => status.ToString(),
                status => Enum.Parse<ConciliacaoStatus>(status))
            .HasMaxLength(20)
            .HasDefaultValue(ConciliacaoStatus.Pendente);

        builder.Property(lancamento => lancamento.CreateAd)
            .IsRequired();

        builder.HasOne(lancamento => lancamento.Caixa)
            .WithMany(caixa => caixa.Lancamentos)
            .HasForeignKey(lancamento => lancamento.CaixaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(lancamento => lancamento.Agendamento)
            .WithMany(agendamento => agendamento.LancamentosCaixa)
            .HasForeignKey(lancamento => lancamento.AgendamentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(lancamento => lancamento.Pagamento)
            .WithMany(pagamento => pagamento.LancamentosCaixa)
            .HasForeignKey(lancamento => lancamento.PagamentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(lancamento => lancamento.Profissional)
            .WithMany()
            .HasForeignKey(lancamento => lancamento.ProfissionalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(lancamento => lancamento.LancamentoOriginal)
            .WithMany()
            .HasForeignKey(lancamento => lancamento.LancamentoOriginalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(lancamento => lancamento.SessaoCaixa)
            .WithMany(sessao => sessao.Lancamentos)
            .HasForeignKey(lancamento => lancamento.SessaoCaixaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(lancamento => lancamento.CaixaId);
        builder.HasIndex(lancamento => lancamento.AgendamentoId);
        builder.HasIndex(lancamento => lancamento.PagamentoId);
        builder.HasIndex(lancamento => lancamento.ProfissionalId);
    }
}
