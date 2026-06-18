using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class AvaliacaoAtendimentoConfiguration : IEntityTypeConfiguration<AvaliacaoAtendimento>
{
    public void Configure(EntityTypeBuilder<AvaliacaoAtendimento> builder)
    {
        builder.ToTable("AvaliacoesAtendimento");

        builder.HasKey(avaliacao => avaliacao.Id);

        builder.HasIndex(avaliacao => avaliacao.AgendamentoId)
            .IsUnique();

        builder.HasIndex(avaliacao => new { avaliacao.EstabelecimentoId, avaliacao.AvaliadoEm });
        builder.HasIndex(avaliacao => new { avaliacao.ProfissionalId, avaliacao.AvaliadoEm });

        builder.Property(avaliacao => avaliacao.ComentarioEstabelecimento)
            .HasMaxLength(1000);

        builder.Property(avaliacao => avaliacao.ComentarioProfissional)
            .HasMaxLength(1000);

        builder.Property(avaliacao => avaliacao.NotaEstabelecimento)
            .IsRequired();

        builder.Property(avaliacao => avaliacao.NotaProfissional)
            .IsRequired();

        builder.HasOne(avaliacao => avaliacao.Agendamento)
            .WithMany()
            .HasForeignKey(avaliacao => avaliacao.AgendamentoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(avaliacao => avaliacao.UsuarioCliente)
            .WithMany()
            .HasForeignKey(avaliacao => avaliacao.UsuarioClienteId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(avaliacao => avaliacao.Estabelecimento)
            .WithMany()
            .HasForeignKey(avaliacao => avaliacao.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(avaliacao => avaliacao.Profissional)
            .WithMany()
            .HasForeignKey(avaliacao => avaliacao.ProfissionalId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
