# Railway — ambiente Staging (branch `staging`)

Guia para configurar homologacao no **mesmo projeto Railway** da producao, com isolamento por environment.

## 1. Criar environment Staging

1. Abra o projeto no [Railway](https://railway.app).
2. **Settings → Environments → New Environment** → nome `staging`.
3. Alterne para o environment `staging` (seletor no topo).

## 2. MySQL dedicado (staging)

1. No environment `staging`, clique **+ New → Database → MySQL**.
2. Aguarde provisionamento.
3. No servico MySQL, copie as variaveis (`MYSQLHOST`, `MYSQLPORT`, `MYSQLDATABASE`, `MYSQLUSER`, `MYSQLPASSWORD`) ou use **Connect → Variables**.

> Producao deve manter seu proprio MySQL no environment `production`. Nunca compartilhe `MYSQL_CS` entre ambientes.

## 3. Servico da API (staging)

1. **+ New → GitHub Repo** (ou duplique o servico existente para staging).
2. Selecione este repositorio.
3. **Settings → Source → Branch** → `staging`.
4. **Settings → Build** → Builder: **Dockerfile** (detectado via [`railway.toml`](../railway.toml)).
5. Confirme **Root Directory** = `/` e **Dockerfile** = `Dockerfile`.

## 4. Variaveis de ambiente (servico API, environment staging)

| Variavel | Valor |
|----------|--------|
| `ASPNETCORE_ENVIRONMENT` | `Staging` |
| `ASPNETCORE_URLS` | `http://0.0.0.0:$PORT` (opcional; entrypoint ja ajusta `PORT`) |
| `MYSQL_CS` | Ver abaixo |
| `Auth__TokenSalt` | Salt unico staging (minimo 32 caracteres) |
| `Auth__TokenHeaderName` | `x-glow-token` |
| `Auth__FrontendBaseUrl` | URL do front staging (links nos e-mails) |
| `RESEND_APITOKEN` | API key em [resend.com/api-keys](https://resend.com/api-keys) |
| `Mensageria__Email__From` | Ex.: `Glow Up Connect <noreply@dominio-verificado.com>` |
| `Mensageria__Email__Habilitado` | `true` |
| `MercadoPago__UsarCheckoutPro` | `true` |
| `MercadoPago__AccessToken` | Token `TEST-...` (sandbox) ou producao |
| `MercadoPago__PayerEmailOverride` | (opcional sandbox) ex.: `test_user_123` |
| `MercadoPago__WebhookSecret` | Secret do painel Mercado Pago (webhooks) |
| `Cors__AllowedOrigins__0` | URL do frontend staging |
| `Swagger__AccessKey` | Chave para acessar `/swagger` (header `X-Swagger-Key`) |
| `GLOW_PROXY_SECRET` | Secret compartilhado com o servico do app (header `X-Glow-Proxy-Secret`) |
| `Captcha__Enabled` | `true` |
| `Captcha__SecretKey` | Secret reCAPTCHA v3 (server-side) |
| `MTLS_SERVER_CERT` / `MTLS_SERVER_KEY` / `MTLS_CA_CERT` | (fase mTLS) PEMs multiline — ver `scripts/tls/generate-mtls-certs.sh` |
| `MTLS_CLIENT_CERT_THUMBPRINT` | (fase mTLS) thumbprint SHA1 do cert do Caddy |

Ver tambem [docs/security/fase1-hardening.md](./security/fase1-hardening.md). as URLs de retorno do Checkout Pro sao derivadas de `Auth__FrontendBaseUrl` (`/assinatura/sucesso`, `/pendente`, `/falha`) e o webhook usa `RAILWAY_PUBLIC_DOMAIN` ou `MercadoPago__PublicBaseUrl`.

> O remetente (`Mensageria__Email__From`) deve usar um dominio verificado em [resend.com/domains](https://resend.com/domains). Nao commite token nem `From` no `appsettings.json` do repositorio.

### Montar `MYSQL_CS`

No servico API (staging), adicione variavel `MYSQL_CS`:

```text
Server=${{MySQL.MYSQLHOST}};Port=${{MySQL.MYSQLPORT}};Database=${{MySQL.MYSQLDATABASE}};User=${{MySQL.MYSQLUSER}};Password=${{MySQL.MYSQLPASSWORD}};SslMode=Required;
```

Substitua `MySQL` pelo nome do servico MySQL no Railway, se diferente.

## 5. Deploy automatico

- Environment **staging** + branch **staging** → cada push em `staging` dispara deploy.
- Environment **production** + branch **main** (ou sua branch de prod) → deploy de producao isolado.

Confirme em **Settings → Deploy** que **Auto Deploy** esta ativo.

## 6. Health check

O [`railway.toml`](../railway.toml) define:

- **Path:** `/health`
- **Timeout:** 60s (cold start .NET)

No painel, confira em **Settings → Deploy → Healthcheck** se o path `/health` esta aplicado.

## 7. Migrations

Em **Staging** e **Production**, a API aplica migrations pendentes automaticamente no startup (`Database:ApplyMigrationsOnStartup`, padrao `true`).

Ao fazer deploy de uma versao com nova migration, basta subir o servico: na inicializacao ela verifica `__EFMigrationsHistory` e executa o que faltar antes de aceitar trafego.

Para desativar (nao recomendado em Railway):

```text
Database__ApplyMigrationsOnStartup=false
```

### Aplicacao manual (opcional)

Se precisar rodar fora do deploy:

```bash
npm i -g @railway/cli
railway login
railway link
railway environment staging
railway run --service <nome-do-servico-api> dotnet ef database update --project src/GLOWAPI.Infrastructure --startup-project src/GLOWAPI.API
```

Ou via PowerShell local com `MYSQL_CS`:

```powershell
$env:MYSQL_CS = "Server=...;Port=...;Database=...;User=...;Password=...;SslMode=Required;"
.\scripts\railway-migrate.ps1 -Environment staging
```

## 8. Servico do App (staging) — BFF same-origin

| Variavel | Valor |
|----------|--------|
| `VITE_API_BASE_URL` | `/api` |
| `VITE_CAPTCHA_SITE_KEY` | Site key reCAPTCHA v3 |
| `GLOW_PROXY_SECRET` | Mesmo valor da API |
| `API_INTERNAL_HOST` | Host privado da API (`<servico>.railway.internal`) |
| `API_INTERNAL_PORT` | `8080` (HTTP) ou omitir quando usar mTLS |
| `API_INTERNAL_URL` | (mTLS) `https://<servico-api>.railway.internal:8443` |
| `MTLS_CLIENT_CERT` / `MTLS_CLIENT_KEY` / `MTLS_CA_CERT` | (mTLS) PEMs do cliente Caddy |

O Caddy faz proxy `/api/*` → API e injeta `X-Glow-Proxy-Secret`. Webhooks externos continuam na URL publica da API.

### Rollout recomendado (staging)

1. Deploy BFF + `GLOW_PROXY_SECRET` + `VITE_API_BASE_URL=/api`
2. Ativar CAPTCHA (`Captcha__*` + `VITE_CAPTCHA_SITE_KEY`)
3. Gerar PKI (`scripts/tls/generate-mtls-certs.sh`) e configurar mTLS na rede interna
4. Limpar bloqueios antigos em `IpRateLimitBlocks` se necessario

## 9. Dominio publico

1. Servico API (staging) → **Settings → Networking → Generate Domain**.
2. Teste:

```bash
curl https://<seu-dominio-staging>.up.railway.app/health
```

Resposta esperada: `{"status":"healthy"}`.

Swagger em staging: `https://<dominio>/swagger`.

## 10. Checklist pos-deploy

- [ ] `GET /health` → 200
- [ ] Logs sem erro de `MYSQL_CS`, `TokenSalt`, `RESEND_APITOKEN` ou `Mensageria__Email__From`
- [ ] Migrations em `__EFMigrationsHistory`
- [ ] Push em `staging` nao redeploya producao
- [ ] `Auth__TokenSalt` staging diferente de producao
- [ ] Token de staging invalido em producao

## 11. Producao (referencia)

No environment `production`:

| Variavel | Valor |
|----------|--------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `MYSQL_CS` | Connection string do MySQL de **producao** |
| `Auth__TokenSalt` | Salt proprio de producao |
| `Auth__FrontendBaseUrl` | URL do front de producao |
| `RESEND_APITOKEN` | API key Resend de producao |
| `Mensageria__Email__From` | Remetente com dominio verificado (producao) |
| `Mensageria__Email__Habilitado` | `true` |

Branch tipica: `main`.
