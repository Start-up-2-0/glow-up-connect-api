using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAgendamentoHorarioPrincipal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Fim",
                table: "Agendamentos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Inicio",
                table: "Agendamentos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Agendamentos" AS a
                SET "Inicio" = sub."Inicio",
                    "Fim" = sub."Fim"
                FROM (
                    SELECT
                        i."AgendamentoId",
                        MIN(i."Inicio") AS "Inicio",
                        MAX(i."Fim") AS "Fim"
                    FROM "AgendamentoItens" AS i
                    GROUP BY i."AgendamentoId"
                ) AS sub
                WHERE a."Id" = sub."AgendamentoId";
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Agendamentos"
                SET "Inicio" = "CreateAd",
                    "Fim" = "CreateAd" + INTERVAL '1 minute'
                WHERE "Inicio" IS NULL OR "Fim" IS NULL;
                """);

            migrationBuilder.AlterColumn<DateTime>(
                name: "Fim",
                table: "Agendamentos",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "Inicio",
                table: "Agendamentos",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Agendamentos_Inicio",
                table: "Agendamentos",
                column: "Inicio");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Agendamentos_Horario",
                table: "Agendamentos",
                sql: "\"Inicio\" < \"Fim\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Agendamentos_Inicio",
                table: "Agendamentos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Agendamentos_Horario",
                table: "Agendamentos");

            migrationBuilder.DropColumn(
                name: "Fim",
                table: "Agendamentos");

            migrationBuilder.DropColumn(
                name: "Inicio",
                table: "Agendamentos");
        }
    }
}
