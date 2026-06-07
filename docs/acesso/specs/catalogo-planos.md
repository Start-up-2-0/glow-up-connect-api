# Spec — Catalogo de planos (seed)

## Objetivo

Garantir que o banco contenha **exatamente 3 planos ativos** para contratacao, alinhados ao `PlanoComercialCatalogo`.

## Planos canonicos

| Id | Nome | Descricao | Preco | Periodo | LimiteProfissionais | LimiteServicos | LimiteAgendamentos | Ativo |
|:--:|------|-----------|------:|---------|:-------------------:|:--------------:|:------------------:|:-----:|
| 1 | Basic | Plano de entrada para operacao solo com agenda, servicos e e-mail | 29.99 | Mensal | 1 | 10 | null | true |
| 2 | Plus | Equipe, convites e notificacoes WhatsApp para o negocio | 99.90 | Mensal | null | null | null | true |
| 3 | Premium | Caixa, financeiro, comissoes e prioridade no marketplace | 199.90 | Mensal | null | null | null | true |

### Notas sobre limites

- **Basic:** `LimiteProfissionais/Servicos/Agendamentos` no banco alimentam `Limites` da assinatura.
- **Plus/Premium:** `null` = ilimitado nos services de validacao.
- **LimiteUsuarios** vem do catalogo (`1` no Basic; `null` nos demais). **LimiteAgendamentosPorDia** e ilimitado em todos os planos pagos (`null`).

### Identificacao pelo nome

O nome **deve** conter a palavra-chave para o catalogo resolver modulos:

- `Basic` → substring `basic`
- `Plus` → substring `plus`
- `Premium` → substring `premium`

Nao usar nomes como "Plano Pro" ou "Basico Premium" — caem no fallback Basic.

## Migrations

- `SeedPlanosComerciais` — upsert dos 3 planos canonicos.
- `CorrigirLimiteAgendamentosPlanoBasic` — remove teto de agendamentos do Basic (`LimiteAgendamentos = null`), alinhado ao catalogo.

Comportamento do `Up()`:

1. Remove planos cujo nome nao esta na lista canonica **e** que nao possuem assinaturas vinculadas.
2. Faz upsert por `Nome` (indice unico) dos 3 planos.
3. Ajusta sequence do `Id` em PostgreSQL.

## API exposta

```http
GET /api/planos
```

Resposta esperada (exemplo Basic):

```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "nome": "Basic",
      "preco": 0,
      "periodo": "Mensal",
      "limiteProfissionais": 1,
      "limiteServicos": 10,
      "limiteAgendamentos": null,
      "limiteUsuarios": 1,
      "limiteAgendamentosPorDia": null,
      "prioridadeListagemPublica": false,
      "modulos": ["Agenda", "Servicos", "HorariosAtendimento", "Notificacoes", "Email"],
      "funcionalidades": ["Cadastro de servicos", "Agenda simples", "..."]
    }
  ]
}
```

## Criterios de aceite

- [ ] Tabela `Planos` contem somente Basic, Plus, Premium ativos apos migration.
- [ ] Nomes unicos respeitados.
- [ ] `GET /api/planos` retorna 3 itens ordenados por preco.
- [ ] Modulos e funcionalidades batem com specs em [planos/](./planos/).
- [ ] Assinaturas existentes em planos removidos nao quebram (planos com FK sao preservados).

## Evolucao futura

- CRUD administrativo de planos ficara no Dashboard Operacional (fora desta API).
- Novos periodos (Trimestral, Anual) podem ser adicionados como linhas separadas ou campo `Periodo` distinto — hoje o seed usa apenas `Mensal`.
