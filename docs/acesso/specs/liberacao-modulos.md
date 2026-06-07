# Spec — Liberacao de modulos

## Objetivo

Definir como a API decide quais modulos um estabelecimento (tenant) pode usar apos contratar um plano.

## Premissas

1. Todo tenant comercial possui um `EstabelecimentoId` (loja ou tenant sintetico do autonomo).
2. A assinatura pertence ao estabelecimento (`Assinatura.EstabelecimentoId`).
3. Modulos liberam com `Assinatura.Status` em **Ativa** ou **Trial** (promocao de lancamento).
4. O plano no banco (`Plano.Nome`) determina o perfil comercial via `PlanoComercialCatalogo`.
5. Modulo e permissao de role sao camadas independentes.

## Fluxo de liberacao

```text
1. Usuario autenticado escolhe plano (GET /api/planos)
2. POST /api/assinaturas com PlanoId
   -> Assinatura criada: PendentePagamento
   -> Modulos operacionais BLOQUEADOS
3. Pagamento inicial gerado no gateway
4. Webhook confirma pagamento (POST /api/webhooks/pagamentos)
   -> Assinatura.Status = Ativa
   -> Inicio/Fim calculados pelo Periodo do plano
5. ModulosAssinaturaService resolve lista efetiva
6. Endpoints com [RequerModuloAssinatura] passam a responder 200 (se permissao OK)
```

## Composicao da lista de modulos

Sempre que assinatura ativa:

```text
Modulos efetivos = [Estabelecimento, Assinatura] + modulos do plano
```

Implementacao: `ModulosAssinaturaService.CriarResposta`.

## Identificacao do plano

`PlanoComercialCatalogo.Obter(plano)` normaliza o nome (lowercase, sem acentos) e busca substring:

| Substring no nome | Perfil |
|-------------------|--------|
| `premium` | Premium |
| `plus` | Plus |
| `basic` ou `basico` | Basic |
| nenhuma | Fallback Basic (modulos basicos; limites do banco) |

**Regra de seed:** usar exatamente `Basic`, `Plus`, `Premium` — ver [catalogo-planos.md](./catalogo-planos.md).

## Camadas de enforcement

### 1. HTTP — PermissionMiddleware

Atributo `[RequerModuloAssinatura(TipoAssinatura, Modulo, parametroId)]` no controller.

- Le `estabelecimentoId` da rota ou query.
- Chama `IModulosAssinaturaService.PossuiModuloPorEstabelecimentoAsync`.
- Falha: **403** `SUBSCRIPTION_MODULE_BLOCKED`.

### 2. Service — limites comerciais

Services consultam `ObterPorEstabelecimentoAsync` e validam `Limites`:

| Limite | Service | Campo |
|--------|---------|-------|
| Usuarios | `EquipeNegocioService`, `ConviteNegocioService` | `Limites.Usuarios` (catalogo) |
| Profissionais | `EquipeNegocioService`, `ConviteNegocioService` | `Limites.Profissionais` (banco) |
| Servicos | `ServicoNegocioService` | `Limites.Servicos` (banco) |

`Limites.AgendamentosPorDia` e exposto na API mas **ainda sem enforcement** no fluxo de agendamento.

### 3. Assincrono — notificacoes

`AgendamentoNotificacaoService` checa modulo **WhatsApp** antes de enfileirar mensagem ao cliente. Sem modulo, nao envia WhatsApp (e-mail de equipe/assinatura segue outros fluxos).

## Respostas da API para o frontend

### Catalogo (pre-contratacao)

```http
GET /api/planos
```

Retorna por plano: `modulos`, `funcionalidades`, limites comerciais, `prioridadeListagemPublica`.

### Contexto operacional (pos-login)

```http
GET /api/usuario/me/estabelecimentos
```

Retorna por negocio: `assinaturaAtiva`, `planoNome`, `modulos[]`, `permissoes[]`.

## Estados da assinatura vs modulos

| Status | Modulos operacionais |
|--------|---------------------|
| `PendentePagamento` | Bloqueados |
| `Ativa` | Liberados conforme plano |
| `Cancelada` / `Suspensa` / outros | Bloqueados |

## Troca de plano

1. `POST /api/assinaturas/{id}/trocar-plano`
2. Se upgrade com cobranca: `PlanoAlteracaoPendenteId` preenchido; modulos antigos permanecem ate pagamento.
3. Webhook aprova pagamento: `PlanoId` atualizado; novos modulos liberados.

## Erros padronizados

| HTTP | Code | Causa |
|------|------|-------|
| 401 | UNAUTHORIZED | Sem token |
| 400 | INVALID_SUBSCRIPTION_SCOPE | `estabelecimentoId` ausente na rota |
| 403 | SUBSCRIPTION_MODULE_BLOCKED | Assinatura inativa ou modulo ausente |
| 403 | *(dominio)* | Sem permissao de negocio |

## Criterios de aceite

- [ ] Seed contem apenas Basic, Plus, Premium ativos.
- [ ] `GET /api/planos` retorna 3 planos com modulos corretos.
- [ ] Assinatura pendente bloqueia modulo Agenda.
- [ ] Webhook de pagamento libera modulos do plano contratado.
- [ ] Endpoint de caixa retorna 403 no Plus e 200 no Premium (com permissao).
- [ ] `GET /api/usuario/me/estabelecimentos` reflete modulos apos ativacao.

## Codigo de referencia

- `src/GLOWAPI.Application/Services/ModulosAssinaturaService.cs`
- `src/GLOWAPI.Application/Services/PlanoComercialCatalogo.cs`
- `src/GLOWAPI.API/Middlewares/PermissionMiddleware.cs`
- `src/GLOWAPI.Application/Services/WebhookPagamentoService.cs`
