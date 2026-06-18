# Fase 1 — Hardening de segurança

Configurações introduzidas no hardening inicial da GLOWAPI.

## Rate limit global (burst)

| Variável | Padrão | Descrição |
|----------|--------|-----------|
| `RateLimit__Enabled` | `true` | Liga/desliga o middleware |
| `RateLimit__BurstMaxRequests` | `80` | Requisições anônimas permitidas na janela geral |
| `RateLimit__BurstWindowSeconds` | `60` | Janela deslizante em segundos |
| `RateLimit__BurstPenaltySeconds` | `30` | Retry-After em throttling suave |
| `RateLimit__HardBlockMultiplier` | `5` | Bloqueio 24h só após `BurstMaxRequests × multiplicador` |
| `RateLimit__BlockDurationHours` | `24` | Duração do bloqueio duro por IP |
| `RateLimit__ExemptAuthenticatedRequests` | `true` | Requisições com `x-glow-token` não contam no burst global |
| `RateLimit__SensitiveMaxRequests` | `15` | Limite em login/cadastro/recuperação de senha |
| `RateLimit__SensitiveWindowSeconds` | `900` | Janela das rotas sensíveis (15 min) |

**Comportamento:** SPA autenticada não é penalizada no burst global. Tráfego anônimo acima do limite recebe `429` com código `RATE_LIMIT_BURST` e `Retry-After` (sem bloqueio de 24h imediato). Bloqueio duro (`IP_BLOCKED_24H`) ocorre apenas em abuso extremo ou repetição massiva em rotas sensíveis.

**Exclusões:** `OPTIONS`, `GET /health`.

**Suporte:** se um cliente legítimo for bloqueado com `IP_BLOCKED_24H`, remover o registro em `IpRateLimitBlocks` ou aguardar `BlockedUntil`.

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
