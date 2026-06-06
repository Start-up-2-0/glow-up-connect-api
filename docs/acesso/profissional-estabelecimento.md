# Acesso — Profissional de estabelecimento

Role global: **`UserRole.ProfissionalEstabelecimento`**

Profissional que trabalha **dentro de uma loja**, vinculado via `ProfissionalEstabelecimento` e, opcionalmente, `EstabelecimentoUsuario`.

## Como o acesso e montado

```text
Role global ProfissionalEstabelecimento
  + vinculo EstabelecimentoUsuario (Role: Profissional ou outra)
  + vinculo ProfissionalEstabelecimento ativo
  -> permissoes efetivas em MatrizPermissaoNegocioService
```

Permissoes operacionais seguem a **role no estabelecimento**, nao a role global isolada.

## Cenario mais comum

Convite aceito:

1. `POST /api/convites/{token}/aceitar`
2. Cria/atualiza `ProfissionalEstabelecimento`
3. Role no estabelecimento: `Profissional`
4. Permissoes: ver [estabelecimento/profissional.md](./estabelecimento/profissional.md)

## Modulos

Dependem do **plano do estabelecimento**, nao do profissional:

| Modulo | Uso |
|--------|-----|
| Agenda | Agenda propria, atendimentos |
| Servicos | Visualizar servicos vinculados |
| HorariosAtendimento | Ver e editar horarios proprios |
| ComissaoProfissionais | Premium — comissao propria (futuro) |

## Endpoints principais

Prefixo: `/api/estabelecimentos/{estabelecimentoId}`

- `GET /agenda/propria`
- `POST /atendimentos/{itemId}/iniciar`
- `POST /atendimentos/{itemId}/finalizar`
- Horarios e servicos conforme [profissional.md](./estabelecimento/profissional.md)

## O que nao acessa

- Assinatura do negocio (`/api/assinaturas`) — responsabilidade do dono
- Caixa, equipe, agenda geral (sem role administrativa)
- Rotas de autonomo (`/api/profissionais-autonomos/*`)

## Profissional com role administrativa

Se o mesmo usuario for **Admin** no estabelecimento **e** profissional vinculado:

- Permissoes administrativas **+** permissoes de profissional (uniao)

Ver [estabelecimento/owner-admin.md](./estabelecimento/owner-admin.md).

## Frontend

Descobrir acessos:

```http
GET /api/usuario/me/estabelecimentos
```

Campos uteis: `Role`, `PossuiVinculoProfissional`, `Permissoes`, `Modulos`.
