using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedCampanhaPromocionalLancamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO `CampanhasPromocionais` (
                    `Codigo`,
                    `Limite`,
                    `Utilizados`,
                    `DiasTrial`,
                    `Ativa`,
                    `CreateAd`)
                VALUES (
                    'lancamento-100',
                    100,
                    0,
                    30,
                    TRUE,
                    UTC_TIMESTAMP())
                ON DUPLICATE KEY UPDATE
                    `Limite` = VALUES(`Limite`),
                    `DiasTrial` = VALUES(`DiasTrial`),
                    `Ativa` = TRUE,
                    `UpdatedAt` = UTC_TIMESTAMP();

                UPDATE `CampanhasPromocionais` AS cp
                SET cp.`Utilizados` = (
                    SELECT COUNT(*)
                    FROM `Assinaturas` AS a
                    WHERE a.`CampanhaPromocionalId` = cp.`Id`
                ),
                    cp.`UpdatedAt` = UTC_TIMESTAMP()
                WHERE cp.`Codigo` = 'lancamento-100';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE cp FROM `CampanhasPromocionais` AS cp
                WHERE cp.`Codigo` = 'lancamento-100'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM `Assinaturas` AS a
                      WHERE a.`CampanhaPromocionalId` = cp.`Id`);
                """);
        }
    }
}
