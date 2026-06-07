# Spec — Plano Plus

## Identificacao

| Campo | Valor |
|-------|-------|
| Nome (banco) | `Plus` |
| Slug catalogo | `plus` |
| Id seed | `2` |
| Preco | R$ 99,90 / Mensal |
| Ativo | true |

## Objetivo

Estabelecimentos que precisam de **equipe** e **WhatsApp automatico** ao cliente, sem modulo financeiro completo.

## Modulos incluidos

Tudo do [Basic](./basic.md), mais:

```text
Profissionais
WhatsApp
```

## Funcionalidades adicionais (catalogo)

- Multiusuario
- Agenda compartilhada
- Gestao de profissionais
- Historico de clientes
- Confirmacao automatica via WhatsApp
- Lembrete automatico de agendamento
- Aviso de cancelamento
- Dashboard basico
- Relatorios basicos

## Limites

| Limite | Valor |
|--------|-------|
| Usuarios | ilimitado |
| Profissionais | ilimitado |
| Servicos | ilimitado |
| Agendamentos por dia | ilimitado |
| Prioridade listagem publica | nao |

Campos `LimiteProfissionais`, `LimiteServicos`, `LimiteAgendamentos` no banco: **null**.

## O que NAO inclui

- Caixa (`GET .../caixa` → 403)
- Financeiro (relatorios avancados)
- Comissao automatica
- Prioridade no marketplace

## Personas alvo

- Saloes e barbearias com equipe
- Estabelecimentos que querem WhatsApp ao cliente sem financeiro

## Permissoes tipicas (alem do modulo)

| Acao | Permissao |
|------|-----------|
| Convidar profissional | ProfissionalConvidar |
| Cadastrar usuario equipe | EquipeGerenciar |

## Upgrade

| Necessidade | Plano |
|-------------|-------|
| Caixa, financeiro, comissoes, prioridade | [Premium](./premium.md) |

## Criterios de aceite

- [ ] Seed com nome `Plus`, preco 99.90, limites null.
- [ ] Modulo Profissionais liberado apos assinatura ativa.
- [ ] WhatsApp ao cliente enfileirado em confirmacao de agendamento.
- [ ] Caixa continua 403.

## Specs de modulos

- [../modulos/profissionais.md](../modulos/profissionais.md)
- [../modulos/whatsapp.md](../modulos/whatsapp.md)
