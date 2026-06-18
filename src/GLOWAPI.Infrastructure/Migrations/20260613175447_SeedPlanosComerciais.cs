using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedPlanosComerciais : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE p FROM `Planos` AS p
                WHERE p.`Nome` NOT IN ('Basic', 'Plus', 'Premium')
                  AND NOT EXISTS (
                      SELECT 1
                      FROM `Assinaturas` AS a
                      WHERE a.`PlanoId` = p.`Id`);

                INSERT INTO `Planos` (
                    `Id`,
                    `Nome`,
                    `Descricao`,
                    `Preco`,
                    `Periodo`,
                    `LimiteProfissionais`,
                    `LimiteServicos`,
                    `LimiteAgendamentos`,
                    `Ativo`,
                    `CreateAd`)
                VALUES
                    (
                        1,
                        'Basic',
                        'Plano de entrada para operacao solo com agenda, servicos e e-mail',
                        29.99,
                        'Mensal',
                        1,
                        10,
                        10,
                        TRUE,
                        UTC_TIMESTAMP()
                    ),
                    (
                        2,
                        'Plus',
                        'Equipe, convites e notificacoes WhatsApp para o negocio',
                        99.90,
                        'Mensal',
                        NULL,
                        NULL,
                        NULL,
                        TRUE,
                        UTC_TIMESTAMP()
                    ),
                    (
                        3,
                        'Premium',
                        'Caixa, financeiro, comissoes e prioridade no marketplace',
                        199.90,
                        'Mensal',
                        NULL,
                        NULL,
                        NULL,
                        TRUE,
                        UTC_TIMESTAMP()
                    )
                ON DUPLICATE KEY UPDATE
                    `Descricao` = VALUES(`Descricao`),
                    `Preco` = VALUES(`Preco`),
                    `Periodo` = VALUES(`Periodo`),
                    `LimiteProfissionais` = VALUES(`LimiteProfissionais`),
                    `LimiteServicos` = VALUES(`LimiteServicos`),
                    `LimiteAgendamentos` = VALUES(`LimiteAgendamentos`),
                    `Ativo` = VALUES(`Ativo`),
                    `UpdatedAt` = UTC_TIMESTAMP();

                SET @max_plano_id = (SELECT COALESCE(MAX(`Id`), 1) FROM `Planos`);
                SET @sql = CONCAT('ALTER TABLE `Planos` AUTO_INCREMENT = ', GREATEST(@max_plano_id, 3));
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE p FROM `Planos` AS p
                WHERE p.`Nome` IN ('Basic', 'Plus', 'Premium')
                  AND NOT EXISTS (
                      SELECT 1
                      FROM `Assinaturas` AS a
                      WHERE a.`PlanoId` = p.`Id`);
                """);
        }
    }
}
