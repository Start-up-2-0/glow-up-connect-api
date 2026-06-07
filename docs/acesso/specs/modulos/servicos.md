# Spec — Modulo Servicos

## Identificacao

- **Enum:** `ModuloAssinatura.Servicos` (valor `3`)
- **Plano minimo:** **Basic**

## Objetivo

Cadastro e gestao do catalogo de servicos do negocio, incluindo vinculos com profissionais executores.

## Enforcement

- **HTTP:** `[RequerModuloAssinatura(..., ModuloAssinatura.Servicos, "estabelecimentoId")]`
- **Service:** `ServicoNegocioService.ValidarLimiteServicosAsync` — conta servicos ativos vs `Limites.Servicos`.

## Endpoints

Prefixo: `/api/estabelecimentos/{estabelecimentoId}`

| Permissao | Metodos |
|-----------|---------|
| ServicoVisualizar | GET `/servicos` |
| ServicoGerenciar | POST `/servicos`, PUT/PATCH `/servicos/{id}`, vinculos `/servicos/{id}/profissionais/*` |

Autonomo (mesmo modulo, facade): `/api/profissionais-autonomos/{profissionalId}/servicos/*`

## Limites por plano

| Plano | LimiteServicos (banco) |
|-------|------------------------|
| Basic | 10 |
| Plus | ilimitado |
| Premium | ilimitado |

Excesso: excecao `LimiteServicosNegocioExcedidoException`.

## Status

**Implementado**.

## Codigo de referencia

- `ServicoNegocioService`
- `EstabelecimentosController`
