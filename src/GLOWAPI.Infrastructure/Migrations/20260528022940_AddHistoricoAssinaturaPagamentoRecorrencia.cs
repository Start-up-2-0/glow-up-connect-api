using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHistoricoAssinaturaPagamentoRecorrencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssinaturasHistorico",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AssinaturaId = table.Column<int>(type: "integer", nullable: false),
                    PagamentoId = table.Column<int>(type: "integer", nullable: true),
                    Evento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StatusAnterior = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    StatusNovo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PlanoId = table.Column<int>(type: "integer", nullable: true),
                    PlanoAlteracaoPendenteId = table.Column<int>(type: "integer", nullable: true),
                    Observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    CreateAd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssinaturasHistorico", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssinaturasHistorico_Assinaturas_AssinaturaId",
                        column: x => x.AssinaturaId,
                        principalTable: "Assinaturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssinaturasHistorico_Pagamentos_PagamentoId",
                        column: x => x.PagamentoId,
                        principalTable: "Pagamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssinaturasRecorrenciasHistorico",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AssinaturaId = table.Column<int>(type: "integer", nullable: false),
                    PagamentoId = table.Column<int>(type: "integer", nullable: true),
                    Evento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CicloInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CicloFim = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RenovacaoAutomatica = table.Column<bool>(type: "boolean", nullable: false),
                    Observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    CreateAd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssinaturasRecorrenciasHistorico", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssinaturasRecorrenciasHistorico_Assinaturas_AssinaturaId",
                        column: x => x.AssinaturaId,
                        principalTable: "Assinaturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssinaturasRecorrenciasHistorico_Pagamentos_PagamentoId",
                        column: x => x.PagamentoId,
                        principalTable: "Pagamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PagamentosHistorico",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PagamentoId = table.Column<int>(type: "integer", nullable: false),
                    AssinaturaId = table.Column<int>(type: "integer", nullable: true),
                    Evento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StatusAnterior = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    StatusNovo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Gateway = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GatewayPaymentId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    MetodoPagamento = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Moeda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    CreateAd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagamentosHistorico", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PagamentosHistorico_Assinaturas_AssinaturaId",
                        column: x => x.AssinaturaId,
                        principalTable: "Assinaturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PagamentosHistorico_Pagamentos_PagamentoId",
                        column: x => x.PagamentoId,
                        principalTable: "Pagamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssinaturasHistorico_AssinaturaId",
                table: "AssinaturasHistorico",
                column: "AssinaturaId");

            migrationBuilder.CreateIndex(
                name: "IX_AssinaturasHistorico_Evento",
                table: "AssinaturasHistorico",
                column: "Evento");

            migrationBuilder.CreateIndex(
                name: "IX_AssinaturasHistorico_PagamentoId",
                table: "AssinaturasHistorico",
                column: "PagamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_AssinaturasRecorrenciasHistorico_AssinaturaId",
                table: "AssinaturasRecorrenciasHistorico",
                column: "AssinaturaId");

            migrationBuilder.CreateIndex(
                name: "IX_AssinaturasRecorrenciasHistorico_Evento",
                table: "AssinaturasRecorrenciasHistorico",
                column: "Evento");

            migrationBuilder.CreateIndex(
                name: "IX_AssinaturasRecorrenciasHistorico_PagamentoId",
                table: "AssinaturasRecorrenciasHistorico",
                column: "PagamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_PagamentosHistorico_AssinaturaId",
                table: "PagamentosHistorico",
                column: "AssinaturaId");

            migrationBuilder.CreateIndex(
                name: "IX_PagamentosHistorico_Evento",
                table: "PagamentosHistorico",
                column: "Evento");

            migrationBuilder.CreateIndex(
                name: "IX_PagamentosHistorico_PagamentoId",
                table: "PagamentosHistorico",
                column: "PagamentoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssinaturasHistorico");

            migrationBuilder.DropTable(
                name: "AssinaturasRecorrenciasHistorico");

            migrationBuilder.DropTable(
                name: "PagamentosHistorico");
        }
    }
}
