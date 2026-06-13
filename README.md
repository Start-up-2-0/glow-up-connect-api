# Glow Up Connect API

API backend do **Glow Up Connect** — marketplace de agendamentos — desenvolvida em **.NET 8** com **Clean Architecture**, **MySQL** e autenticação customizada via header `x-glow-token`.

## Stack

| Tecnologia | Uso |
|---|---|
| .NET 8 | Runtime e SDK |
| Entity Framework Core 8 | ORM e migrations |
| MySQL 8+ | Banco de dados (Pomelo EF Core) |
| BCrypt | Hash de senhas |
| Swagger (OpenAPI) | Documentação da API |
| xUnit + Moq + FluentAssertions | Testes unitários e de integração |

## Arquitetura

O projeto segue 4 camadas com dependência unidirecional:

```text
API → Application → Domain ← Infrastructure
```

| Camada | Responsabilidade |
|---|---|
| **Domain** | Entidades, enums e exceções de domínio |
| **Application** | Casos de uso, interfaces, options e serviços de negócio |
| **Infrastructure** | EF Core, repositórios, migrations e serviços técnicos |
| **API** | Controllers, middlewares, DTOs e pipeline HTTP |

## Estrutura de pastas

```text
glow-up-connect-api/
├── Dockerfile
├── railway.toml
├── docs/
│   └── railway-staging-setup.md
├── src/
│   ├── GLOWAPI.Domain/
│   │   ├── Entities/
│   │   ├── Enums/
│   │   └── Exceptions/
│   ├── GLOWAPI.Application/
│   │   ├── Interfaces/
│   │   ├── Models/
│   │   ├── Options/
│   │   └── Services/
│   ├── GLOWAPI.Infrastructure/
│   │   ├── Configurations/
│   │   ├── Migrations/
│   │   ├── Repositories/
│   │   └── Security/
│   └── GLOWAPI.API/
│       ├── Controllers/
│       ├── DTOs/
│       ├── Middlewares/
│       ├── Models/
│       └── Program.cs
├── tests/
│   └── GLOWAPI.Tests/
│       ├── Unit/
│       ├── Integration/
│       └── Helpers/
└── GLOWAPI.sln
```

## Autenticação (`x-glow-token`)

A API **não usa JWT**. O fluxo de autenticação é baseado em token assinado customizado (HMAC-SHA256) enviado no header configurável `x-glow-token`.

### Fluxo resumido

1. **Login** — `POST /api/auth/login` retorna `token` + `refreshToken`
2. **Rotas privadas** — enviar header `x-glow-token: <token>`
3. **Refresh** — `POST /api/auth/refresh` com `{ "refreshToken": "..." }` no body
4. **Logout** — `POST /api/auth/logout` com header `x-glow-token`

### Rotas públicas

| Método | Rota |
|---|---|
| GET | `/health` |
| POST | `/api/usuario` | Cadastro de cliente (e-mail + avatar opcional em base64) |
| POST | `/api/auth/confirmar-email` | Ativa conta com `token` (link) ou `codigo` (6 digitos) |
| POST | `/api/auth/reenviar-confirmacao` | Reenvia link e codigo |
| POST | `/api/auth/login` |
| POST | `/api/auth/refresh` |
| POST | `/api/auth/forgot-password` *(501)* |
| POST | `/api/auth/reset-password` *(501)* |

Demais rotas exigem token válido.

### Cadastro de cliente

Fluxo: cadastro → confirmar e-mail (link ou codigo) → login.

**Documentacao completa (endpoints, exemplos JSON, cURL e script de teste):** [docs/cadastro-usuario.md](docs/cadastro-usuario.md).

```http
POST /api/usuario
Content-Type: application/json

{
  "nome": "Maria Silva",
  "email": "maria@email.com",
  "telefone": "11999999999",
  "senha": "Senha123!",
  "avatarBase64": "data:image/jpeg;base64,/9j/4AAQ..."
}
```

