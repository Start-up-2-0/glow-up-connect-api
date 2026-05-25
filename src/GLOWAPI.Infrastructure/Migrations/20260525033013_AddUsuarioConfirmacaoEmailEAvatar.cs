using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUsuarioConfirmacaoEmailEAvatar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvatarBase64",
                table: "Usuarios",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConfirmacaoCodigoHash",
                table: "Usuarios",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmacaoExpiraEm",
                table: "Usuarios",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConfirmacaoTokenHash",
                table: "Usuarios",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_ConfirmacaoCodigoHash",
                table: "Usuarios",
                column: "ConfirmacaoCodigoHash");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_ConfirmacaoTokenHash",
                table: "Usuarios",
                column: "ConfirmacaoTokenHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuarios_ConfirmacaoCodigoHash",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_ConfirmacaoTokenHash",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "AvatarBase64",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "ConfirmacaoCodigoHash",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "ConfirmacaoExpiraEm",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "ConfirmacaoTokenHash",
                table: "Usuarios");
        }
    }
}
