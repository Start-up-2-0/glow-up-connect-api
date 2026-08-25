using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GLOWAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddComodidadesPerfil : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Comodidades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nome = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Slug = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Icone = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    Ativo = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreateAd = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comodidades", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EstabelecimentoComodidades",
                columns: table => new
                {
                    EstabelecimentoId = table.Column<int>(type: "int", nullable: false),
                    ComodidadeId = table.Column<int>(type: "int", nullable: false),
                    CreateAd = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstabelecimentoComodidades", x => new { x.EstabelecimentoId, x.ComodidadeId });
                    table.ForeignKey(
                        name: "FK_EstabelecimentoComodidades_Comodidades_ComodidadeId",
                        column: x => x.ComodidadeId,
                        principalTable: "Comodidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EstabelecimentoComodidades_Estabelecimentos_EstabelecimentoId",
                        column: x => x.EstabelecimentoId,
                        principalTable: "Estabelecimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Comodidades",
                columns: new[] { "Id", "Ativo", "CreateAd", "Icone", "Nome", "Ordem", "Slug", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, true, new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Utc), "wifi", "Wi-Fi", 1, "wi-fi", null },
                    { 2, true, new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Utc), "snowflake", "Ambiente climatizado", 2, "ambiente-climatizado", null },
                    { 3, true, new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Utc), "cup-soda", "Bebidas", 3, "bebidas", null },
                    { 4, true, new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Utc), "coffee", "Café", 4, "cafe", null },
                    { 5, true, new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Utc), "gamepad-2", "Videogame", 5, "videogame", null },
                    { 6, true, new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Utc), "tv", "TV", 6, "tv", null },
                    { 7, true, new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Utc), "car", "Estacionamento", 7, "estacionamento", null },
                    { 8, true, new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Utc), "accessibility", "Acessibilidade", 8, "acessibilidade", null },
                    { 9, true, new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Utc), "baby", "Atendimento infantil", 9, "atendimento-infantil", null },
                    { 10, true, new DateTime(2026, 8, 24, 0, 0, 0, 0, DateTimeKind.Utc), "paw-print", "Pet friendly", 10, "pet-friendly", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Comodidades_Slug",
                table: "Comodidades",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EstabelecimentoComodidades_ComodidadeId",
                table: "EstabelecimentoComodidades",
                column: "ComodidadeId");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EstabelecimentoComodidades");

            migrationBuilder.DropTable(
                name: "Comodidades");

        }
    }
}
