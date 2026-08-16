using GLOWAPI.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260816210000_ConviteLinkMultiUso")]
    public partial class ConviteLinkMultiUso : Migration
    {
        /// <summary>
        /// Idempotente: staging já pode ter LimiteUsuarios/QuantidadeUtilizacoes
        /// (schema parcial fora do histórico EF) sem esta migration aplicada.
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                SET @col_exists := (
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = 'ConvitesNegocio'
                      AND COLUMN_NAME = 'LimiteUsuarios'
                );
                SET @sql := IF(
                    @col_exists = 0,
                    'ALTER TABLE `ConvitesNegocio` ADD `LimiteUsuarios` int NOT NULL DEFAULT 1',
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
                      AND TABLE_NAME = 'ConvitesNegocio'
                      AND COLUMN_NAME = 'QuantidadeUtilizacoes'
                );
                SET @sql := IF(
                    @col_exists = 0,
                    'ALTER TABLE `ConvitesNegocio` ADD `QuantidadeUtilizacoes` int NOT NULL DEFAULT 0',
                    'SELECT 1'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);

            migrationBuilder.Sql("""
                UPDATE `ConvitesNegocio`
                SET `Status` = 'Cancelado',
                    `UpdatedAt` = UTC_TIMESTAMP()
                WHERE `Status` IN ('Pendente', 'Aceito', 'Rejeitado');
                """);

            migrationBuilder.Sql("""
                SET @idx_exists := (
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.STATISTICS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = 'ConvitesNegocio'
                      AND INDEX_NAME = 'IX_ConvitesNegocio_EstabelecimentoId_Email_TipoConvite_Status'
                );
                SET @sql := IF(
                    @idx_exists > 0,
                    'DROP INDEX `IX_ConvitesNegocio_EstabelecimentoId_Email_TipoConvite_Status` ON `ConvitesNegocio`',
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
                      AND TABLE_NAME = 'ConvitesNegocio'
                      AND INDEX_NAME = 'IX_ConvitesNegocio_EstabelecimentoId_Status'
                );
                SET @sql := IF(
                    @idx_exists = 0,
                    'CREATE INDEX `IX_ConvitesNegocio_EstabelecimentoId_Status` ON `ConvitesNegocio` (`EstabelecimentoId`, `Status`)',
                    'SELECT 1'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS `ConvitesNegocioUtilizacoes` (
                    `Id` int NOT NULL AUTO_INCREMENT,
                    `ConviteNegocioId` int NOT NULL,
                    `UsuarioId` int NOT NULL,
                    `UtilizadoEm` datetime(6) NOT NULL,
                    PRIMARY KEY (`Id`),
                    UNIQUE KEY `IX_ConvitesNegocioUtilizacoes_ConviteNegocioId_UsuarioId` (`ConviteNegocioId`, `UsuarioId`),
                    KEY `IX_ConvitesNegocioUtilizacoes_UsuarioId` (`UsuarioId`),
                    CONSTRAINT `FK_ConvitesNegocioUtilizacoes_ConvitesNegocio_ConviteNegocioId`
                        FOREIGN KEY (`ConviteNegocioId`) REFERENCES `ConvitesNegocio` (`Id`) ON DELETE CASCADE,
                    CONSTRAINT `FK_ConvitesNegocioUtilizacoes_Usuarios_UsuarioId`
                        FOREIGN KEY (`UsuarioId`) REFERENCES `Usuarios` (`Id`) ON DELETE RESTRICT
                ) CHARACTER SET=utf8mb4;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TABLE IF EXISTS `ConvitesNegocioUtilizacoes`;
                """);

            migrationBuilder.Sql("""
                SET @idx_exists := (
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.STATISTICS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = 'ConvitesNegocio'
                      AND INDEX_NAME = 'IX_ConvitesNegocio_EstabelecimentoId_Status'
                );
                SET @sql := IF(
                    @idx_exists > 0,
                    'DROP INDEX `IX_ConvitesNegocio_EstabelecimentoId_Status` ON `ConvitesNegocio`',
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
                      AND TABLE_NAME = 'ConvitesNegocio'
                      AND COLUMN_NAME = 'LimiteUsuarios'
                );
                SET @sql := IF(
                    @col_exists > 0,
                    'ALTER TABLE `ConvitesNegocio` DROP COLUMN `LimiteUsuarios`',
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
                      AND TABLE_NAME = 'ConvitesNegocio'
                      AND COLUMN_NAME = 'QuantidadeUtilizacoes'
                );
                SET @sql := IF(
                    @col_exists > 0,
                    'ALTER TABLE `ConvitesNegocio` DROP COLUMN `QuantidadeUtilizacoes`',
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
                      AND TABLE_NAME = 'ConvitesNegocio'
                      AND INDEX_NAME = 'IX_ConvitesNegocio_EstabelecimentoId_Email_TipoConvite_Status'
                );
                SET @sql := IF(
                    @idx_exists = 0,
                    'CREATE INDEX `IX_ConvitesNegocio_EstabelecimentoId_Email_TipoConvite_Status` ON `ConvitesNegocio` (`EstabelecimentoId`, `Email`, `TipoConvite`, `Status`)',
                    'SELECT 1'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);
        }
    }
}
