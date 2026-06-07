# Spec — Modulo ComissaoProfissionais

## Identificacao

- **Enum:** `ModuloAssinatura.ComissaoProfissionais` (valor `14`)
- **Plano minimo:** **Premium**

## Objetivo

Comissao automatica para profissionais vinculados ao estabelecimento, com rastreabilidade por atendimento.

## Funcionalidades comerciais (catalogo)

- Comissao automatica
- Gestao completa da equipe (aspecto financeiro)

## Enforcement

- **Catalogo:** incluido no Premium.
- **HTTP:** **sem** `[RequerModuloAssinatura(ComissaoProfissionais)]` hoje.
- Entidade `ComissaoProfissional` existe no dominio; endpoints de consulta **em evolucao**.

## Permissoes previstas

| Persona | Permissao |
|---------|-----------|
| Owner/Admin | visualizar comissoes da equipe |
| Profissional | `ComissaoVisualizarPropria` |

## Limites

Nenhum.

## Status

**Planejado** — modulo listado na API; regras de calculo e endpoints **pendentes**.

## Codigo de referencia

- Entidade `ComissaoProfissional`
- `docs/acesso/estabelecimento/profissional.md`
- `PlanoComercialCatalogo.ModulosPremium`
