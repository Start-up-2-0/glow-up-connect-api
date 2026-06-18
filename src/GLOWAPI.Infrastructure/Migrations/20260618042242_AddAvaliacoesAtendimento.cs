using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAvaliacoesAtendimento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "NotaMedia",
                table: "Profissionais",
                type: "decimal(65,30)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalAvaliacoes",
                table: "Profissionais",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "NotaMedia",
                table: "Estabelecimentos",
                type: "decimal(65,30)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalAvaliacoes",
                table: "Estabelecimentos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AvaliacoesAtendimento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AgendamentoId = table.Column<int>(type: "int", nullable: false),
                    UsuarioClienteId = table.Column<int>(type: "int", nullable: true),
                    EstabelecimentoId = table.Column<int>(type: "int", nullable: false),
                    ProfissionalId = table.Column<int>(type: "int", nullable: false),
                    NotaEstabelecimento = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    ComentarioEstabelecimento = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NotaProfissional = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    ComentarioProfissional = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvaliadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Origem = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvaliacoesAtendimento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AvaliacoesAtendimento_Agendamentos_AgendamentoId",
                        column: x => x.AgendamentoId,
                        principalTable: "Agendamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AvaliacoesAtendimento_Estabelecimentos_EstabelecimentoId",
                        column: x => x.EstabelecimentoId,
                        principalTable: "Estabelecimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AvaliacoesAtendimento_Profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AvaliacoesAtendimento_Usuarios_UsuarioClienteId",
                        column: x => x.UsuarioClienteId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AvaliacoesConvites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AgendamentoId = table.Column<int>(type: "int", nullable: false),
                    TokenPublico = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ExpiraEm = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UtilizadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvaliacoesConvites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AvaliacoesConvites_Agendamentos_AgendamentoId",
                        column: x => x.AgendamentoId,
                        principalTable: "Agendamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AvaliacoesHistorico",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AvaliacaoAtendimentoId = table.Column<int>(type: "int", nullable: false),
                    NotaEstabelecimentoAnterior = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    ComentarioEstabelecimentoAnterior = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NotaProfissionalAnterior = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    ComentarioProfissionalAnterior = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AlteradoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    Motivo = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AlteradoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvaliacoesHistorico", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AvaliacoesHistorico_AvaliacoesAtendimento_AvaliacaoAtendimen~",
                        column: x => x.AvaliacaoAtendimentoId,
                        principalTable: "AvaliacoesAtendimento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AvaliacoesHistorico_Usuarios_AlteradoPorUsuarioId",
                        column: x => x.AlteradoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AvaliacoesAtendimento_AgendamentoId",
                table: "AvaliacoesAtendimento",
                column: "AgendamentoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AvaliacoesAtendimento_EstabelecimentoId_AvaliadoEm",
                table: "AvaliacoesAtendimento",
                columns: new[] { "EstabelecimentoId", "AvaliadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_AvaliacoesAtendimento_ProfissionalId_AvaliadoEm",
                table: "AvaliacoesAtendimento",
                columns: new[] { "ProfissionalId", "AvaliadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_AvaliacoesAtendimento_UsuarioClienteId",
                table: "AvaliacoesAtendimento",
                column: "UsuarioClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_AvaliacoesConvites_AgendamentoId",
                table: "AvaliacoesConvites",
                column: "AgendamentoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AvaliacoesConvites_TokenPublico",
                table: "AvaliacoesConvites",
                column: "TokenPublico",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AvaliacoesHistorico_AlteradoPorUsuarioId",
                table: "AvaliacoesHistorico",
                column: "AlteradoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_AvaliacoesHistorico_AvaliacaoAtendimentoId",
                table: "AvaliacoesHistorico",
                column: "AvaliacaoAtendimentoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AvaliacoesConvites");

            migrationBuilder.DropTable(
                name: "AvaliacoesHistorico");

            migrationBuilder.DropTable(
                name: "AvaliacoesAtendimento");

            migrationBuilder.DropColumn(
                name: "NotaMedia",
                table: "Profissionais");

            migrationBuilder.DropColumn(
                name: "TotalAvaliacoes",
                table: "Profissionais");

            migrationBuilder.DropColumn(
                name: "NotaMedia",
                table: "Estabelecimentos");

            migrationBuilder.DropColumn(
                name: "TotalAvaliacoes",
                table: "Estabelecimentos");
        }
    }
}
