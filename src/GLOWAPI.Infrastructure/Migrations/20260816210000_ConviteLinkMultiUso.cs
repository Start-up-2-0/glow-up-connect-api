using GLOWAPI.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260816210000_ConviteLinkMultiUso")]
    public partial class ConviteLinkMultiUso : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LimiteUsuarios",
                table: "ConvitesNegocio",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "QuantidadeUtilizacoes",
                table: "ConvitesNegocio",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE `ConvitesNegocio`
                SET `Status` = 'Cancelado',
                    `UpdatedAt` = UTC_TIMESTAMP()
                WHERE `Status` IN ('Pendente', 'Aceito', 'Rejeitado');
                """);

            migrationBuilder.DropIndex(
                name: "IX_ConvitesNegocio_EstabelecimentoId_Email_TipoConvite_Status",
                table: "ConvitesNegocio");

            migrationBuilder.CreateIndex(
                name: "IX_ConvitesNegocio_EstabelecimentoId_Status",
                table: "ConvitesNegocio",
                columns: new[] { "EstabelecimentoId", "Status" });

            migrationBuilder.CreateTable(
                name: "ConvitesNegocioUtilizacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ConviteNegocioId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    UtilizadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConvitesNegocioUtilizacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConvitesNegocioUtilizacoes_ConvitesNegocio_ConviteNegocioId",
                        column: x => x.ConviteNegocioId,
                        principalTable: "ConvitesNegocio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConvitesNegocioUtilizacoes_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConvitesNegocioUtilizacoes_ConviteNegocioId_UsuarioId",
                table: "ConvitesNegocioUtilizacoes",
                columns: new[] { "ConviteNegocioId", "UsuarioId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConvitesNegocioUtilizacoes_UsuarioId",
                table: "ConvitesNegocioUtilizacoes",
                column: "UsuarioId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ConvitesNegocioUtilizacoes");

            migrationBuilder.DropIndex(
                name: "IX_ConvitesNegocio_EstabelecimentoId_Status",
                table: "ConvitesNegocio");

            migrationBuilder.DropColumn(name: "LimiteUsuarios", table: "ConvitesNegocio");
            migrationBuilder.DropColumn(name: "QuantidadeUtilizacoes", table: "ConvitesNegocio");

            migrationBuilder.CreateIndex(
                name: "IX_ConvitesNegocio_EstabelecimentoId_Email_TipoConvite_Status",
                table: "ConvitesNegocio",
                columns: new[] { "EstabelecimentoId", "Email", "TipoConvite", "Status" });
        }
    }
}
