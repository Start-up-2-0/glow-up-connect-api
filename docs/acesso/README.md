# Controle de acesso — GLOWAPI

Referencia de **quem acessa o que** na API, com base no codigo em producao (`PermissionMiddleware`, `MatrizPermissaoNegocioService`, `PlanoComercialCatalogo`).

## Como a API decide o acesso

Tres camadas independentes:

```text
1. Autenticacao (x-glow-token)
   -> Quase tudo exige token, exceto rotas [AllowAnonymous]

2. Modulo de assinatura (RequerModuloAssinatura)
   -> Estabelecimento precisa de assinatura ATIVA com o modulo no plano

3. Permissao de negocio (RequerPermissaoNegocio)
   -> Usuario precisa de vinculo ativo em EstabelecimentoUsuario + role com a permissao
```

Para montar menu ou guardas no frontend (apenas operadores de negocio — **nao** Cliente):

```http
GET /api/usuario/me/estabelecimentos
```

Retorna, por negocio: `Permissoes`, `Modulos`, `AssinaturaAtiva`, `PlanoNome`, `Role`.

Usuarios com role global `Cliente` recebem **403** neste endpoint.

## Documentos por tipo de acesso

| Documento | Descricao |
|-----------|-----------|
| [publico.md](./publico.md) | Visitante sem login |
| [cliente.md](./cliente.md) | Usuario com role global `Cliente` |
| [dono-estabelecimento.md](./dono-estabelecimento.md) | Role `DonoEstabelecimento` + onboarding de loja |
| [profissional-autonomo.md](./profissional-autonomo.md) | Role `ProfissionalAutonomo` + ownership do perfil |
| [modulos.md](./modulos.md) | Catalogo completo de modulos e endpoints protegidos |

### Planos comerciais

| Plano | Modulos liberados |
|-------|-------------------|
| [plano-basic.md](./planos/plano-basic.md) | Agenda, Servicos, Horarios, Notificacoes, Email |
| [plano-plus.md](./planos/plano-plus.md) | Basic + Profissionais + WhatsApp |
| [plano-premium.md](./planos/plano-premium.md) | Plus + Caixa + Financeiro + ComissaoProfissionais |

### Roles dentro do estabelecimento

Aplicam-se sobre o plano contratado. Ver [modulos.md](./modulos.md) para cruzamento modulo + permissao.

| Role | Documento |
|------|-----------|
| Owner / Admin | [estabelecimento/owner-admin.md](./estabelecimento/owner-admin.md) |
| Manager | [estabelecimento/manager.md](./estabelecimento/manager.md) |
| Receptionist | [estabelecimento/receptionist.md](./estabelecimento/receptionist.md) |
| Profissional | [estabelecimento/profissional.md](./estabelecimento/profissional.md) |

## Roles globais (`UserRole`)

| Role | Papel |
|------|-------|
| `Cliente` | Consumidor — ver [cliente.md](./cliente.md) |
| `DonoEstabelecimento` | Contrata assinatura de estabelecimento; permissoes operacionais via role no negocio |
| `ProfissionalAutonomo` | Contrata assinatura propria — ver [profissional-autonomo.md](./profissional-autonomo.md) |
| `ProfissionalEstabelecimento` | Atua dentro de loja — ver [profissional-estabelecimento.md](./profissional-estabelecimento.md) |
| `Admin` | Administracao interna da plataforma (sem middleware dedicado hoje) |

A role global **nao substitui** permissoes de negocio. Um `DonoEstabelecimento` so opera endpoints de gestao se estiver vinculado ao estabelecimento (tipicamente como `Owner`).

## Regra pratica para o frontend

```text
Pode exibir funcionalidade X?
  = usuario autenticado
  AND role global != Cliente (para contexto de negocio)
  AND (se rota de negocio) vinculo ativo no estabelecimento
  AND permissao X em Permissoes[]
  AND modulo necessario em Modulos[]
  AND AssinaturaAtiva == true
```

## Codigo-fonte de referencia

| Arquivo | Responsabilidade |
|---------|------------------|
| `src/GLOWAPI.Domain/Enums/ModuloAssinatura.cs` | Enum de modulos |
| `src/GLOWAPI.Domain/Enums/PermissaoNegocio.cs` | Enum de permissoes |
| `src/GLOWAPI.Application/Services/PlanoComercialCatalogo.cs` | Modulos por plano |
| `src/GLOWAPI.Application/Services/MatrizPermissaoNegocioService.cs` | Permissoes por role |
| `src/GLOWAPI.API/Middlewares/PermissionMiddleware.cs` | Enforcement HTTP |

## Documentos relacionados

- [tasks-profissionais-estabelecimento-acesso.md](../tasks-profissionais-estabelecimento-acesso.md) — epic de equipe e permissoes
- [frontend/README.md](../frontend/README.md) — integracao frontend (fluxo cliente)
