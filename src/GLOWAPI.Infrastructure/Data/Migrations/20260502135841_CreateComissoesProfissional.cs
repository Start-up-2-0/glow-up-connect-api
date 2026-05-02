using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GLOWAPI.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateComissoesProfissional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ComissoesProfissional",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProfissionalEstabelecimentoId = table.Column<int>(type: "integer", nullable: false),
                    TipoComissao = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Percentual = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    ValorFixo = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    InicioVigencia = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FimVigencia = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreateAd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComissoesProfissional", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComissoesProfissional_ProfissionalEstabelecimentos_Profissi~",
                        column: x => x.ProfissionalEstabelecimentoId,
                        principalTable: "ProfissionalEstabelecimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComissoesProfissional_ProfissionalEstabelecimentoId",
                table: "ComissoesProfissional",
                column: "ProfissionalEstabelecimentoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComissoesProfissional");
        }
    }
}
