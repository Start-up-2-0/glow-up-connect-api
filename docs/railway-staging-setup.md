# Railway — ambiente Staging (branch `staging`)

Guia para configurar homologacao no **mesmo projeto Railway** da producao, com isolamento por environment.

## 1. Criar environment Staging

1. Abra o projeto no [Railway](https://railway.app).
2. **Settings → Environments → New Environment** → nome `staging`.
3. Alterne para o environment `staging` (seletor no topo).

## 2. PostgreSQL dedicado (staging)

1. No environment `staging`, clique **+ New → Database → PostgreSQL**.
2. Aguarde provisionamento.
3. No servico Postgres, copie as variaveis (`PGHOST`, `PGPORT`, `PGDATABASE`, `PGUSER`, `PGPASSWORD`) ou use **Connect → Variables**.

> Producao deve manter seu proprio Postgres no environment `production`. Nunca compartilhe `POSTGSL` entre ambientes.

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
| `POSTGSL` | Ver abaixo |
| `Auth__TokenSalt` | Salt unico staging (minimo 32 caracteres) |
| `Auth__TokenHeaderName` | `x-glow-token` |
| `Auth__FrontendBaseUrl` | URL do front staging (links nos e-mails) |
| `RESEND_APITOKEN` | API key em [resend.com/api-keys](https://resend.com/api-keys) |
| `Mensageria__Email__From` | Ex.: `Glow Up Connect <noreply@dominio-verificado.com>` |
| `Mensageria__Email__Habilitado` | `true` |
| `MercadoPago__UsarCheckoutPro` | `true` |
| `MercadoPago__AccessToken` | Token `TEST-...` (sandbox) ou producao |
| `MercadoPago__PayerEmailOverride` | (opcional sandbox) ex.: `test_user_123` |

Com `MercadoPago__UsarCheckoutPro=true`, as URLs de retorno do Checkout Pro sao derivadas de `Auth__FrontendBaseUrl` (`/assinatura/sucesso`, `/pendente`, `/falha`) e o webhook usa `RAILWAY_PUBLIC_DOMAIN` ou `MercadoPago__PublicBaseUrl`.

> O remetente (`Mensageria__Email__From`) deve usar um dominio verificado em [resend.com/domains](https://resend.com/domains). Nao commite token nem `From` no `appsettings.json` do repositorio.

### Montar `POSTGSL`

No servico API (staging), adicione variavel `POSTGSL`:

```text
Host=${{Postgres.PGHOST}};Port=${{Postgres.PGPORT}};Database=${{Postgres.PGDATABASE}};Username=${{Postgres.PGUSER}};Password=${{Postgres.PGPASSWORD}};SSL Mode=Require;Trust Server Certificate=true
```

Substitua `Postgres` pelo nome do servico PostgreSQL no Railway, se diferente.

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

O container de runtime nao inclui `dotnet-ef`. Aplique migrations assim:

### Primeiro deploy (recomendado)

```bash
npm i -g @railway/cli
railway login
railway link
railway environment staging
railway run --service <nome-do-servico-api> dotnet ef database update --project src/GLOWAPI.Infrastructure --startup-project src/GLOWAPI.API
```

Requer [.NET 8 SDK](https://dotnet.microsoft.com/download) e `dotnet tool install --global dotnet-ef` na maquina local; o comando roda no contexto Railway com acesso ao Postgres de staging.

### Script local (PowerShell)

```powershell
.\scripts\railway-migrate.ps1 -Environment staging
```

## 8. Dominio publico

1. Servico API (staging) → **Settings → Networking → Generate Domain**.
2. Teste:

```bash
curl https://<seu-dominio-staging>.up.railway.app/health
```

Resposta esperada: `{"status":"healthy"}`.

Swagger em staging: `https://<dominio>/swagger`.

## 9. Checklist pos-deploy

- [ ] `GET /health` → 200
- [ ] Logs sem erro de `POSTGSL`, `TokenSalt`, `RESEND_APITOKEN` ou `Mensageria__Email__From`
- [ ] Migrations em `__EFMigrationsHistory`
- [ ] Push em `staging` nao redeploya producao
- [ ] `Auth__TokenSalt` staging diferente de producao
- [ ] Token de staging invalido em producao

## 10. Producao (referencia)

No environment `production`:

| Variavel | Valor |
|----------|--------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `POSTGSL` | Connection string do Postgres de **producao** |
| `Auth__TokenSalt` | Salt proprio de producao |
| `Auth__FrontendBaseUrl` | URL do front de producao |
| `RESEND_APITOKEN` | API key Resend de producao |
| `Mensageria__Email__From` | Remetente com dominio verificado (producao) |
| `Mensageria__Email__Habilitado` | `true` |

Branch tipica: `main`.
