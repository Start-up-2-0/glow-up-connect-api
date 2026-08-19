using GLOWAPI.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260819120000_ExpandirCategoriasProfissionalAutonomo")]
    public partial class ExpandirCategoriasProfissionalAutonomo : Migration
    {
        private const string TipoProfissionalAutonomo = "ProfissionalAutonomo";
        private const string NomeAntigoAutonomo = "Barbeiro ou cabeleireiro(a)";
        private const string NomeBarbeiro = "Barbeiro";

        private static readonly string[] NovasCategoriasAutonomo =
        [
            "Cabeleireiro(a)",
            "Barbeiro(a)",
            "Manicure / Pedicure",
            "Designer de Sobrancelhas",
            "Lash Designer",
            "Maquiador(a)",
            "Esteticista",
            "Massoterapeuta",
            "Trancista",
            "Nail Designer",
            "Depilador(a)",
            "Especialista em Limpeza de Pele",
            "Micropigmentador(a)",
            "Piercer",
            "Podólogo(a)",
        ];

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                UPDATE `CategoriasEstabelecimento`
                SET `Nome` = '{NomeBarbeiro}',
                    `UpdatedAt` = UTC_TIMESTAMP(6)
                WHERE `Id` = 2
                  AND `TipoAssinatura` = '{TipoProfissionalAutonomo}'
                  AND `Nome` = '{NomeAntigoAutonomo}';
                """);

            migrationBuilder.Sql($"""
                INSERT INTO `CategoriasEstabelecimento` (`Nome`, `Ativo`, `TipoAssinatura`, `CreateAd`)
                SELECT v.`Nome`, 1, '{TipoProfissionalAutonomo}', UTC_TIMESTAMP(6)
                FROM (
                    SELECT 'Cabeleireiro(a)' AS `Nome` UNION ALL
                    SELECT 'Barbeiro(a)' UNION ALL
                    SELECT 'Manicure / Pedicure' UNION ALL
                    SELECT 'Designer de Sobrancelhas' UNION ALL
                    SELECT 'Lash Designer' UNION ALL
                    SELECT 'Maquiador(a)' UNION ALL
                    SELECT 'Esteticista' UNION ALL
                    SELECT 'Massoterapeuta' UNION ALL
                    SELECT 'Trancista' UNION ALL
                    SELECT 'Nail Designer' UNION ALL
                    SELECT 'Depilador(a)' UNION ALL
                    SELECT 'Especialista em Limpeza de Pele' UNION ALL
                    SELECT 'Micropigmentador(a)' UNION ALL
                    SELECT 'Piercer' UNION ALL
                    SELECT 'Podólogo(a)'
                ) v
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM `CategoriasEstabelecimento` c
                    WHERE c.`Nome` = v.`Nome`
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var nomesNovosSql = string.Join(", ", NovasCategoriasAutonomo.Select(n => $"'{n}'"));

            migrationBuilder.Sql($"""
                UPDATE `Estabelecimentos` e
                INNER JOIN `CategoriasEstabelecimento` c ON c.`Id` = e.`CategoriaEstabelecimentoId`
                SET e.`CategoriaEstabelecimentoId` = 2
                WHERE c.`TipoAssinatura` = '{TipoProfissionalAutonomo}'
                  AND c.`Id` <> 2
                  AND c.`Nome` IN ({nomesNovosSql});
                """);

            migrationBuilder.Sql($"""
                DELETE FROM `CategoriasEstabelecimento`
                WHERE `TipoAssinatura` = '{TipoProfissionalAutonomo}'
                  AND `Id` <> 2
                  AND `Nome` IN ({nomesNovosSql});
                """);

            migrationBuilder.Sql($"""
                UPDATE `CategoriasEstabelecimento`
                SET `Nome` = '{NomeAntigoAutonomo}',
                    `UpdatedAt` = UTC_TIMESTAMP(6)
                WHERE `Id` = 2
                  AND `TipoAssinatura` = '{TipoProfissionalAutonomo}'
                  AND `Nome` = '{NomeBarbeiro}';
                """);
        }
    }
}
