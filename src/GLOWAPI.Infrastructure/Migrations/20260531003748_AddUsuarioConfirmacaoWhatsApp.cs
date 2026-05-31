using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUsuarioConfirmacaoWhatsApp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WhatsAppConfirmacaoCodigoHash",
                table: "Usuarios",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WhatsAppConfirmacaoExpiraEm",
                table: "Usuarios",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WhatsAppConfirmacaoTokenHash",
                table: "Usuarios",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WhatsAppConfirmadoEm",
                table: "Usuarios",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WhatsAppOptIn",
                table: "Usuarios",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_WhatsAppConfirmacaoCodigoHash",
                table: "Usuarios",
                column: "WhatsAppConfirmacaoCodigoHash");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_WhatsAppConfirmacaoTokenHash",
                table: "Usuarios",
                column: "WhatsAppConfirmacaoTokenHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuarios_WhatsAppConfirmacaoCodigoHash",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_WhatsAppConfirmacaoTokenHash",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "WhatsAppConfirmacaoCodigoHash",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "WhatsAppConfirmacaoExpiraEm",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "WhatsAppConfirmacaoTokenHash",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "WhatsAppConfirmadoEm",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "WhatsAppOptIn",
                table: "Usuarios");
        }
    }
}
