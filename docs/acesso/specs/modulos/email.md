# Spec — Modulo Email

## Identificacao

- **Enum:** `ModuloAssinatura.Email` (valor `11`)
- **Plano minimo:** **Basic**

## Objetivo

Notificacoes por e-mail ao cliente e comunicacoes transacionais (confirmacao, lembrete, cancelamento de agendamento; confirmacao de pagamento de assinatura).

## Funcionalidades comerciais (catalogo)

- Confirmacao por e-mail
- Cancelamento por e-mail
- Lembrete por e-mail

## Enforcement

- **Catalogo:** incluido no Basic; listado em `modulos[]` da API.
- **HTTP:** sem atributo dedicado.
- **Assincrono:** e-mails de assinatura (`AssinaturaNotificacaoService`) e equipe (`EquipeNotificacaoService`) usam `CanalMensagemNotificacao.Email` sem checar modulo Email explicitamente hoje.

## Comportamento esperado (produto)

| Cenario | Basic | Plus/Premium |
|---------|:-----:|:------------:|
| E-mail ao cliente sobre agendamento | sim | sim |
| WhatsApp ao cliente | nao | sim (modulo WhatsApp) |

## Limites

Nenhum.

## Status

**Parcial** — modulo no catalogo e API; enforcement granular por modulo Email **pendente** nos services de notificacao.

## Evolucao

- Adicionar `PossuiModulo(Email)` antes de enfileirar e-mail ao cliente em `AgendamentoNotificacaoService`.

## Codigo de referencia

- `AssinaturaNotificacaoService`
- `EquipeNotificacaoService`
- `PlanoComercialCatalogo.ModulosBasic`
