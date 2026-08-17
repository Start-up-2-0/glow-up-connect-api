-- Diagnóstico de storage do banco Glow Up Connect API
-- Executar antes e depois da recompactação de imagens e do worker de retenção (30 dias).

-- 1) Tamanho por tabela
SELECT
    table_name AS tabela,
    ROUND((data_length + index_length) / 1024 / 1024, 2) AS size_mb,
    table_rows AS linhas_estimadas
FROM information_schema.tables
WHERE table_schema = DATABASE()
ORDER BY (data_length + index_length) DESC
LIMIT 15;

-- 2) Total em imagens Base64 (longtext)
SELECT 'Usuarios' AS tabela,
       COUNT(*) AS com_foto,
       ROUND(AVG(LENGTH(AvatarBase64)) / 1024, 1) AS media_kb,
       ROUND(SUM(LENGTH(AvatarBase64)) / 1024 / 1024, 1) AS total_mb
FROM Usuarios
WHERE AvatarBase64 IS NOT NULL AND AvatarBase64 != ''
UNION ALL
SELECT 'Estabelecimentos',
       COUNT(*),
       ROUND(AVG(LENGTH(Logo)) / 1024, 1),
       ROUND(SUM(LENGTH(Logo)) / 1024 / 1024, 1)
FROM Estabelecimentos
WHERE Logo != ''
UNION ALL
SELECT 'Profissionais',
       COUNT(*),
       ROUND(AVG(LENGTH(Logo)) / 1024, 1),
       ROUND(SUM(LENGTH(Logo)) / 1024 / 1024, 1)
FROM Profissionais
WHERE Logo != '';

-- 3) Webhooks e logs acumulados
SELECT
    ROUND(SUM(LENGTH(Payload)) / 1024 / 1024, 1) AS webhook_payload_mb,
    COUNT(*) AS total_webhooks
FROM WebhookPagamentos;

SELECT
    ROUND(SUM(LENGTH(RequestPayload) + LENGTH(ResponsePayload)) / 1024 / 1024, 1) AS logs_notificacao_mb,
    COUNT(*) AS total_logs
FROM MensagensNotificacaoLogs;

-- 4) Pós-deploy: habilitar recompactação única (Railway/env)
-- CompactacaoImagensWorker__Habilitado=true
-- Após varredura completa nos logs ("concluiu varredura completa"), voltar para false.
