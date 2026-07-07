using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ModuloFinanceiroCompleto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConciliacaoStatus",
                table: "LancamentosCaixa",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pendente")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "LancamentoOriginalId",
                table: "LancamentosCaixa",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SessaoCaixaId",
                table: "LancamentosCaixa",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ExigirSessaoCaixaAberta",
                table: "Caixas",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ConciliacaoItens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EstabelecimentoId = table.Column<int>(type: "int", nullable: false),
                    LancamentoCaixaId = table.Column<int>(type: "int", nullable: true),
                    DescricaoExtrato = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ValorExtrato = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    DataExtrato = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ReferenciaExtrato = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Conciliado = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreateAd = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConciliacaoItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConciliacaoItens_Estabelecimentos_EstabelecimentoId",
                        column: x => x.EstabelecimentoId,
                        principalTable: "Estabelecimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConciliacaoItens_LancamentosCaixa_LancamentoCaixaId",
                        column: x => x.LancamentoCaixaId,
                        principalTable: "LancamentosCaixa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ContasPagar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EstabelecimentoId = table.Column<int>(type: "int", nullable: false),
                    LancamentoCaixaId = table.Column<int>(type: "int", nullable: true),
                    Fornecedor = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Categoria = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descricao = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Valor = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Vencimento = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Recorrente = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreateAd = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContasPagar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContasPagar_Estabelecimentos_EstabelecimentoId",
                        column: x => x.EstabelecimentoId,
                        principalTable: "Estabelecimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContasPagar_LancamentosCaixa_LancamentoCaixaId",
                        column: x => x.LancamentoCaixaId,
                        principalTable: "LancamentosCaixa",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ContasReceber",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EstabelecimentoId = table.Column<int>(type: "int", nullable: false),
                    AgendamentoId = table.Column<int>(type: "int", nullable: true),
                    PagamentoId = table.Column<int>(type: "int", nullable: true),
                    LancamentoCaixaId = table.Column<int>(type: "int", nullable: true),
                    Descricao = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Valor = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Vencimento = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreateAd = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContasReceber", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContasReceber_Agendamentos_AgendamentoId",
                        column: x => x.AgendamentoId,
                        principalTable: "Agendamentos",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ContasReceber_Estabelecimentos_EstabelecimentoId",
                        column: x => x.EstabelecimentoId,
                        principalTable: "Estabelecimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContasReceber_LancamentosCaixa_LancamentoCaixaId",
                        column: x => x.LancamentoCaixaId,
                        principalTable: "LancamentosCaixa",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ContasReceber_Pagamentos_PagamentoId",
                        column: x => x.PagamentoId,
                        principalTable: "Pagamentos",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SessoesCaixa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CaixaId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    AbertoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    SaldoInicial = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    SaldoInformadoFechamento = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    Diferenca = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreateAd = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessoesCaixa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessoesCaixa_Caixas_CaixaId",
                        column: x => x.CaixaId,
                        principalTable: "Caixas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessoesCaixa_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_LancamentosCaixa_LancamentoOriginalId",
                table: "LancamentosCaixa",
                column: "LancamentoOriginalId");

            migrationBuilder.CreateIndex(
                name: "IX_LancamentosCaixa_SessaoCaixaId",
                table: "LancamentosCaixa",
                column: "SessaoCaixaId");

            migrationBuilder.CreateIndex(
                name: "IX_ConciliacaoItens_EstabelecimentoId",
                table: "ConciliacaoItens",
                column: "EstabelecimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_ConciliacaoItens_LancamentoCaixaId",
                table: "ConciliacaoItens",
                column: "LancamentoCaixaId");

            migrationBuilder.CreateIndex(
                name: "IX_ContasPagar_EstabelecimentoId",
                table: "ContasPagar",
                column: "EstabelecimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_ContasPagar_LancamentoCaixaId",
                table: "ContasPagar",
                column: "LancamentoCaixaId");

            migrationBuilder.CreateIndex(
                name: "IX_ContasReceber_AgendamentoId",
                table: "ContasReceber",
                column: "AgendamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_ContasReceber_EstabelecimentoId",
                table: "ContasReceber",
                column: "EstabelecimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_ContasReceber_LancamentoCaixaId",
                table: "ContasReceber",
                column: "LancamentoCaixaId");

            migrationBuilder.CreateIndex(
                name: "IX_ContasReceber_PagamentoId",
                table: "ContasReceber",
                column: "PagamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_SessoesCaixa_CaixaId_Status",
                table: "SessoesCaixa",
                columns: new[] { "CaixaId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SessoesCaixa_UsuarioId",
                table: "SessoesCaixa",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_LancamentosCaixa_LancamentosCaixa_LancamentoOriginalId",
                table: "LancamentosCaixa",
                column: "LancamentoOriginalId",
                principalTable: "LancamentosCaixa",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LancamentosCaixa_SessoesCaixa_SessaoCaixaId",
                table: "LancamentosCaixa",
                column: "SessaoCaixaId",
                principalTable: "SessoesCaixa",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LancamentosCaixa_LancamentosCaixa_LancamentoOriginalId",
                table: "LancamentosCaixa");

            migrationBuilder.DropForeignKey(
                name: "FK_LancamentosCaixa_SessoesCaixa_SessaoCaixaId",
                table: "LancamentosCaixa");

            migrationBuilder.DropTable(
                name: "ConciliacaoItens");

            migrationBuilder.DropTable(
                name: "ContasPagar");

            migrationBuilder.DropTable(
                name: "ContasReceber");

            migrationBuilder.DropTable(
                name: "SessoesCaixa");

            migrationBuilder.DropIndex(
                name: "IX_LancamentosCaixa_LancamentoOriginalId",
                table: "LancamentosCaixa");

            migrationBuilder.DropIndex(
                name: "IX_LancamentosCaixa_SessaoCaixaId",
                table: "LancamentosCaixa");

            migrationBuilder.DropColumn(
                name: "ConciliacaoStatus",
                table: "LancamentosCaixa");

            migrationBuilder.DropColumn(
                name: "LancamentoOriginalId",
                table: "LancamentosCaixa");

            migrationBuilder.DropColumn(
                name: "SessaoCaixaId",
                table: "LancamentosCaixa");

            migrationBuilder.DropColumn(
                name: "ExigirSessaoCaixaAberta",
                table: "Caixas");
        }
    }
}
