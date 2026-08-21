using GLOWAPI.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations;

/// <summary>
/// Adiciona tipo (Individual/Combo) e imagem ilustrativa aos serviços.
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260819170000_AddTipoServicoEImagemServico")]
public partial class AddTipoServicoEImagemServico : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            SET @imagem_exists := (
                SELECT COUNT(*)
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = 'Servicos'
                  AND COLUMN_NAME = 'Imagem'
            );
            SET @sql := IF(
                @imagem_exists = 0,
                'ALTER TABLE `Servicos` ADD `Imagem` longtext CHARACTER SET utf8mb4 NULL',
                'SELECT 1'
            );
            PREPARE stmt FROM @sql;
            EXECUTE stmt;
            DEALLOCATE PREPARE stmt;

            SET @tipo_exists := (
                SELECT COUNT(*)
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = 'Servicos'
                  AND COLUMN_NAME = 'TipoServico'
            );
            SET @sql := IF(
                @tipo_exists = 0,
                'ALTER TABLE `Servicos` ADD `TipoServico` varchar(50) CHARACTER SET utf8mb4 NOT NULL DEFAULT ''Individual''',
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
            SET @tipo_exists := (
                SELECT COUNT(*)
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = 'Servicos'
                  AND COLUMN_NAME = 'TipoServico'
            );
            SET @sql := IF(
                @tipo_exists > 0,
                'ALTER TABLE `Servicos` DROP COLUMN `TipoServico`',
                'SELECT 1'
            );
            PREPARE stmt FROM @sql;
            EXECUTE stmt;
            DEALLOCATE PREPARE stmt;

            SET @imagem_exists := (
                SELECT COUNT(*)
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = 'Servicos'
                  AND COLUMN_NAME = 'Imagem'
            );
            SET @sql := IF(
                @imagem_exists > 0,
                'ALTER TABLE `Servicos` DROP COLUMN `Imagem`',
                'SELECT 1'
            );
            PREPARE stmt FROM @sql;
            EXECUTE stmt;
            DEALLOCATE PREPARE stmt;
            """);
    }
}
