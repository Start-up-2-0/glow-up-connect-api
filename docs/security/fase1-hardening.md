# Fase 1 — Hardening de segurança

Configurações introduzidas no hardening inicial da GLOWAPI.

## Rate limit global (burst)

| Variável | Padrão | Descrição |
|----------|--------|-----------|
| `RateLimit__Enabled` | `true` | Liga/desliga o middleware |
| `RateLimit__BurstMaxRequests` | `3` | Requisições permitidas na janela |
| `RateLimit__BurstWindowSeconds` | `5` | Janela deslizante em segundos |
| `RateLimit__BlockDurationHours` | `24` | Duração do bloqueio por IP |

**Comportamento:** a partir da 4ª requisição na janela, o IP é bloqueado por 24h (`429`, código `IP_BLOCKED_24H`).

**Exclusões:** `OPTIONS`, `GET /health`.

**Suporte:** se um cliente legítimo for bloqueado, verificar tabela `IpRateLimitBlocks` e aguardar expiração ou remover o registro manualmente em emergência.

## CORS

Configure origens explícitas:

```bash
Cors__AllowedOrigins__0=https://app.seudominio.com
Cors__AllowedOrigins__1=https://staging.seudominio.com
```

## Mercado Pago webhook

```bash
MercadoPago__WebhookSecret=<secret do painel MP>
```

URL de notificação (Checkout Pro): `/api/webhooks/pagamentos/mercado-pago` (alias `/mercadopago` mantido).

Headers validados: `x-signature`, `x-request-id`.

## WhatsApp webhook (Evolution API)

A Evolution envia o campo `apikey` no corpo do JSON (mesma chave configurada na instancia). Nao ha suporte a header customizado.

```bash
Mensageria__WhatsApp__ApiKey=<apikey da instancia Evolution>
Mensageria__WhatsApp__InstanceName=<nome da instancia>
```

**Comportamento (staging/production):** requisicoes sem `apikey` valida no body retornam `401` (`WEBHOOK_WHATSAPP_NAO_AUTORIZADO`). Se o payload incluir `instance`, deve coincidir com `InstanceName`.

## Swagger (staging)

```bash
Swagger__AccessKey=<chave longa>
```

Acesso: header `X-Swagger-Key` nas rotas `/swagger`.

## Demais secrets obrigatórios (staging/production)

- `Auth__TokenSalt` (mín. 32 caracteres, único por ambiente)
- `MYSQL_CS`, `RESEND_APITOKEN`, `MercadoPago__AccessToken`, etc. (ver `HostedConfigurationValidator`)
