-- Reverte role global de usuários promovidos incorretamente ao aceitar convite.
-- Mantém DonoEstabelecimento apenas para quem é Owner ativo em algum estabelecimento.
--
-- Executar em homologação antes de produção. Revisar o SELECT de pré-visualização.

BEGIN;

-- Pré-visualização: usuários que serão revertidos para Cliente
SELECT
    u."Id",
    u."Email",
    u."Role" AS role_atual,
    'Cliente' AS role_nova
FROM "Usuarios" u
WHERE u."Role" IN ('ProfissionalEstabelecimento', 'DonoEstabelecimento')
  AND EXISTS (
      SELECT 1
      FROM "EstabelecimentoUsuarios" eu
      WHERE eu."UsuarioId" = u."Id"
        AND eu."Ativo" = TRUE
  )
  AND NOT EXISTS (
      SELECT 1
      FROM "EstabelecimentoUsuarios" eu_owner
      WHERE eu_owner."UsuarioId" = u."Id"
        AND eu_owner."Ativo" = TRUE
        AND eu_owner."RoleNoEstabelecimento" = 'Owner'
  );

UPDATE "Usuarios" u
SET
    "Role" = 'Cliente',
    "UpdatedAt" = NOW() AT TIME ZONE 'UTC'
WHERE u."Role" IN ('ProfissionalEstabelecimento', 'DonoEstabelecimento')
  AND EXISTS (
      SELECT 1
      FROM "EstabelecimentoUsuarios" eu
      WHERE eu."UsuarioId" = u."Id"
        AND eu."Ativo" = TRUE
  )
  AND NOT EXISTS (
      SELECT 1
      FROM "EstabelecimentoUsuarios" eu_owner
      WHERE eu_owner."UsuarioId" = u."Id"
        AND eu_owner."Ativo" = TRUE
        AND eu_owner."RoleNoEstabelecimento" = 'Owner'
  );

-- Exemplo pontual (descomente se necessário):
-- UPDATE "Usuarios"
-- SET "Role" = 'Cliente', "UpdatedAt" = NOW() AT TIME ZONE 'UTC'
-- WHERE "Email" = 'jamesgustavo14@gmail.com'
--   AND "Role" <> 'Cliente'
--   AND NOT EXISTS (
--       SELECT 1 FROM "EstabelecimentoUsuarios" eu
--       WHERE eu."UsuarioId" = "Usuarios"."Id"
--         AND eu."Ativo" = TRUE
--         AND eu."RoleNoEstabelecimento" = 'Owner'
--   );

COMMIT;
