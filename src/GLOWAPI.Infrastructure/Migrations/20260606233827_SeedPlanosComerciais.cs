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
                DELETE FROM "Planos" AS p
                WHERE p."Nome" NOT IN ('Basic', 'Plus', 'Premium')
                  AND NOT EXISTS (
                      SELECT 1
                      FROM "Assinaturas" AS a
                      WHERE a."PlanoId" = p."Id");

                INSERT INTO "Planos" (
                    "Id",
                    "Nome",
                    "Descricao",
                    "Preco",
                    "Periodo",
                    "LimiteProfissionais",
                    "LimiteServicos",
                    "LimiteAgendamentos",
                    "Ativo",
                    "CreateAd")
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
                        NOW()
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
                        NOW()
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
                        NOW()
                    )
                ON CONFLICT ("Nome") DO UPDATE SET
                    "Descricao" = EXCLUDED."Descricao",
                    "Preco" = EXCLUDED."Preco",
                    "Periodo" = EXCLUDED."Periodo",
                    "LimiteProfissionais" = EXCLUDED."LimiteProfissionais",
                    "LimiteServicos" = EXCLUDED."LimiteServicos",
                    "LimiteAgendamentos" = EXCLUDED."LimiteAgendamentos",
                    "Ativo" = EXCLUDED."Ativo",
                    "UpdatedAt" = NOW();

                SELECT setval(
                    pg_get_serial_sequence('"Planos"', 'Id'),
                    GREATEST(
                        (SELECT COALESCE(MAX("Id"), 1) FROM "Planos"),
                        3));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "Planos" AS p
                WHERE p."Nome" IN ('Basic', 'Plus', 'Premium')
                  AND NOT EXISTS (
                      SELECT 1
                      FROM "Assinaturas" AS a
                      WHERE a."PlanoId" = p."Id");
                """);
        }
    }
}
