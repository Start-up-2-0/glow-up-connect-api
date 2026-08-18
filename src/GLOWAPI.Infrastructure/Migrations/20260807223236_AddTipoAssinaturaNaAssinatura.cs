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
            // Idempotente: o deploy anterior pode ter adicionado a coluna e falhado no backfill.
            migrationBuilder.Sql("""
                SET @col_exists := (
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = 'Assinaturas'
                      AND COLUMN_NAME = 'TipoAssinatura'
                );
                SET @sql := IF(
                    @col_exists = 0,
                    'ALTER TABLE `Assinaturas` ADD `TipoAssinatura` varchar(50) CHARACTER SET utf8mb4 NOT NULL DEFAULT ''Estabelecimento''',
                    'SELECT 1'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);

            migrationBuilder.Sql("""
                UPDATE Assinaturas a
                INNER JOIN ProfissionalEstabelecimentos pe
                    ON pe.EstabelecimentoId = a.EstabelecimentoId
                    AND pe.Ativo = 1
                INNER JOIN Profissionais p
                    ON p.Id = pe.ProfissionalId
                SET a.TipoAssinatura = 'ProfissionalAutonomo'
                WHERE p.TipoProfissional = 'Autonomo';
                """);

            migrationBuilder.Sql("""
                UPDATE Assinaturas a
                INNER JOIN EstabelecimentoUsuarios eu
                    ON eu.EstabelecimentoId = a.EstabelecimentoId
                    AND eu.Ativo = 1
                    AND eu.RoleNoEstabelecimento = 'Owner'
                INNER JOIN Usuarios u
                    ON u.Id = eu.UsuarioId
                SET a.TipoAssinatura = 'ProfissionalAutonomo'
                WHERE u.Role = 'ProfissionalAutonomo';
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
            migrationBuilder.Sql("""
                SET @col_exists := (
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = 'Assinaturas'
                      AND COLUMN_NAME = 'TipoAssinatura'
                );
                SET @sql := IF(
                    @col_exists > 0,
                    'ALTER TABLE `Assinaturas` DROP COLUMN `TipoAssinatura`',
                    'SELECT 1'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);
        }
    }
}
