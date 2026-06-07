# Specs — Planos e modulos comerciais

Especificacoes canonicas do catalogo comercial da GLOWAPI: **3 planos**, **12 modulos operacionais** e as regras de liberacao.

Fonte de verdade no codigo:

| Artefato | Caminho |
|----------|---------|
| Enum de modulos | `src/GLOWAPI.Domain/Enums/ModuloAssinatura.cs` |
| Catalogo por plano | `src/GLOWAPI.Application/Services/PlanoComercialCatalogo.cs` |
| Resolucao de modulos | `src/GLOWAPI.Application/Services/ModulosAssinaturaService.cs` |
| Enforcement HTTP | `src/GLOWAPI.API/Middlewares/PermissionMiddleware.cs` |
| Seed de planos | migration `SeedPlanosComerciais` |

## Documentos

| Documento | Conteudo |
|-----------|----------|
| [liberacao-modulos.md](./liberacao-modulos.md) | Fluxo de liberacao (assinatura, pagamento, middleware) |
| [catalogo-planos.md](./catalogo-planos.md) | Contrato de seed dos 3 planos no banco |
| [promocao-lancamento.md](./promocao-lancamento.md) | Trial 30 dias — 100 primeiros tenants |
| [ciclo-cobranca.md](./ciclo-cobranca.md) | Dia de vencimento, alertas D-3 e geracao D-2 |
| [cobrancas-assinatura.md](./cobrancas-assinatura.md) | Pagamento interno, status e listagem API |

### Specs de modulos

| Modulo | Plano minimo | Spec |
|--------|:------------:|------|
| Estabelecimento | Qualquer ativa | [modulos/estabelecimento.md](./modulos/estabelecimento.md) |
| Assinatura | Qualquer ativa | [modulos/assinatura.md](./modulos/assinatura.md) |
| Agenda | Basic | [modulos/agenda.md](./modulos/agenda.md) |
| Servicos | Basic | [modulos/servicos.md](./modulos/servicos.md) |
| HorariosAtendimento | Basic | [modulos/horarios-atendimento.md](./modulos/horarios-atendimento.md) |
| Notificacoes | Basic | [modulos/notificacoes.md](./modulos/notificacoes.md) |
| Email | Basic | [modulos/email.md](./modulos/email.md) |
| Profissionais | Plus | [modulos/profissionais.md](./modulos/profissionais.md) |
| WhatsApp | Plus | [modulos/whatsapp.md](./modulos/whatsapp.md) |
| Caixa | Premium | [modulos/caixa.md](./modulos/caixa.md) |
| Financeiro | Premium | [modulos/financeiro.md](./modulos/financeiro.md) |
| ComissaoProfissionais | Premium | [modulos/comissao-profissionais.md](./modulos/comissao-profissionais.md) |

### Specs de planos

| Plano | Preco mensal | Spec |
|-------|:------------:|------|
| Basic | R$ 0,00 | [planos/basic.md](./planos/basic.md) |
| Plus | R$ 99,90 | [planos/plus.md](./planos/plus.md) |
| Premium | R$ 199,90 | [planos/premium.md](./planos/premium.md) |

## Matriz plano x modulo

```text
                    Basic  Plus  Premium
Estabelecimento       *      *      *
Assinatura            *      *      *
Agenda                *      *      *
Servicos              *      *      *
HorariosAtendimento   *      *      *
Notificacoes          *      *      *
Email                 *      *      *
Profissionais               *      *
WhatsApp                    *      *
Caixa                              *
Financeiro                         *
ComissaoProfissionais              *
```

## Regra do frontend

```text
Exibir funcionalidade X =
  autenticado
  AND AssinaturaAtiva
  AND modulo X em Modulos[]
  AND permissao Y em Permissoes[]   (rotas de negocio)
```

Consultar: `GET /api/usuario/me/estabelecimentos` e `GET /api/planos`.

## Documentos relacionados

- [../modulos.md](../modulos.md) — referencia rapida de endpoints
- [../README.md](../README.md) — controle de acesso geral
- [../../tasks-onboarding-assinatura.md](../../tasks-onboarding-assinatura.md) — epic de onboarding
