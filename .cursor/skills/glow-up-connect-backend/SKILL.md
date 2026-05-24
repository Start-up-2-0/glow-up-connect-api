---
name: glow-up-connect-backend
description: >-
  Desenvolve a API GLOWAPI (.NET 8, Clean Architecture) do marketplace Glow Up Connect
  (agendamentos, estabelecimentos, profissionais, pagamentos e assinaturas).
  Use ao criar entidades, migrations, repositórios, services, controllers ou endpoints
  neste repositório glow-up-connect-api.
---

# Glow Up Connect — Backend (GLOWAPI)

## Visão do produto

Marketplace de agendamentos para profissionais autônomos e estabelecimentos (barbearias, salões, estética). Conecta clientes, profissionais e lojas com gestão de agenda, pagamentos via gateway (Mercado Pago, AbacatePay) e caixa interno.

**Monetização:** plano gratuito (taxa por agendamento + saque mínimo) vs assinatura (sem taxa por agendamento).

## Stack e arquitetura

- .NET 8, EF Core, PostgreSQL, Swagger, JWT (configurado, auth ainda parcial nos controllers)
- Clean Architecture em 4 camadas:

| Camada | Projeto | Responsabilidade |
|--------|---------|-------------------|
| Domain | `GLOWAPI.Domain` | Entidades, enums |
| Application | `GLOWAPI.Application` | Interfaces de repositório/serviço, lógica de negócio |
| Infrastructure | `GLOWAPI.Infrastructure` | DbContext, Fluent API, repositórios, DI |
| API | `GLOWAPI.API` | Controllers, DTOs Request/Response, middlewares |

**Dependências:** Domain ← Application ← Infrastructure ← API. Domain não referencia outras camadas.

## Estado atual do código

- 20 entidades + enums implementados; migration `InitialCreate` aplicável
- Repositórios: todos registrados em `Infrastructure/DependencyInjection.cs`
- Services: apenas `UsuarioService`
- Controllers: apenas `UsuarioController`
- Próximo trabalho típico: services/controllers para demais domínios

## Convenções do projeto

### Nomenclatura

- Entidades, propriedades e métodos em **português** (`CriarUsuarioAsync`, `Estabelecimento`)
- Enums em inglês no código (`UserRole`, `AgendamentoStatus`) — manter padrão existente
- Tabelas no plural em Fluent API (`Usuarios`, `Estabelecimentos`)

### Datas

- Maioria das entidades: `CreateAd` + `UpdatedAt`
- Exceção: `Usuario` usa `CreatedAt` — **não renomear sem migration explícita**
- Sempre usar `DateTime.UtcNow` para timestamps

### PublicGuid

- `Estabelecimento` e `Profissional` possuem `PublicGuid` (Guid único, gerado na criação, imutável)
- Rotas públicas previstas: `/agendar/loja/{publicGuid}`, `/agendar/profissional/{publicGuid}`
- Nunca expor `Id` interno em links públicos

### Campos de auditoria padrão

`Ativo` (default true), timestamps, soft delete via `Ativo = false` (padrão em `Usuario`)

## Fluxo para nova feature (camada completa)

```txt
1. Domain: entidade/enums (se ainda não existir)
2. Infrastructure: *Configuration.cs (Fluent API)
3. Application: I*Repository (se necessário) + I*Service + *Service
4. Infrastructure: *Repository + registro no DependencyInjection
5. Application: registro no DependencyInjection
6. API: DTOs em GLOWAPI.API/DTOs/{Entidade}/ + Controller
7. dotnet build GLOWAPI.sln
8. Migration (se alterou modelo)
```

## Fluxo para alterar entidade existente

```txt
1. Alterar entidade em Domain/Entities
2. Atualizar Configuration correspondente
3. dotnet build GLOWAPI.sln
4. dotnet ef migrations add DescricaoDaAlteracao ...
5. dotnet ef database update ...
6. Validar tabela/índices/FKs
```

## Comandos EF Core (PowerShell)

Build:

```powershell
dotnet build GLOWAPI.sln
```

Migration:

```powershell
dotnet ef migrations add NomeDaMigration `
  --project src/GLOWAPI.Infrastructure `
  --startup-project src/GLOWAPI.API `
  --output-dir Migrations
```

Apply:

```powershell
dotnet ef database update `
  --project src/GLOWAPI.Infrastructure `
  --startup-project src/GLOWAPI.API
```

## Padrão de código existente

### Controller (API)

- `[ApiController]` + `[Route("api/[controller]")]`
- Injeta `I*Service`, retorna `IActionResult`
- DTOs de request/response ficam em `GLOWAPI.API/DTOs/`, não na Application
- `CreatedAtAction` no POST, `NoContent` no PUT/DELETE

### Service (Application)

- Interface em `Application/Interfaces/Services/`
- Implementação em `Application/Services/`
- Injeta repositórios via interface
- Validações de negócio lançam `InvalidOperationException` ou `KeyNotFoundException`
- Senhas: BCrypt (`BCrypt.Net.BCrypt`)

### Repository (Infrastructure)

- Interface em `Application/Interfaces/Repositories/`
- Implementação em `Infrastructure/Repositories/`
- Métodos async com `CancellationToken`
- `SalvarAlteracoesAsync` após mutações

### Fluent API (Infrastructure/Configurations)

- `ToTable`, `HasKey`, `IsRequired`, `HasMaxLength`, índices únicos
- Enums: `HasConversion(role => role.ToString(), ...)`
- Relacionamentos e FKs explícitos
- Configurações descobertas via `ApplyConfigurationsFromAssembly`

## Regras de negócio críticas

Resumo — detalhes em [reference.md](reference.md):

1. **Um usuário = uma role global** (`Cliente`, `DonoEstabelecimento`, `ProfissionalAutonomo`, `ProfissionalEstabelecimento`, `Admin`)
2. **Profissional autônomo** opera sozinho (endereço, caixa, assinatura, serviços)
3. **Profissional vinculado** vê só agendamentos atribuídos a ele
4. **Agendamento** pode ter múltiplos `AgendamentoItem` (serviços/profissionais diferentes)
5. **Financeiro:** `Pagamento` (gateway) ≠ `Caixa` (saldo interno) ≠ `LancamentoCaixa` (movimentação)
6. **Webhooks** idempotentes — registrar antes de processar, evitar lançamentos duplicados
7. **Planos:** CRUD admin fica fora desta API; aqui só `GET /planos` e `POST /assinaturas/trocar-plano`

## Banco de dados

- Dev: `ConnectionStrings:DefaultConnection` em `appsettings.Development.json`
- Prod: **somente** variável `POSTGSL` (ignora outras connection strings)
- Provider: PostgreSQL obrigatório

## Checklist antes de PR

- [ ] Build passa
- [ ] Sem referências cruzadas indevidas entre camadas
- [ ] Configuration Fluent API atualizada
- [ ] DI registrado (Application + Infrastructure)
- [ ] Migration gerada se modelo mudou
- [ ] Regras de domínio respeitadas (roles, caixa, webhooks)
- [ ] Escopo mínimo — sem refatorações não solicitadas

## Documentação de referência no repo

- `context/Projeto.md` — visão geral do produto
- `context/Modelagem-v1.md` — entidades e regras detalhadas
- `context/Guia-Entidades-e-Migrations.md` — ciclo de implementação
- Domínio completo: [reference.md](reference.md)
