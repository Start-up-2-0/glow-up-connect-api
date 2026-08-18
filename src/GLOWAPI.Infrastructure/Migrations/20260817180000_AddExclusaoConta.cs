using GLOWAPI.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260817180000_AddExclusaoConta")]
    public partial class AddExclusaoConta : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                SET @col_exists := (
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = 'Usuarios'
                      AND COLUMN_NAME = 'ExclusaoStatus'
                );
                SET @sql := IF(
                    @col_exists = 0,
                    'ALTER TABLE `Usuarios` ADD `ExclusaoStatus` varchar(20) NOT NULL DEFAULT ''Nenhuma'', ADD `ExclusaoSolicitadaEm` datetime(6) NULL, ADD `ExclusaoEfetivarEm` datetime(6) NULL',
                    'SELECT 1'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);

            migrationBuilder.Sql("""
                SET @idx_exists := (
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.STATISTICS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = 'Usuarios'
                      AND INDEX_NAME = 'IX_Usuarios_ExclusaoStatus_ExclusaoEfetivarEm'
                );
                SET @sql := IF(
                    @idx_exists = 0,
                    'CREATE INDEX `IX_Usuarios_ExclusaoStatus_ExclusaoEfetivarEm` ON `Usuarios` (`ExclusaoStatus`, `ExclusaoEfetivarEm`)',
                    'SELECT 1'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);

            migrationBuilder.Sql("""
                SET @col_exists := (
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = 'Assinaturas'
                      AND COLUMN_NAME = 'StatusAntesExclusao'
                );
                SET @sql := IF(
                    @col_exists = 0,
                    'ALTER TABLE `Assinaturas` ADD `StatusAntesExclusao` varchar(50) NULL',
                    'SELECT 1'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                SET @idx_exists := (
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.STATISTICS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = 'Usuarios'
                      AND INDEX_NAME = 'IX_Usuarios_ExclusaoStatus_ExclusaoEfetivarEm'
                );
                SET @sql := IF(
                    @idx_exists > 0,
                    'DROP INDEX `IX_Usuarios_ExclusaoStatus_ExclusaoEfetivarEm` ON `Usuarios`',
                    'SELECT 1'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);

            migrationBuilder.Sql("""
                SET @col_exists := (
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = 'Usuarios'
                      AND COLUMN_NAME = 'ExclusaoStatus'
                );
                SET @sql := IF(
                    @col_exists > 0,
                    'ALTER TABLE `Usuarios` DROP COLUMN `ExclusaoEfetivarEm`, DROP COLUMN `ExclusaoSolicitadaEm`, DROP COLUMN `ExclusaoStatus`',
                    'SELECT 1'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);

            migrationBuilder.Sql("""
                SET @col_exists := (
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = 'Assinaturas'
                      AND COLUMN_NAME = 'StatusAntesExclusao'
                );
                SET @sql := IF(
                    @col_exists > 0,
                    'ALTER TABLE `Assinaturas` DROP COLUMN `StatusAntesExclusao`',
                    'SELECT 1'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);
        }
    }
}
