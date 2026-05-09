using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLOWAPI.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameUsuarioCreateAdToCreatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Tentivas",
                table: "Usuarios",
                newName: "Tentativas");

            migrationBuilder.RenameColumn(
                name: "Senha",
                table: "Usuarios",
                newName: "SenhaHash");

            migrationBuilder.RenameColumn(
                name: "CreateAd",
                table: "Usuarios",
                newName: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Tentativas",
                table: "Usuarios",
                newName: "Tentivas");

            migrationBuilder.RenameColumn(
                name: "SenhaHash",
                table: "Usuarios",
                newName: "Senha");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Usuarios",
                newName: "CreateAd");
        }
    }
}
