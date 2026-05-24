using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorSessaoAutenticacaoGlowToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessTokenJti",
                table: "SessoesAutenticacao");

            migrationBuilder.AddColumn<DateTime>(
                name: "BloqueadoAte",
                table: "Usuarios",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AccessTokenExpiraEm",
                table: "SessoesAutenticacao",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "AccessTokenHash",
                table: "SessoesAutenticacao",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimaRenovacaoEm",
                table: "SessoesAutenticacao",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LogsAutenticacao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: true),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Evento = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Detalhes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogsAutenticacao", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessoesAutenticacao_AccessTokenHash",
                table: "SessoesAutenticacao",
                column: "AccessTokenHash");

            migrationBuilder.CreateIndex(
                name: "IX_LogsAutenticacao_CreatedAt",
                table: "LogsAutenticacao",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LogsAutenticacao_Evento",
                table: "LogsAutenticacao",
                column: "Evento");

            migrationBuilder.CreateIndex(
                name: "IX_LogsAutenticacao_UsuarioId",
                table: "LogsAutenticacao",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogsAutenticacao");

            migrationBuilder.DropIndex(
                name: "IX_SessoesAutenticacao_AccessTokenHash",
                table: "SessoesAutenticacao");

            migrationBuilder.DropColumn(
                name: "BloqueadoAte",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "AccessTokenExpiraEm",
                table: "SessoesAutenticacao");

            migrationBuilder.DropColumn(
                name: "AccessTokenHash",
                table: "SessoesAutenticacao");

            migrationBuilder.DropColumn(
                name: "UltimaRenovacaoEm",
                table: "SessoesAutenticacao");

            migrationBuilder.AddColumn<string>(
                name: "AccessTokenJti",
                table: "SessoesAutenticacao",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");
        }
    }
}
