using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCampanhaPromocionalECicloCobranca : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CicloFim",
                table: "Pagamentos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CicloInicio",
                table: "Pagamentos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataGeracao",
                table: "Pagamentos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataVencimento",
                table: "Pagamentos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NumeroCiclo",
                table: "Pagamentos",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "TipoCobranca",
                table: "Pagamentos",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CampanhaPromocionalId",
                table: "Assinaturas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DiaVencimento",
                table: "Assinaturas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProximaDataAlerta",
                table: "Assinaturas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProximaDataGeracaoCobranca",
                table: "Assinaturas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProximaDataVencimento",
                table: "Assinaturas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimoAlertaFaturaEm",
                table: "Assinaturas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CampanhasPromocionais",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Limite = table.Column<int>(type: "integer", nullable: false),
                    Utilizados = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    DiasTrial = table.Column<int>(type: "integer", nullable: false),
                    Ativa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreateAd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampanhasPromocionais", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assinaturas_CampanhaPromocionalId",
                table: "Assinaturas",
                column: "CampanhaPromocionalId");

            migrationBuilder.CreateIndex(
                name: "IX_CampanhasPromocionais_Codigo",
                table: "CampanhasPromocionais",
                column: "Codigo",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Assinaturas_CampanhasPromocionais_CampanhaPromocionalId",
                table: "Assinaturas",
                column: "CampanhaPromocionalId",
                principalTable: "CampanhasPromocionais",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                UPDATE "Assinaturas"
                SET "DiaVencimento" = 10
                WHERE "DiaVencimento" = 0;
                """);

            migrationBuilder.InsertData(
                table: "CampanhasPromocionais",
                columns: ["Codigo", "Limite", "Utilizados", "DiasTrial", "Ativa", "CreateAd"],
                values: ["lancamento-100", 100, 0, 30, true, DateTime.UtcNow]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assinaturas_CampanhasPromocionais_CampanhaPromocionalId",
                table: "Assinaturas");

            migrationBuilder.DropTable(
                name: "CampanhasPromocionais");

            migrationBuilder.DropIndex(
                name: "IX_Assinaturas_CampanhaPromocionalId",
                table: "Assinaturas");

            migrationBuilder.DropColumn(
                name: "CicloFim",
                table: "Pagamentos");

            migrationBuilder.DropColumn(
                name: "CicloInicio",
                table: "Pagamentos");

            migrationBuilder.DropColumn(
                name: "DataGeracao",
                table: "Pagamentos");

            migrationBuilder.DropColumn(
                name: "DataVencimento",
                table: "Pagamentos");

            migrationBuilder.DropColumn(
                name: "NumeroCiclo",
                table: "Pagamentos");

            migrationBuilder.DropColumn(
                name: "TipoCobranca",
                table: "Pagamentos");

            migrationBuilder.DropColumn(
                name: "CampanhaPromocionalId",
                table: "Assinaturas");

            migrationBuilder.DropColumn(
                name: "DiaVencimento",
                table: "Assinaturas");

            migrationBuilder.DropColumn(
                name: "ProximaDataAlerta",
                table: "Assinaturas");

            migrationBuilder.DropColumn(
                name: "ProximaDataGeracaoCobranca",
                table: "Assinaturas");

            migrationBuilder.DropColumn(
                name: "ProximaDataVencimento",
                table: "Assinaturas");

            migrationBuilder.DropColumn(
                name: "UltimoAlertaFaturaEm",
                table: "Assinaturas");
        }
    }
}
