using GLOWAPI.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260819123000_SepararCategoriasLojas")]
    public partial class SepararCategoriasLojas : Migration
    {
        private const string TipoEstabelecimento = "Estabelecimento";
        private const string NomeLojaLegado = "Barbearia ou salão de beleza";
        private const string NomeBarbearia = "Barbearia";
        private const string NomeSalao = "Salão de Beleza";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                UPDATE `CategoriasEstabelecimento`
                SET `Nome` = '{NomeBarbearia}',
                    `UpdatedAt` = UTC_TIMESTAMP(6)
                WHERE `Id` = 1
                  AND `TipoAssinatura` = '{TipoEstabelecimento}'
                  AND `Nome` = '{NomeLojaLegado}';
                """);

            migrationBuilder.Sql($"""
                INSERT INTO `CategoriasEstabelecimento` (`Nome`, `Ativo`, `TipoAssinatura`, `CreateAd`)
                SELECT '{NomeSalao}', 1, '{TipoEstabelecimento}', UTC_TIMESTAMP(6)
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM `CategoriasEstabelecimento` c
                    WHERE c.`Nome` = '{NomeSalao}'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                UPDATE `Estabelecimentos` e
                INNER JOIN `CategoriasEstabelecimento` c ON c.`Id` = e.`CategoriaEstabelecimentoId`
                SET e.`CategoriaEstabelecimentoId` = 1
                WHERE c.`TipoAssinatura` = '{TipoEstabelecimento}'
                  AND c.`Nome` = '{NomeSalao}'
                  AND c.`Id` <> 1;
                """);

            migrationBuilder.Sql($"""
                DELETE FROM `CategoriasEstabelecimento`
                WHERE `TipoAssinatura` = '{TipoEstabelecimento}'
                  AND `Nome` = '{NomeSalao}'
                  AND `Id` <> 1;
                """);

            migrationBuilder.Sql($"""
                UPDATE `CategoriasEstabelecimento`
                SET `Nome` = '{NomeLojaLegado}',
                    `UpdatedAt` = UTC_TIMESTAMP(6)
                WHERE `Id` = 1
                  AND `TipoAssinatura` = '{TipoEstabelecimento}'
                  AND `Nome` = '{NomeBarbearia}';
                """);
        }
    }
}
