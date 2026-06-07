# Spec — Modulo Estabelecimento

## Identificacao

- **Enum:** `ModuloAssinatura.Estabelecimento` (valor `1`)
- **Plano minimo:** qualquer assinatura **ativa**

## Objetivo

Representa o tenant comercial (estabelecimento ou negocio sintetico do autonomo). Modulo base sempre presente quando a assinatura esta ativa.

## O que libera

- Existencia operacional do negocio na plataforma.
- Vinculo usuario ↔ estabelecimento via `EstabelecimentoUsuario`.
- Endpoints de perfil do negocio (dependem de permissao, nao de modulo especifico alem deste base).

## Enforcement

- **Automatico:** `ModulosAssinaturaService` inclui em toda assinatura ativa.
- **HTTP:** nao ha atributo dedicado; rotas de perfil usam apenas `[RequerPermissaoNegocio(NegocioEditar)]`.

## Endpoints relacionados

| Metodo | Rota | Permissao |
|--------|------|-----------|
| PUT | `/api/estabelecimentos/{id}/perfil` | NegocioEditar |
| POST | `/api/estabelecimentos/{id}/whatsapp/iniciar-confirmacao` | NegocioEditar |
| POST | `/api/estabelecimentos/{id}/whatsapp/confirmar` | NegocioEditar |

## Limites

Nenhum limite especifico deste modulo.

## Status

**Implementado** — tenant unificado apos migration `UnifyCommercialModelToTenant`.

## Codigo de referencia

- `ModulosAssinaturaService.ModulosEstabelecimento`
- `EstabelecimentosController` (rotas de perfil)
