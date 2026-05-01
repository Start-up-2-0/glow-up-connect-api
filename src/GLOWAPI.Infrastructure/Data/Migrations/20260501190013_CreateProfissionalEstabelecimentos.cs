using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GLOWAPI.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateProfissionalEstabelecimentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProfissionalEstabelecimentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProfissionalId = table.Column<int>(type: "integer", nullable: false),
                    EstabelecimentoId = table.Column<int>(type: "integer", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    DataEntrada = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataSaida = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PodeReceberAgendamento = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreateAd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfissionalEstabelecimentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfissionalEstabelecimentos_Estabelecimentos_Estabelecimen~",
                        column: x => x.EstabelecimentoId,
                        principalTable: "Estabelecimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProfissionalEstabelecimentos_Profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProfissionalEstabelecimentos_EstabelecimentoId",
                table: "ProfissionalEstabelecimentos",
                column: "EstabelecimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfissionalEstabelecimentos_ProfissionalId",
                table: "ProfissionalEstabelecimentos",
                column: "ProfissionalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProfissionalEstabelecimentos");
        }
    }
}