- Sempre cria usuario com role `Cliente` (nao envie `role` no body).
- Conta inicia com `ativo: false` ate confirmar o e-mail.
- `avatarBase64`: opcional; aceita data URI (`data:image/jpeg;base64,...`) ou base64 puro com `avatarContentType`.
- O avatar e **validado** e persistido como data URI no MySQL (`Usuarios.AvatarBase64`, tipo `text`) — **sem pasta nem arquivo em disco**.
- Limite: 5 MB decodificado; tipos `image/jpeg`, `image/png`, `image/webp`.

```http
POST /api/auth/confirmar-email
Content-Type: application/json

{ "codigo": "482913" }
```

ou `{ "token": "<token-do-link>" }` (informe exatamente um dos dois).

```http
POST /api/auth/reenviar-confirmacao
Content-Type: application/json

{ "email": "maria@email.com" }
```

### Exemplo — login

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@email.com",
  "senha": "Senha123!"
}
```

```json
{
  "success": true,
  "message": "Login realizado com sucesso",
  "data": {
    "token": "eyJ1aWQi...",
    "refreshToken": "opaque-base64...",
    "expiresAt": "2026-05-24T01:00:00Z",
    "refreshExpiresAt": "2026-05-31T00:45:00Z",
    "usuario": {
      "id": 1,
      "nome": "Usuario Teste",
      "email": "user@email.com",
      "role": "Cliente",
      "avatarBase64": "data:image/png;base64,..."
    }
  }
}
```

Login com e-mail nao confirmado retorna `403` e codigo `EMAIL_NAO_CONFIRMADO`.

### Exemplo — rota protegida

```http
GET /api/usuario/me
x-glow-token: eyJ1aWQi...
```

### Pipeline HTTP

```text
ExceptionMiddleware → HTTPS → GlowTokenAuthenticationMiddleware → PermissionMiddleware → Controllers
```

## Configuração

### Desenvolvimento

Crie `src/GLOWAPI.API/appsettings.Development.json` localmente (arquivo ignorado pelo Git):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=glowapi_db;User=root;Password=;"
  },
  "Database": {
    "Provider": "MySQL"
  },
  "Auth": {
    "MaxLoginAttempts": 5,
    "LockoutMinutes": 15,
    "SessionMinutes": 15,
    "RefreshTokenDays": 7,
    "SlidingRenewalMinutes": 30,
    "ValidateIpOnToken": false,
    "ValidateUserAgentOnToken": false,
    "TokenSalt": "glow-dev-token-salt-min-32-chars!!",
    "TokenHeaderName": "x-glow-token"
  }
}
```

> **Importante:** `TokenSalt` deve ter no mínimo 32 caracteres. Em produção, use variável de ambiente ou secrets manager — nunca commite credenciais reais.

Alternativa: [User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets):

```bash
dotnet user-secrets init --project src/GLOWAPI.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=..." --project src/GLOWAPI.API
dotnet user-secrets set "Auth:TokenSalt" "seu-salt-com-minimo-32-chars" --project src/GLOWAPI.API
```

### Staging e produção (Railway)

Quando `ASPNETCORE_ENVIRONMENT` é `Staging` ou `Production`, a connection string vem **somente** da variável `MYSQL_CS` (valores distintos por ambiente no Railway):

```bash
MYSQL_CS="Server=<host>;Port=<port>;Database=<database>;User=<user>;Password=<password>;SslMode=Required;"
```

`ConnectionStrings__DefaultConnection` é ignorada nesses ambientes para evitar fallback acidental.

| Ambiente | `ASPNETCORE_ENVIRONMENT` | Branch Git (deploy) | Swagger |
|----------|--------------------------|---------------------|---------|
| Local | `Development` | — | Sim |
| Homologação | `Staging` | `staging` | Sim |
| Produção | `Production` | `main` | Não |

Variáveis obrigatórias no Railway (API):

