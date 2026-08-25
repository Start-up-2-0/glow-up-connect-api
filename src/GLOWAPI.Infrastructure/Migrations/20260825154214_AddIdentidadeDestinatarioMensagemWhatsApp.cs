using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentidadeDestinatarioMensagemWhatsApp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EhVerificacaoWhatsApp",
                table: "MensagensNotificacao",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "UsuarioId",
                table: "MensagensNotificacao",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MensagensNotificacao_UsuarioId",
                table: "MensagensNotificacao",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MensagensNotificacao_UsuarioId",
                table: "MensagensNotificacao");

            migrationBuilder.DropColumn(
                name: "EhVerificacaoWhatsApp",
                table: "MensagensNotificacao");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "MensagensNotificacao");
        }
    }
}
