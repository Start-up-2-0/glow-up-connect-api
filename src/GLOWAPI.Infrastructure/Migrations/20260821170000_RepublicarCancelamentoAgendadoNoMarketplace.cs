using GLOWAPI.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations;

/// <summary>
/// Corrige vitrine: CancelamentoAgendado mantém acesso até o fim do período —
/// quem foi ocultado cedo demais volta a aparecer no explorar.
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260821170000_RepublicarCancelamentoAgendadoNoMarketplace")]
public partial class RepublicarCancelamentoAgendadoNoMarketplace : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE Estabelecimentos e
            INNER JOIN Assinaturas a ON a.EstabelecimentoId = e.Id
            SET e.VisivelPublicamente = 1,
                e.UpdatedAt = UTC_TIMESTAMP()
            WHERE e.Ativo = 1
              AND e.VisivelPublicamente = 0
              AND a.Status = 'CancelamentoAgendado'
              AND (a.Fim IS NULL OR a.Fim >= UTC_TIMESTAMP());

            UPDATE Estabelecimentos e
            INNER JOIN AssinaturaEstabelecimentos ae ON ae.EstabelecimentoId = e.Id
            INNER JOIN Assinaturas a ON a.Id = ae.AssinaturaId
            SET e.VisivelPublicamente = 1,
                e.UpdatedAt = UTC_TIMESTAMP()
            WHERE e.Ativo = 1
              AND e.VisivelPublicamente = 0
              AND a.Status = 'CancelamentoAgendado'
              AND (a.Fim IS NULL OR a.Fim >= UTC_TIMESTAMP());
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Reversão intencionalmente omitida.
    }
}
