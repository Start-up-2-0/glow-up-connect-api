using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GLOWAPI.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateAssinaturas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Assinaturas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlanoId = table.Column<int>(type: "integer", nullable: false),
                    EstabelecimentoId = table.Column<int>(type: "integer", nullable: true),
                    ProfissionalAutonomoId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Inicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Fim = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RenovacaoAutomatica = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Gateway = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GatewaySubscriptionId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    GatewayCustomerId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    UltimoPagamentoId = table.Column<int>(type: "integer", nullable: true),
                    CreateAd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CanceladoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assinaturas", x => x.Id);
                    table.CheckConstraint("CK_Assinaturas_Titular", "((\"EstabelecimentoId\" IS NOT NULL AND \"ProfissionalAutonomoId\" IS NULL) OR (\"EstabelecimentoId\" IS NULL AND \"ProfissionalAutonomoId\" IS NOT NULL))");
                    table.ForeignKey(
                        name: "FK_Assinaturas_Estabelecimentos_EstabelecimentoId",
                        column: x => x.EstabelecimentoId,
                        principalTable: "Estabelecimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assinaturas_Planos_PlanoId",
                        column: x => x.PlanoId,
                        principalTable: "Planos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assinaturas_Profissionais_ProfissionalAutonomoId",
                        column: x => x.ProfissionalAutonomoId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assinaturas_EstabelecimentoId",
                table: "Assinaturas",
                column: "EstabelecimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Assinaturas_PlanoId",
                table: "Assinaturas",
                column: "PlanoId");

            migrationBuilder.CreateIndex(
                name: "IX_Assinaturas_ProfissionalAutonomoId",
                table: "Assinaturas",
                column: "ProfissionalAutonomoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Assinaturas");
        }
    }
}
