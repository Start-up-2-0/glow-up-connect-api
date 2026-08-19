using GLOWAPI.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations;

/// <summary>
/// Reativa visibilidade pública de lojas com assinatura ativa (onboarding obrigatório permanece só para autônomos).
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260819153000_RepublicarLojasAssinaturaAtiva")]
public partial class RepublicarLojasAssinaturaAtiva : Migration
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
              AND a.TipoAssinatura = 'Estabelecimento';

            UPDATE Estabelecimentos e
            INNER JOIN AssinaturaEstabelecimentos ae ON ae.EstabelecimentoId = e.Id
            INNER JOIN Assinaturas a ON a.Id = ae.AssinaturaId
            SET e.VisivelPublicamente = 1,
                e.UpdatedAt = UTC_TIMESTAMP()
            WHERE e.Ativo = 1
              AND e.VisivelPublicamente = 0
              AND a.Status IN ('Ativa', 'Trial')
              AND a.TipoAssinatura = 'Estabelecimento';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Reversão intencionalmente omitida.
    }
}
