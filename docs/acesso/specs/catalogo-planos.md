# Spec — Catalogo de planos (Essencial + Premium)

## Objetivo

Garantir que o banco contenha **exatamente 2 planos ativos** para contratacao, alinhados ao `PlanoComercialCatalogo`.

## Planos canonicos

| Id | Nome | Descricao | Preco | LimiteEstabelecimentos | Ativo |
|:--:|------|-----------|------:|:----------------------:|:-----:|
| 2 | Essencial | Operacao completa com equipe, WhatsApp e gestao para uma unidade | 79.90 | 1 | true |
| 3 | Premium | Caixa, financeiro, comissoes, ate 5 unidades e prioridade no marketplace | 199.90 | 5 | true |

### Planos legados (inativos)

| Id | Nome | Status |
|:--:|------|--------|
| 1 | Basic | `Ativo = false` — assinaturas migradas para Essencial (PlanoId 2) |

O antigo **Plus** foi renomeado para **Essencial** no `Plano.Id = 2`.

### Notas sobre limites

- **Essencial:** modulos do antigo Plus; `LimiteEstabelecimentos = 1`.
- **Premium:** modulos financeiros + `Clientes`; `LimiteEstabelecimentos = 5`.
- **LimiteUsuarios** vem do catalogo (`null` em ambos).

## Migration

`ReestruturarPlanosEssencialPremium`:

1. Adiciona coluna `LimiteEstabelecimentos` e tabela `AssinaturaEstabelecimentos`.
2. Migra assinaturas Basic (`PlanoId = 1`) para Essencial (`PlanoId = 2`).
3. Renomeia Plus para Essencial no `Plano.Id = 2`.
4. Define Premium com `LimiteEstabelecimentos = 5`.
5. Desativa Basic (`Ativo = false`).
6. Backfill de vinculo matriz para assinaturas Premium ativas.

## API exposta

```http
GET /api/planos
```

Retorna 2 planos ativos ordenados por preco.

```http
POST /api/assinaturas/{id}/estabelecimentos
```

Adiciona unidade filial (Premium).

```http
GET /api/rede/resumo?assinaturaId=
```

Painel consolidado da rede (Premium).

## Criterios de aceite

- [ ] `GET /api/planos` retorna Essencial e Premium.
- [ ] Essencial: 1 loja, modulos do antigo Plus.
- [ ] Premium: ate 5 lojas, modulos financeiros + Clientes.
- [ ] Filial herda modulos da assinatura titular.
