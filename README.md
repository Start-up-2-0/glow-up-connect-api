# Glow Up Connect API

API backend do **Glow Up Connect** — marketplace de agendamentos — desenvolvida em **.NET 8** com **Clean Architecture**, **PostgreSQL** e autenticação customizada via header `x-glow-token`.

## Stack

| Tecnologia | Uso |
|---|---|
| .NET 8 | Runtime e SDK |
| Entity Framework Core 8 | ORM e migrations |
| PostgreSQL | Banco de dados |
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
| POST | `/api/auth/login` |
| POST | `/api/auth/refresh` |
| POST | `/api/auth/forgot-password` *(501)* |
| POST | `/api/auth/reset-password` *(501)* |

Demais rotas exigem token válido.

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
      "role": "Cliente"
    }
  }
}
```

### Exemplo — rota protegida

```http
GET /api/Usuario/1
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
    "DefaultConnection": "Host=localhost;Port=5432;Database=glowapi_db;Username=postgres;Password=postgres"
  },
  "Database": {
    "Provider": "PostgreSQL"
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

### Produção

Quando `ASPNETCORE_ENVIRONMENT=Production`, a connection string vem da variável:

```bash
POSTGSL="Host=<host>;Port=5432;Database=<database>;Username=<user>;Password=<password>;SSL Mode=Require;Trust Server Certificate=true"
```

`ConnectionStrings__DefaultConnection` e outras variáveis de conexão são ignoradas em produção para evitar fallback acidental.

## Como executar

### Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- PostgreSQL acessível
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
| `AuthController` | `/api/auth` | Login, logout, refresh |
| `UsuarioController` | `/api/Usuario` | CRUD de usuários |
| `HealthController` | `/health` | Health check |

## Migrations recentes (auth)

| Migration | Descrição |
|---|---|
| `CreateSessoesAutenticacao` | Tabela de sessões persistidas |
| `RefactorSessaoAutenticacaoGlowToken` | `AccessTokenHash`, `BloqueadoAte`, `LogsAutenticacao` |

## Licença

Projeto privado — Glow Up Connect.
