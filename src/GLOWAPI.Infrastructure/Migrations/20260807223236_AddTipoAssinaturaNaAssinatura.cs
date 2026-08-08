using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTipoAssinaturaNaAssinatura : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TipoAssinatura",
                table: "Assinaturas",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Estabelecimento")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql("""
                UPDATE Assinaturas a
                INNER JOIN ProfissionaisEstabelecimentos pe
                    ON pe.EstabelecimentoId = a.EstabelecimentoId
                    AND pe.Ativo = 1
                INNER JOIN Profissionais p
                    ON p.Id = pe.ProfissionalId
                SET a.TipoAssinatura = 'ProfissionalAutonomo'
                WHERE p.TipoProfissional = 'Autonomo';
                """);

            migrationBuilder.Sql("""
                UPDATE Assinaturas
                SET TipoAssinatura = 'ProfissionalAutonomo'
                WHERE OnboardingPendenteJson IS NOT NULL
                  AND OnboardingPendenteJson LIKE '%ProfissionalAutonomo%';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TipoAssinatura",
                table: "Assinaturas");
        }
    }
}
