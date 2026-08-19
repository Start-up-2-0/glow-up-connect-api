using GLOWAPI.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations;

/// <summary>
/// Revalidação retroativa: assinaturas ativas com onboarding incompleto deixam de ficar públicas.
/// A lógica completa é reaplicada no login via UsuarioNegocioContextoService.
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260819140000_RevalidarPublicacaoOnboardingObrigatorio")]
public partial class RevalidarPublicacaoOnboardingObrigatorio : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE Estabelecimentos e
            INNER JOIN Assinaturas a ON a.EstabelecimentoId = e.Id
            SET e.VisivelPublicamente = 0,
                e.UpdatedAt = UTC_TIMESTAMP()
            WHERE e.Ativo = 1
              AND e.VisivelPublicamente = 1
              AND a.Status IN ('Ativa', 'Trial')
              AND (
                    NOT EXISTS (
                        SELECT 1 FROM Servicos s
                        WHERE s.EstabelecimentoId = e.Id AND s.Ativo = 1
                    )
                 OR NOT EXISTS (
                        SELECT 1 FROM HorarioAtendimentoProfissionais h
                        WHERE h.EstabelecimentoId = e.Id AND h.Ativo = 1
                    )
                 OR (
                        a.TipoAssinatura = 'Estabelecimento'
                        AND (
                            NOT EXISTS (
                                SELECT 1 FROM ProfissionalEstabelecimentos pe
                                INNER JOIN Profissionais p ON p.Id = pe.ProfissionalId
                                WHERE pe.EstabelecimentoId = e.Id
                                  AND pe.Ativo = 1
                                  AND pe.PodeReceberAgendamento = 1
                                  AND p.Ativo = 1
                            )
                         OR NOT EXISTS (
                                SELECT 1 FROM HorarioFuncionamentoEstabelecimentos hf
                                WHERE hf.EstabelecimentoId = e.Id AND hf.Ativo = 1
                            )
                         OR NOT EXISTS (
                                SELECT 1
                                FROM ProfissionalServicos ps
                                INNER JOIN Servicos s ON s.Id = ps.ServicoId
                                INNER JOIN Profissionais p ON p.Id = ps.ProfissionalId
                                INNER JOIN ProfissionalEstabelecimentos pe
                                    ON pe.ProfissionalId = p.Id AND pe.EstabelecimentoId = e.Id
                                WHERE ps.Ativo = 1
                                  AND s.Ativo = 1
                                  AND s.EstabelecimentoId = e.Id
                                  AND p.Ativo = 1
                                  AND pe.Ativo = 1
                                  AND pe.PodeReceberAgendamento = 1
                            )
                        )
                    )
              );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Reversão intencionalmente omitida: não republicar perfis incompletos automaticamente.
    }
}
