using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GLOWAPI.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateMetasProfissional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MetasProfissional",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProfissionalEstabelecimentoId = table.Column<int>(type: "integer", nullable: false),
                    TipoMeta = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    QuantidadeAtendimentos = table.Column<int>(type: "integer", nullable: true),
                    ValorFaturamento = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    InicioPeriodo = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FimPeriodo = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreateAd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetasProfissional", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MetasProfissional_ProfissionalEstabelecimentos_Profissional~",
                        column: x => x.ProfissionalEstabelecimentoId,
                        principalTable: "ProfissionalEstabelecimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MetasProfissional_ProfissionalEstabelecimentoId",
                table: "MetasProfissional",
                column: "ProfissionalEstabelecimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_MetasProfissional_Status",
                table: "MetasProfissional",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MetasProfissional");
        }
    }
}
