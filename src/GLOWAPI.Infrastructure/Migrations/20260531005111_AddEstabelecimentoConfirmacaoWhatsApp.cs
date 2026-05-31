using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEstabelecimentoConfirmacaoWhatsApp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WhatsAppConfirmacaoCodigoHash",
                table: "Estabelecimentos",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WhatsAppConfirmacaoExpiraEm",
                table: "Estabelecimentos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WhatsAppConfirmacaoTokenHash",
                table: "Estabelecimentos",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WhatsAppConfirmadoEm",
                table: "Estabelecimentos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WhatsAppOptIn",
                table: "Estabelecimentos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Estabelecimentos_WhatsAppConfirmacaoCodigoHash",
                table: "Estabelecimentos",
                column: "WhatsAppConfirmacaoCodigoHash");

            migrationBuilder.CreateIndex(
                name: "IX_Estabelecimentos_WhatsAppConfirmacaoTokenHash",
                table: "Estabelecimentos",
                column: "WhatsAppConfirmacaoTokenHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Estabelecimentos_WhatsAppConfirmacaoCodigoHash",
                table: "Estabelecimentos");

            migrationBuilder.DropIndex(
                name: "IX_Estabelecimentos_WhatsAppConfirmacaoTokenHash",
                table: "Estabelecimentos");

            migrationBuilder.DropColumn(
                name: "WhatsAppConfirmacaoCodigoHash",
                table: "Estabelecimentos");

            migrationBuilder.DropColumn(
                name: "WhatsAppConfirmacaoExpiraEm",
                table: "Estabelecimentos");

            migrationBuilder.DropColumn(
                name: "WhatsAppConfirmacaoTokenHash",
                table: "Estabelecimentos");

            migrationBuilder.DropColumn(
                name: "WhatsAppConfirmadoEm",
                table: "Estabelecimentos");

            migrationBuilder.DropColumn(
                name: "WhatsAppOptIn",
                table: "Estabelecimentos");
        }
    }
}
