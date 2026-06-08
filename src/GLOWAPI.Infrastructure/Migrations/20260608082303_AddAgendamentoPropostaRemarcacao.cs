using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAgendamentoPropostaRemarcacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgendamentosPropostasRemarcacao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AgendamentoId = table.Column<int>(type: "integer", nullable: false),
                    DataSugerida = table.Column<DateOnly>(type: "date", nullable: false),
                    HorarioInicioSugerido = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TokenPublico = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiraEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RespondidoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UsuarioExecutorId = table.Column<int>(type: "integer", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgendamentosPropostasRemarcacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgendamentosPropostasRemarcacao_Agendamentos_AgendamentoId",
                        column: x => x.AgendamentoId,
                        principalTable: "Agendamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgendamentosPropostasRemarcacao_Usuarios_UsuarioExecutorId",
                        column: x => x.UsuarioExecutorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgendamentosPropostasRemarcacao_AgendamentoId",
                table: "AgendamentosPropostasRemarcacao",
                column: "AgendamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_AgendamentosPropostasRemarcacao_TokenPublico",
                table: "AgendamentosPropostasRemarcacao",
                column: "TokenPublico",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgendamentosPropostasRemarcacao_UsuarioExecutorId",
                table: "AgendamentosPropostasRemarcacao",
                column: "UsuarioExecutorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgendamentosPropostasRemarcacao");
        }
    }
}
