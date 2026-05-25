using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateMensagensNotificacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MensagensNotificacao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Guid = table.Column<Guid>(type: "uuid", nullable: false),
                    EstabelecimentoId = table.Column<int>(type: "integer", nullable: true),
                    Canal = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Destinatario = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Assunto = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Conteudo = table.Column<string>(type: "text", nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Tentativas = table.Column<int>(type: "integer", nullable: false),
                    MaximoTentativas = table.Column<int>(type: "integer", nullable: false),
                    Prioridade = table.Column<int>(type: "integer", nullable: false),
                    Provedor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AgendadoPara = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProcessamentoIniciadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EnviadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FalhouEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MensagemErro = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    InstanciaWorker = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MensagensNotificacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MensagensNotificacao_Estabelecimentos_EstabelecimentoId",
                        column: x => x.EstabelecimentoId,
                        principalTable: "Estabelecimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MensagensNotificacaoLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MensagemNotificacaoId = table.Column<int>(type: "integer", nullable: false),
                    Tentativa = table.Column<int>(type: "integer", nullable: false),
                    RequestPayload = table.Column<string>(type: "text", nullable: true),
                    ResponsePayload = table.Column<string>(type: "text", nullable: true),
                    RespostaProvedor = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TempoExecucaoMs = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MensagemErro = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MensagensNotificacaoLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MensagensNotificacaoLogs_MensagensNotificacao_MensagemNotif~",
                        column: x => x.MensagemNotificacaoId,
                        principalTable: "MensagensNotificacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MensagensNotificacao_Canal",
                table: "MensagensNotificacao",
                column: "Canal");

            migrationBuilder.CreateIndex(
                name: "IX_MensagensNotificacao_EstabelecimentoId",
                table: "MensagensNotificacao",
                column: "EstabelecimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_MensagensNotificacao_Guid",
                table: "MensagensNotificacao",
                column: "Guid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MensagensNotificacao_Status_AgendadoPara_Prioridade_CriadoEm",
                table: "MensagensNotificacao",
                columns: new[] { "Status", "AgendadoPara", "Prioridade", "CriadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_MensagensNotificacao_Status_ProcessamentoIniciadoEm",
                table: "MensagensNotificacao",
                columns: new[] { "Status", "ProcessamentoIniciadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_MensagensNotificacaoLogs_CriadoEm",
                table: "MensagensNotificacaoLogs",
                column: "CriadoEm");

            migrationBuilder.CreateIndex(
                name: "IX_MensagensNotificacaoLogs_MensagemNotificacaoId",
                table: "MensagensNotificacaoLogs",
                column: "MensagemNotificacaoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MensagensNotificacaoLogs");

            migrationBuilder.DropTable(
                name: "MensagensNotificacao");
        }
    }
}