| Variável | Staging | Produção |
|----------|---------|----------|
| `MYSQL_CS` | MySQL do environment staging | MySQL do environment production |
| `Auth__TokenSalt` | Salt próprio (≥ 32 chars) | Salt próprio (diferente) |
| `Auth__FrontendBaseUrl` | URL do front staging | URL do front produção |
| `RESEND_APITOKEN` | API key Resend | API key Resend (produção) |
| `Mensageria__Email__From` | Remetente verificado no Resend | Remetente produção |
| `Mensageria__Email__Habilitado` | `true` | `true` |
| `ASPNETCORE_URLS` | `http://0.0.0.0:$PORT` (opcional) | idem |

Chaves aninhadas no Railway usam `__` (ex.: `Mensageria__Email__From` → `Mensageria:Email:From`). E-mails transitam pela fila assíncrona e são enviados via Resend — ver [docs/mensageria.md](docs/mensageria.md) e [context/Mensageria-assincrona.md](context/Mensageria-assincrona.md).

Guia completo do painel Railway: [docs/railway-staging-setup.md](docs/railway-staging-setup.md).

### Deploy com Docker (Railway)

O repositório inclui [`Dockerfile`](Dockerfile), [`.dockerignore`](.dockerignore) e [`railway.toml`](railway.toml) (health check em `/health`).

```bash
docker build -t glowapi-api .
docker run -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Staging \
  -e MYSQL_CS="Server=..." \
  -e Auth__TokenSalt="seu-salt-staging-com-minimo-32-chars" \
  glowapi-api
```

Migrations no ambiente Railway:

```bash
.\scripts\railway-migrate.ps1 -Environment staging
```

CI (GitHub Actions): workflows separados — [`ci-staging.yml`](.github/workflows/ci-staging.yml) (branch `staging`) e [`ci-main.yml`](.github/workflows/ci-main.yml) (branch `main`). Deploy continua via Railway por branch.

## Como executar

### Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- MySQL 8+ acessível
- EF Core CLI:

```bash
dotnet tool install --global dotnet-ef
```

### 1. Restaurar e compilar

```bash
dotnet restore
dotnet build
```

### 2. Aplicar migrations

```bash
dotnet ef database update \
  --project src/GLOWAPI.Infrastructure \
  --startup-project src/GLOWAPI.API
```

> Se o banco já existir com migrations antigas, pode ser necessário fazer baseline de `InitialCreate` no `__EFMigrationsHistory` antes de rodar as migrations pendentes.

### 3. Subir a API

```bash
dotnet run --project src/GLOWAPI.API
```

Swagger disponível em desenvolvimento: `https://localhost:<porta>/swagger`

No Swagger, use o scheme **GlowToken** (header `x-glow-token`) para testar rotas protegidas.

## Testes

```bash
dotnet test
```

| Tipo | Local |
|---|---|
| Unitários (domínio, serviços, middleware) | `tests/GLOWAPI.Tests/Unit/` |
| Integração (auth, WebApplicationFactory) | `tests/GLOWAPI.Tests/Integration/` |

## Endpoints principais

| Controller | Prefixo | Descrição |
|---|---|---|
| `AuthController` | `/api/auth` | Login, confirmar e-mail, reenviar confirmacao, refresh, logout |
| `UsuarioController` | `/api/usuario` | Cadastro cliente (`POST`), perfil (`GET/PUT/DELETE /me`) |
| `HealthController` | `/health` | Health check |

## Migrations recentes

| Migration | Descrição |
|---|---|
| `AddUsuarioConfirmacaoEmailEAvatar` | `AvatarBase64` (text), confirmacao de e-mail no usuario |
| `CreateMensagensNotificacao` | Fila de notificacoes (e-mail de confirmacao) |
| `CreateSessoesAutenticacao` | Tabela de sessões persistidas |
| `RefactorSessaoAutenticacaoGlowToken` | `AccessTokenHash`, `BloqueadoAte`, `LogsAutenticacao` |

## Licença

Projeto privado — Glow Up Connect.
