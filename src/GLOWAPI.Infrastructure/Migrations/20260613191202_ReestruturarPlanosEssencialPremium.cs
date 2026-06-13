using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReestruturarPlanosEssencialPremium : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LimiteEstabelecimentos",
                table: "Planos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AssinaturaEstabelecimentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AssinaturaId = table.Column<int>(type: "int", nullable: false),
                    EstabelecimentoId = table.Column<int>(type: "int", nullable: false),
                    EhMatriz = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    CreateAd = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssinaturaEstabelecimentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssinaturaEstabelecimentos_Assinaturas_AssinaturaId",
                        column: x => x.AssinaturaId,
                        principalTable: "Assinaturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssinaturaEstabelecimentos_Estabelecimentos_EstabelecimentoId",
                        column: x => x.EstabelecimentoId,
                        principalTable: "Estabelecimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AssinaturaEstabelecimentos_AssinaturaId",
                table: "AssinaturaEstabelecimentos",
                column: "AssinaturaId");

            migrationBuilder.CreateIndex(
                name: "IX_AssinaturaEstabelecimentos_EstabelecimentoId",
                table: "AssinaturaEstabelecimentos",
                column: "EstabelecimentoId",
                unique: true);

            migrationBuilder.Sql("""
                UPDATE `Assinaturas`
                SET `PlanoId` = 2
                WHERE `PlanoId` = 1;

                UPDATE `Planos`
                SET
                    `Nome` = 'Essencial',
                    `Descricao` = 'Operacao completa com equipe, WhatsApp e gestao para uma unidade',
                    `Preco` = 79.90,
                    `LimiteProfissionais` = NULL,
                    `LimiteServicos` = NULL,
                    `LimiteAgendamentos` = NULL,
                    `LimiteEstabelecimentos` = 1,
                    `Ativo` = TRUE,
                    `UpdatedAt` = UTC_TIMESTAMP()
                WHERE `Id` = 2;

                UPDATE `Planos`
                SET
                    `Ativo` = FALSE,
                    `UpdatedAt` = UTC_TIMESTAMP()
                WHERE `Id` = 1;

                UPDATE `Planos`
                SET
                    `Descricao` = 'Caixa, financeiro, comissoes, ate 5 unidades e prioridade no marketplace',
                    `LimiteEstabelecimentos` = 5,
                    `UpdatedAt` = UTC_TIMESTAMP()
                WHERE `Id` = 3;

                INSERT INTO `AssinaturaEstabelecimentos` (`AssinaturaId`, `EstabelecimentoId`, `EhMatriz`, `CreateAd`)
                SELECT a.`Id`, a.`EstabelecimentoId`, TRUE, UTC_TIMESTAMP()
                FROM `Assinaturas` AS a
                INNER JOIN `Planos` AS p ON p.`Id` = a.`PlanoId`
                WHERE a.`EstabelecimentoId` IS NOT NULL
                  AND p.`Nome` = 'Premium'
                  AND a.`Status` IN ('Ativa', 'Trial')
                  AND NOT EXISTS (
                      SELECT 1
                      FROM `AssinaturaEstabelecimentos` AS ae
                      WHERE ae.`EstabelecimentoId` = a.`EstabelecimentoId`);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssinaturaEstabelecimentos");

            migrationBuilder.DropColumn(
                name: "LimiteEstabelecimentos",
                table: "Planos");
        }
    }
}
