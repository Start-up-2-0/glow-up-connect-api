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
| `RateLimit__SensitiveMaxRequests` | `15` | Limite de falhas de login por IP (15 min) |
| `RateLimit__SensitiveWindowSeconds` | `900` | Janela das rotas sensíveis (15 min) |

**Comportamento:** tráfego via BFF (`X-Glow-Proxy-Secret`), mTLS ou token **válido** não conta no burst global. Tráfego anônimo direto na API recebe `429` (`RATE_LIMIT_BURST`). Login acumula falhas por IP (`LOGIN_IP_RATE_LIMITED`) além do lockout por conta.

**Exclusões:** `OPTIONS`, `GET /health`.

## BFF / proxy origin (SPA legítima)

| Variável | Uso |
|----------|-----|
| `GLOW_PROXY_SECRET` | Secret compartilhado App (Caddy) ↔ API; header `X-Glow-Proxy-Secret` |
| `ProxyOrigin__Enabled` | `true` automaticamente em staging/production quando o secret existe |

Rotas isentas: `GET /health`, `/api/webhooks/**`. Demais rotas de app exigem o header do Caddy.

## Request proof (anti-replay via BFF)

| Variável | Uso |
|----------|-----|
| `REQUEST_PROOF_SECRET` | Secret server-side (mín. 32 caracteres) para assinar proofs |
| `RequestProof__Enabled` | `true` em staging/production (auto quando o secret existe) |
| `RequestProof__TtlSeconds` | Validade do proof (padrão `60`) |
| `RequestProof__ClockSkewSeconds` | Tolerância de relógio (padrão `30`) |

**Comportamento:** tráfego via BFF deve enviar `X-Glow-Request-Proof` (uso único). O SPA obtém proofs em `GET /api/security/request-proof` (cookie `guc_rqctx` HttpOnly). Nonces ficam em memória na instância da API (sem Redis).

**Isenções:** `OPTIONS`, `GET /health`, `/api/webhooks/**`, `GET /api/security/request-proof`.

**Erros:** `403` `REQUEST_PROOF_AUSENTE`, `REQUEST_PROOF_INVALIDO`, `REQUEST_PROOF_EXPIRADO`, `REQUEST_PROOF_REPLAY`.

## CAPTCHA (login e cadastro)

| Variável | Uso |
|----------|-----|
| `Captcha__Enabled` | `true` em staging/production |
| `Captcha__SecretKey` | Secret server-side reCAPTCHA v2 |
| `VITE_CAPTCHA_SITE_KEY` | Site key reCAPTCHA v2 Checkbox no build do frontend |

Falha de validação: `400` `CAPTCHA_INVALIDO`.

## mTLS (Caddy → API, rede privada Railway)

| Variável (API) | Uso |
|----------------|-----|
| `MTLS_SERVER_CERT` / `MTLS_SERVER_KEY` | Certificado servidor (PEM multiline no Railway) |
| `MTLS_CA_CERT` | CA interna |
| `MTLS_CLIENT_CERT_THUMBPRINT` | Allowlist do certificado do Caddy |

| Variável (App) | Uso |
|----------------|-----|
| `MTLS_CLIENT_CERT` / `MTLS_CLIENT_KEY` | Client cert do Caddy |
| `MTLS_CA_CERT` | CA para validar a API |
| `API_INTERNAL_URL` | Host privado sem porta na URL; use `MTLS_UPSTREAM_PORT` |
| `MTLS_UPSTREAM_PORT` | Deve ser igual a `MTLS_MUTUAL_TLS_PORT` da API (padrao `8443`) |
| `MTLS_REQUIRED` | `true` no App e na API |

Gerar PKI: [`scripts/tls/generate-mtls-certs.sh`](../scripts/tls/generate-mtls-certs.sh).

Porta publica (`$PORT`): health e webhooks (HTTP). BFF → API via mTLS em `MTLS_MUTUAL_TLS_PORT` (padrao 8443). Logs de startup exibem as portas ativas.

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
