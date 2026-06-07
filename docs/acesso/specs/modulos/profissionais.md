# Spec — Modulo Profissionais

## Identificacao

- **Enum:** `ModuloAssinatura.Profissionais` (valor `2`)
- **Plano minimo:** **Plus**

## Objetivo

Multiusuario no estabelecimento: equipe interna, convites de profissionais, gestao de vinculos e roles.

## Enforcement

- **HTTP:** `[RequerModuloAssinatura(..., ModuloAssinatura.Profissionais, "estabelecimentoId")]`
- **Service:** `ValidarLimiteUsuariosAsync` e `ValidarLimiteProfissionaisAsync` em `EquipeNegocioService` e `ConviteNegocioService`.

## Endpoints

| Area | Rotas | Permissao |
|------|-------|-----------|
| Equipe usuarios | POST/PATCH `/api/estabelecimentos/{id}/equipe/usuarios/*` | EquipeGerenciar |
| Profissionais equipe | POST `/equipe/profissionais`, PATCH `.../status` | ProfissionalConvidar / ProfissionalGerenciar |
| Convites | POST `/api/convites/estabelecimentos/{id}/profissionais` | ProfissionalConvidar |

## Limites por plano

| Plano | Usuarios (catalogo) | Profissionais (banco) |
|-------|:-------------------:|:---------------------:|
| Basic | 1 | 1 |
| Plus | ilimitado | ilimitado |
| Premium | ilimitado | ilimitado |

Basic bloqueia equipe mesmo que usuario tente via API → 403 `SUBSCRIPTION_MODULE_BLOCKED`.

## Personas

- Owner/Admin/Manager: convidar e gerenciar (com permissao).
- Profissional autonomo no Basic: **nao** recebe este modulo (operacao solo).

## Status

**Implementado**.

## Codigo de referencia

- `EquipeNegocioService`
- `ConviteNegocioService`
- `EstabelecimentosController`, `ConvitesController`
