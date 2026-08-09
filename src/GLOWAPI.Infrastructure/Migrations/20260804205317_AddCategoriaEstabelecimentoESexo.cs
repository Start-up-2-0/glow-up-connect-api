using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoriaEstabelecimentoESexo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Sexo",
                table: "Usuarios",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "CategoriaEstabelecimentoId",
                table: "Estabelecimentos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CategoriasEstabelecimento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nome = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Ativo = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreateAd = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriasEstabelecimento", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Estabelecimentos_CategoriaEstabelecimentoId",
                table: "Estabelecimentos",
                column: "CategoriaEstabelecimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoriasEstabelecimento_Nome",
                table: "CategoriasEstabelecimento",
                column: "Nome",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Estabelecimentos_CategoriasEstabelecimento_CategoriaEstabele~",
                table: "Estabelecimentos",
                column: "CategoriaEstabelecimentoId",
                principalTable: "CategoriasEstabelecimento",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Seed das categorias iniciais (extensível — novas categorias são apenas INSERTs).
            migrationBuilder.Sql("""
                INSERT INTO `CategoriasEstabelecimento` (`Id`, `Nome`, `Ativo`, `CreateAd`)
                VALUES (1, 'Cabelo e barba', 1, NOW(6)),
                       (2, 'Beleza e estética', 1, NOW(6))
                ON DUPLICATE KEY UPDATE `Nome` = VALUES(`Nome`);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Estabelecimentos_CategoriasEstabelecimento_CategoriaEstabele~",
                table: "Estabelecimentos");

            migrationBuilder.DropTable(
                name: "CategoriasEstabelecimento");

            migrationBuilder.DropIndex(
                name: "IX_Estabelecimentos_CategoriaEstabelecimentoId",
                table: "Estabelecimentos");

            migrationBuilder.DropColumn(
                name: "Sexo",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "CategoriaEstabelecimentoId",
                table: "Estabelecimentos");
        }
    }
}
