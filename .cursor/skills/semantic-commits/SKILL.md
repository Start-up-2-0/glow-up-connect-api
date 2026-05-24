---
name: semantic-commits
description: >-
  Planeja e cria commits semanticos pequenos para o glow-up-connect-api, em portugues,
  agrupados por camada e responsabilidade (domain, auth, api, database, testes).
  Use quando o usuario pedir commits, separar alteracoes, mensagens de commit,
  historico organizado, ou executar o script scripts/commit-auth-feature.
---

# Commits semanticos — Glow Up Connect API

## Quando usar

- Usuario pede commits, separar PR, organizar historico ou "commitar por partes"
- Apos implementar feature grande (auth, CRUD, migrations, testes)
- Antes de push/PR — garantir commits atomicos

## Regra rapida

Ler `.cursor/rules/commit-conventions.mdc` e seguir **a risca**.

Formato: `<tipo>(<escopo>): <descricao em portugues>`

## Workflow do agente

### 1. Inventariar mudancas

```bash
git status --short
git diff --name-only
```

Ignorar sempre: `bin/`, `obj/`, `appsettings.Development.json`, credenciais.

### 2. Agrupar por responsabilidade unica

| Grupo | Arquivos tipicos |
|-------|------------------|
| Domain | `GLOWAPI.Domain/Entities/`, `Exceptions/` |
| Application | `Interfaces/`, `Services/`, `Options/`, `Models/` |
| Infrastructure | `Repositories/`, `Configurations/`, `Security/`, `Migrations/`, `DependencyInjection.cs` |
| API | `Controllers/`, `DTOs/`, `Middlewares/`, `Program.cs`, `appsettings.json` |
| Testes | `tests/GLOWAPI.Tests/` |

Uma migration = um commit `chore(database): ...`

### 3. Propor sequencia ao usuario

Listar commits planejados **antes** de executar, com mensagem + arquivos de cada um.

Ordem: domain → application → infrastructure → migrations → api → testes

### 4. Criar commits

**Preferencia:** usuario executa commits no terminal proprio (evita co-autoria do Cursor).

Se o usuario pedir que o agente commite:
- Usar `git commit -m "mensagem"` — **sem** trailers `Co-authored-by`
- Um `git add` + `git commit` por grupo
- Nunca `git commit --amend` salvo pedido explicito

Script existente para batch (auth feature):

```cmd
scripts\commit-auth-feature.cmd -DryRun
scripts\commit-auth-feature.cmd
```

> Rodar o `.cmd`, nunca duplo clique no `.ps1` (abre Notepad no Windows).

### 5. Validar

```bash
git log --oneline -n 15
dotnet test
```

## Matriz tipo × escopo

| Alteracao | Tipo | Escopo exemplo |
|-----------|------|----------------|
| Nova entidade | `feat` | `domain` |
| Bloqueio temporario em Usuario | `feat` | `usuario` |
| GlowTokenService | `feat` | `auth` |
| Repositorio novo | `feat` | `infrastructure` |
| Migration EF | `chore` | `database` |
| AuthController | `feat` | `auth` |
| ExceptionMiddleware | `feat` | `api` |
| Remover JWT | `refactor` | `auth` |
| Testes unitarios auth | `test` | `auth` |
| README | `docs` | — ou omitir escopo |

## Anti-patterns

```
# Ruim
fix: ajustes
feat: varias coisas
chore: update

# Bom
feat(auth): implementar servico de assinatura glow token
test(auth): adicionar testes de integracao do controller de auth
```

## Referencia detalhada

- Convencoes completas: [reference.md](reference.md)
- Script batch auth: `scripts/commit-auth-feature.ps1`
