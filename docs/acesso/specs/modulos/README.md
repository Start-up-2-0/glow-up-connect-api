# Specs de modulos

Cada modulo e um flag comercial liberado pela assinatura ativa. Ver [../liberacao-modulos.md](../liberacao-modulos.md).

## Template de spec

Todo modulo segue esta estrutura:

1. **Identificacao** — valor do enum `ModuloAssinatura`
2. **Plano minimo** — primeiro plano que inclui o modulo
3. **Objetivo** — o que o modulo representa no produto
4. **Enforcement** — HTTP middleware, service ou assincrono
5. **Endpoints** — rotas protegidas
6. **Permissoes** — permissoes de negocio tipicas (alem do modulo)
7. **Limites** — restricoes numericas por plano
8. **Status** — implementado / parcial / planejado

## Indice

| Spec | Enum |
|------|------|
| [estabelecimento.md](./estabelecimento.md) | `Estabelecimento` |
| [assinatura.md](./assinatura.md) | `Assinatura` |
| [agenda.md](./agenda.md) | `Agenda` |
| [servicos.md](./servicos.md) | `Servicos` |
| [horarios-atendimento.md](./horarios-atendimento.md) | `HorariosAtendimento` |
| [notificacoes.md](./notificacoes.md) | `Notificacoes` |
| [email.md](./email.md) | `Email` |
| [profissionais.md](./profissionais.md) | `Profissionais` |
| [whatsapp.md](./whatsapp.md) | `WhatsApp` |
| [caixa.md](./caixa.md) | `Caixa` |
| [financeiro.md](./financeiro.md) | `Financeiro` |
| [comissao-profissionais.md](./comissao-profissionais.md) | `ComissaoProfissionais` |
