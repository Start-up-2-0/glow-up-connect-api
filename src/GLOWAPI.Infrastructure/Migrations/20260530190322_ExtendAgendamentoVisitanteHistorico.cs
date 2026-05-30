using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExtendAgendamentoVisitanteHistorico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "UsuarioClienteId",
                table: "Agendamentos",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "ClienteEmail",
                table: "Agendamentos",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClienteNome",
                table: "Agendamentos",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClienteTelefone",
                table: "Agendamentos",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Origem",
                table: "Agendamentos",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "PublicoLoja");

            migrationBuilder.Sql("""
                UPDATE "Agendamentos"
                SET "Status" = 'PendenteConfirmacao'
                WHERE "Status" = 'PendentePagamento';
                """);

            migrationBuilder.CreateTable(
                name: "AgendamentosHistorico",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AgendamentoId = table.Column<int>(type: "integer", nullable: false),
                    UsuarioExecutorId = table.Column<int>(type: "integer", nullable: true),
                    StatusAnterior = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StatusNovo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgendamentosHistorico", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgendamentosHistorico_Agendamentos_AgendamentoId",
                        column: x => x.AgendamentoId,
                        principalTable: "Agendamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgendamentosHistorico_Usuarios_UsuarioExecutorId",
                        column: x => x.UsuarioExecutorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgendamentosHistorico_AgendamentoId",
                table: "AgendamentosHistorico",
                column: "AgendamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_AgendamentosHistorico_CriadoEm",
                table: "AgendamentosHistorico",
                column: "CriadoEm");

            migrationBuilder.CreateIndex(
                name: "IX_AgendamentosHistorico_UsuarioExecutorId",
                table: "AgendamentosHistorico",
                column: "UsuarioExecutorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgendamentosHistorico");

            migrationBuilder.DropColumn(
                name: "ClienteEmail",
                table: "Agendamentos");

            migrationBuilder.DropColumn(
                name: "ClienteNome",
                table: "Agendamentos");

            migrationBuilder.DropColumn(
                name: "ClienteTelefone",
                table: "Agendamentos");

            migrationBuilder.DropColumn(
                name: "Origem",
                table: "Agendamentos");

            migrationBuilder.AlterColumn<int>(
                name: "UsuarioClienteId",
                table: "Agendamentos",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
