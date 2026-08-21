using GLOWAPI.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations;

/// <summary>
/// Reativa visibilidade pública de profissionais autônomos com assinatura ativa,
/// alinhando a vitrine a lojas (já republicadas em 20260819153000).
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260821153000_RepublicarAutonomosAssinaturaAtiva")]
public partial class RepublicarAutonomosAssinaturaAtiva : Migration
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
              AND a.Status IN ('Ativa', 'Trial')
              AND a.TipoAssinatura = 'ProfissionalAutonomo';

            UPDATE Estabelecimentos e
            INNER JOIN AssinaturaEstabelecimentos ae ON ae.EstabelecimentoId = e.Id
            INNER JOIN Assinaturas a ON a.Id = ae.AssinaturaId
            SET e.VisivelPublicamente = 1,
                e.UpdatedAt = UTC_TIMESTAMP()
            WHERE e.Ativo = 1
              AND e.VisivelPublicamente = 0
              AND a.Status IN ('Ativa', 'Trial')
              AND a.TipoAssinatura = 'ProfissionalAutonomo';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Reversão intencionalmente omitida.
    }
}
