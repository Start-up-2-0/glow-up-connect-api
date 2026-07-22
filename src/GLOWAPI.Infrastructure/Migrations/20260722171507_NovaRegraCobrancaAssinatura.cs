using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NovaRegraCobrancaAssinatura : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "VisivelPublicamente",
                table: "Estabelecimentos",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataReferenciaCiclo",
                table: "Assinaturas",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.Sql(
                """
                UPDATE Assinaturas
                SET DataReferenciaCiclo = DATE(COALESCE(NULLIF(Inicio, '0001-01-01 00:00:00'), CreateAd))
                WHERE DataReferenciaCiclo = '0001-01-01 00:00:00';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VisivelPublicamente",
                table: "Estabelecimentos");

            migrationBuilder.DropColumn(
                name: "DataReferenciaCiclo",
                table: "Assinaturas");
        }
    }
}
