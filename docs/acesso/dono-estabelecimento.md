# Acesso — Dono de estabelecimento

Role global: **`UserRole.DonoEstabelecimento`**

Usuario que **contrata e administra** um ou mais estabelecimentos. A role global indica intencao de negocio; permissoes operacionais vêm da **role no estabelecimento** (tipicamente `Owner`).

## Fluxo tipico

```text
1. Login (DonoEstabelecimento)
2. POST /api/assinaturas (tipoAssinatura: Estabelecimento)
3. Pagamento confirmado -> assinatura Ativa
4. Vinculo automatico como Owner em EstabelecimentoUsuario
5. GET /api/usuario/me/estabelecimentos -> Permissoes + Modulos
6. Operacao via /api/estabelecimentos/{id}/*
```

## Role no estabelecimento

Apos onboarding, opera como **`Owner`** — ver [estabelecimento/owner-admin.md](./estabelecimento/owner-admin.md).

Permissoes e modulos efetivos = **Owner** ∩ **plano contratado**.

## Assinatura

| Metodo | Rota | Descricao |
|--------|------|-----------|
| POST | `/api/assinaturas` | Criar estabelecimento + assinatura |
| POST | `/api/assinaturas/{id}/trocar-plano` | Trocar plano |
| POST | `/api/assinaturas/{id}/cancelar` | Cancelar |

Requisito: `UserRole.DonoEstabelecimento` (ou usuario autorizado no vinculo).

## Modulos por plano contratado

| Plano | Documento |
|-------|-----------|
| Basic | [planos/plano-basic.md](./planos/plano-basic.md) |
| Plus | [planos/plano-plus.md](./planos/plano-plus.md) |
| Premium | [planos/plano-premium.md](./planos/plano-premium.md) |

## O que herda alem da gestao

- Rotas publicas — [publico.md](./publico.md)
- Pode usar o app como cliente (agendamentos em outras lojas) se quiser — [cliente.md](./cliente.md)

## Multiplos estabelecimentos

Cada estabelecimento tem:

- Assinatura e plano proprios
- Modulos proprios
- Role do usuario pode variar (Owner em um, Admin em outro)

Consultar lista completa:

```http
GET /api/usuario/me/estabelecimentos
```

## Diferenca vs Profissional autonomo

| Aspecto | DonoEstabelecimento | ProfissionalAutonomo |
|---------|---------------------|----------------------|
| Tipo assinatura | Estabelecimento | ProfissionalAutonomo |
| Rotas principais | `/api/estabelecimentos/{id}/*` | `/api/profissionais-autonomos/{id}/*` |
| Equipe / multiusuario | Plus+ | Nao (solo) |
| Role tipica | Owner | Owner (tenant interno) |

Ver [profissional-autonomo.md](./profissional-autonomo.md).

## Documentos relacionados

- [tasks-onboarding-assinatura.md](../tasks-onboarding-assinatura.md)
- [modulos.md](./modulos.md)
