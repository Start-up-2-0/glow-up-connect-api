using GLOWAPI.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260816120000_SepararCategoriasPorTipoAssinatura")]
    public partial class SepararCategoriasPorTipoAssinatura : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TipoAssinatura",
                table: "CategoriasEstabelecimento",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Estabelecimento");

            // Categorias extras (se existirem) saem das opções sem colidir no índice único de Nome.
            migrationBuilder.Sql("""
                UPDATE `CategoriasEstabelecimento`
                SET `Nome` = CONCAT('old-', `Id`),
                    `Ativo` = 0,
                    `UpdatedAt` = UTC_TIMESTAMP()
                WHERE `Id` NOT IN (1, 2);
                """);

            migrationBuilder.Sql("""
                UPDATE `CategoriasEstabelecimento`
                SET `Nome` = 'Barbearia ou salão de beleza',
                    `TipoAssinatura` = 'Estabelecimento',
                    `Ativo` = 1,
                    `UpdatedAt` = UTC_TIMESTAMP()
                WHERE `Id` = 1;

                UPDATE `CategoriasEstabelecimento`
                SET `Nome` = 'Barbeiro ou cabeleireiro(a)',
                    `TipoAssinatura` = 'ProfissionalAutonomo',
                    `Ativo` = 1,
                    `UpdatedAt` = UTC_TIMESTAMP()
                WHERE `Id` = 2;
                """);

            migrationBuilder.Sql("""
                UPDATE `Estabelecimentos` e
                INNER JOIN `Assinaturas` a ON a.`EstabelecimentoId` = e.`Id`
                SET e.`CategoriaEstabelecimentoId` = CASE
                    WHEN a.`TipoAssinatura` = 'ProfissionalAutonomo' THEN 2
                    ELSE 1
                END;

                UPDATE `Estabelecimentos` e
                INNER JOIN `AssinaturaEstabelecimentos` ae ON ae.`EstabelecimentoId` = e.`Id`
                INNER JOIN `Assinaturas` a ON a.`Id` = ae.`AssinaturaId`
                SET e.`CategoriaEstabelecimentoId` = CASE
                    WHEN a.`TipoAssinatura` = 'ProfissionalAutonomo' THEN 2
                    ELSE 1
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE `CategoriasEstabelecimento`
                SET `Nome` = 'Cabelo e barba',
                    `Ativo` = 1,
                    `UpdatedAt` = UTC_TIMESTAMP()
                WHERE `Id` = 1;

                UPDATE `CategoriasEstabelecimento`
                SET `Nome` = 'Beleza e estética',
                    `Ativo` = 1,
                    `UpdatedAt` = UTC_TIMESTAMP()
                WHERE `Id` = 2;
                """);

            migrationBuilder.DropColumn(
                name: "TipoAssinatura",
                table: "CategoriasEstabelecimento");
        }
    }
}
