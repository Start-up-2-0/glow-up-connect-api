# Plano Premium

> Spec canonica: [../specs/planos/premium.md](../specs/planos/premium.md).

Identificacao no catalogo: nome contem **`premium`**.

Inclui **todos os modulos do Essencial** mais os abaixo.

## Modulos adicionais (alem do Essencial)

| Modulo | Disponivel |
|--------|:----------:|
| Caixa | sim |
| Financeiro | sim |
| ComissaoProfissionais | sim |
| Clientes | sim |

## Multi-loja

| Limite | Valor |
|--------|-------|
| Estabelecimentos por assinatura | **5** (1 matriz + ate 4 filiais) |
| Cobranca | Uma fatura na assinatura titular (matriz) |

Filial herda modulos e limites da assinatura titular via `AssinaturaEstabelecimento`.

## Limites comerciais

| Limite | Valor |
|--------|-------|
| Usuarios no negocio | Ilimitado |
| Agendamentos por dia | Ilimitado |
| Prioridade na listagem publica | **Sim** |

Estabelecimentos Premium podem aparecer com prioridade em `/api/publico/estabelecimentos/proximos`.

## Funcionalidades (catalogo comercial)

Tudo do Essencial, mais:

- Ate 5 unidades na mesma assinatura
- Painel consolidado da rede (`GET /api/rede/resumo`)
- CRM de clientes
- Auditoria de negocio
- Controle de caixa
- Fluxo financeiro
- Comissao automatica
- Relatorios financeiros
- Dashboard avancado
- Metricas do estabelecimento
- Historico financeiro
- Gestao completa da equipe
- Prioridade na busca e listagem do marketplace

## O que o Premium desbloqueia na operacao

| Area | Endpoint base | Permissao tipica |
|------|---------------|------------------|
| Resumo do caixa | GET `.../caixa` | CaixaVisualizar |
| Lancamentos | GET `.../caixa/lancamentos` | CaixaVisualizar |
| Comissoes | *(matriz Profissional; endpoints em evolucao)* | ComissaoVisualizarPropria |
| Financeiro | *(catalogo; endpoints dedicados em evolucao)* | CaixaGerenciar (Owner) |
| Rede | GET `/api/rede/resumo` | Owner |
| Clientes | GET `.../clientes` | Clientes |
| Auditoria | GET `.../auditoria` | Owner |

## Modulos x roles (exemplo Owner)

Com Premium ativo, Owner tem permissao **e** modulo para:

```text
Agenda + Servicos + Horarios + Profissionais + Caixa + WhatsApp + Email + Clientes
```

Receptionist no mesmo Premium **nao** ve caixa (permissoes de role, nao de plano).

## Downgrade

Premium para Essencial e bloqueado enquanto houver mais de 1 loja vinculada.

## Planos anteriores

- [plano-essencial.md](./plano-essencial.md) (substitui Basic e Plus)

## Referencia de modulos

- [../modulos.md](../modulos.md)

