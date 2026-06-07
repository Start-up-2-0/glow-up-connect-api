# Spec — Cobrancas de assinatura (Pagamento interno)

## Modelo

Cada ciclo gera um registro em `Pagamentos` com:

| Campo | Uso |
|-------|-----|
| `TipoCobranca` | Inicial, Recorrente, TrocaPlano |
| `NumeroCiclo` | Sequencial por assinatura |
| `DataVencimento` | Dia escolhido no ciclo |
| `DataGeracao` | Quando a cobranca foi criada (D-2) |
| `CicloInicio` / `CicloFim` | Periodo coberto |

## Status

| Status | Significado |
|--------|-------------|
| Pendente | Aguardando pagamento no gateway |
| Pago | Confirmado via webhook |
| Recusado | Falha no gateway |
| Cancelado | Cancelado/expirado |
| Atrasado | Pendente apos vencimento |

## API

`GET /api/assinaturas/{id}/cobrancas` — lista cobrancas do tenant (Owner autenticado).

## Servico

`CobrancaAssinaturaService` centraliza geracao, atualizacao por webhook e logs em `PagamentoHistorico`, `AssinaturaHistorico` e `AssinaturaRecorrenciaHistorico`.
