using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProfissionalVitrineSomenteExibicao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Profissionais_UsuarioId",
                table: "Profissionais");

            migrationBuilder.AddColumn<bool>(
                name: "SomenteExibicao",
                table: "ProfissionalEstabelecimentos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "UsuarioId",
                table: "Profissionais",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "IX_Profissionais_UsuarioId",
                table: "Profissionais",
                column: "UsuarioId",
                unique: true,
                filter: "\"UsuarioId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Profissionais_UsuarioId",
                table: "Profissionais");

            migrationBuilder.DropColumn(
                name: "SomenteExibicao",
                table: "ProfissionalEstabelecimentos");

            migrationBuilder.AlterColumn<int>(
                name: "UsuarioId",
                table: "Profissionais",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Profissionais_UsuarioId",
                table: "Profissionais",
                column: "UsuarioId",
                unique: true);
        }
    }
}
