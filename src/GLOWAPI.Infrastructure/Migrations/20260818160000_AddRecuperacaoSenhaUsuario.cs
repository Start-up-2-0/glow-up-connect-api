using GLOWAPI.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260818160000_AddRecuperacaoSenhaUsuario")]
    public partial class AddRecuperacaoSenhaUsuario : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                SET @col_exists := (
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = 'Usuarios'
                      AND COLUMN_NAME = 'RecuperacaoTokenHash'
                );
                SET @sql := IF(
                    @col_exists = 0,
                    'ALTER TABLE `Usuarios` ADD `RecuperacaoTokenHash` varchar(128) NULL, ADD `RecuperacaoCodigoHash` varchar(128) NULL, ADD `RecuperacaoExpiraEm` datetime(6) NULL',
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
                      AND INDEX_NAME = 'IX_Usuarios_RecuperacaoTokenHash'
                );
                SET @sql := IF(
                    @idx_exists = 0,
                    'CREATE INDEX `IX_Usuarios_RecuperacaoTokenHash` ON `Usuarios` (`RecuperacaoTokenHash`)',
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
                      AND INDEX_NAME = 'IX_Usuarios_RecuperacaoCodigoHash'
                );
                SET @sql := IF(
                    @idx_exists = 0,
                    'CREATE INDEX `IX_Usuarios_RecuperacaoCodigoHash` ON `Usuarios` (`RecuperacaoCodigoHash`)',
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
                      AND INDEX_NAME = 'IX_Usuarios_RecuperacaoTokenHash'
                );
                SET @sql := IF(
                    @idx_exists > 0,
                    'DROP INDEX `IX_Usuarios_RecuperacaoTokenHash` ON `Usuarios`',
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
                      AND INDEX_NAME = 'IX_Usuarios_RecuperacaoCodigoHash'
                );
                SET @sql := IF(
                    @idx_exists > 0,
                    'DROP INDEX `IX_Usuarios_RecuperacaoCodigoHash` ON `Usuarios`',
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
                      AND COLUMN_NAME = 'RecuperacaoTokenHash'
                );
                SET @sql := IF(
                    @col_exists > 0,
                    'ALTER TABLE `Usuarios` DROP COLUMN `RecuperacaoExpiraEm`, DROP COLUMN `RecuperacaoCodigoHash`, DROP COLUMN `RecuperacaoTokenHash`',
                    'SELECT 1'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);
        }
    }
}
