using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DescontoPermanenteCampanhaLancamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PercentualDescontoMensalidade",
                table: "CampanhasPromocionais",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 50m);

            migrationBuilder.AddColumn<decimal>(
                name: "PercentualDescontoPermanente",
                table: "Assinaturas",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE `CampanhasPromocionais`
                SET `PercentualDescontoMensalidade` = 50,
                    `UpdatedAt` = UTC_TIMESTAMP()
                WHERE `Codigo` = 'lancamento-100';

                UPDATE `Assinaturas` AS a
                INNER JOIN `CampanhasPromocionais` AS c ON a.`CampanhaPromocionalId` = c.`Id`
                SET a.`PercentualDescontoPermanente` = c.`PercentualDescontoMensalidade`,
                    a.`UpdatedAt` = UTC_TIMESTAMP()
                WHERE a.`PercentualDescontoPermanente` IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PercentualDescontoMensalidade",
                table: "CampanhasPromocionais");

            migrationBuilder.DropColumn(
                name: "PercentualDescontoPermanente",
                table: "Assinaturas");
        }
    }
}
