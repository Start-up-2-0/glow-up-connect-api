# Referência de Domínio — Glow Up Connect V1

## Entidades (20)

Usuario, Estabelecimento, EstabelecimentoUsuario, Profissional, ProfissionalEstabelecimento,
Endereco, Servico, ProfissionalServico, HorarioFuncionamentoEstabelecimento,
HorarioAtendimentoProfissional, Agendamento, AgendamentoItem, Caixa, LancamentoCaixa,
Plano, Assinatura, Pagamento, WebhookPagamento, ComissaoProfissional, MetaProfissional

## Roles globais (UserRole)

Cliente | DonoEstabelecimento | ProfissionalAutonomo | ProfissionalEstabelecimento | Admin

## Roles no estabelecimento (EstablishmentUserRole)

Owner | Admin | Manager | Receptionist

## Tipos de profissional (ProfessionalType)

Autonomo | VinculadoEstabelecimento

## Status principais

**Agendamento:** PendentePagamento, Confirmado, EmAtendimento, Concluido, Cancelado, Expirado, Reembolsado

**Pagamento:** Pendente, Autorizado, Pago, Recusado, Cancelado, Estornado, Chargeback, Expirado

**Assinatura:** Ativa, PendentePagamento, Cancelada, Expirada, Suspensa, Trial

**LancamentoCaixa:** EntradaAgendamento, ComissaoProfissional, TaxaPlataforma, Assinatura, Saque, Estorno, Chargeback, AjusteManual

**Comissao:** Percentual, ValorFixo, Mista

**Meta:** Atendimentos, Faturamento, Mista

## Relacionamentos-chave

- Usuario 1:1 Profissional (opcional)
- Usuario 1:N EstabelecimentoUsuario, Agendamento
- Estabelecimento 1:1 Endereco, Caixa | 1:N Servico, ProfissionalEstabelecimento
- Profissional autônomo: 1:1 Endereco, Caixa | 1:N Servico, HorarioAtendimento
- Agendamento 1:N AgendamentoItem, Pagamento
- Caixa 1:N LancamentoCaixa
- Plano 1:N Assinatura 1:N Pagamento
- ProfissionalEstabelecimento 1:N ComissaoProfissional, MetaProfissional

## Regras de agendamento

- Loja: precisa `EstabelecimentoId`; respeita horário da loja E do profissional
- Autônomo: precisa `ProfissionalAutonomoId`
- Item: ServicoId + ProfissionalId + Inicio/Fim + Valor + Status
- Pode repassar item para outro profissional (`RepassadoDeProfissionalId`)
- Confirmação exige disponibilidade + pagamento
- Profissional vinculado só visualiza itens atribuídos a ele

## Regras de serviço

- Serviço de loja: `EstabelecimentoId` obrigatório
- Serviço de autônomo: `ProfissionalAutonomoId` obrigatório
- `ProfissionalServico` permite preço/duração específicos por profissional

## Regras financeiras

- Cliente paga → gateway confirma via webhook → gera Pagamento + LancamentoCaixa
- Plano gratuito: desconta taxa fixa por agendamento
- Caixa: SaldoTotal, SaldoDisponivel, SaldoRetido
- Saque: validar mínimo, saldo, pendências, chargeback
- Profissional vinculado: comissão rastreável, sem caixa geral próprio na loja
- Pagamento (gateway) ≠ Caixa (saldo interno) ≠ LancamentoCaixa (movimentação)

## Assinaturas e planos

- Assinatura pertence a estabelecimento OU profissional autônomo (nunca ambos)
- Cobrança via gateway externo; status atualizado por webhook
- Plano gratuito limitado; plano pago via assinatura
- Limites: profissionais, serviços, agendamentos
- CRUD de planos: Dashboard Operacional (fora desta API)

## Gateways

MercadoPago, AbacatePay — pagamentos e assinaturas via webhook idempotente (WebhookPagamento)

- Todo webhook recebido deve ser registrado
- Processamento idempotente — eventos duplicados não geram lançamentos duplicados
- Erros de processamento rastreáveis

## Links públicos

- Estabelecimento e Profissional possuem `PublicGuid` único e imutável
- `/agendar/loja/{publicGuid}`
- `/agendar/profissional/{publicGuid}`

## Endpoints previstos (Plano)

- GET /planos — listar planos ativos
- POST /assinaturas/trocar-plano — trocar plano do estabelecimento ou autônomo

## Ordem de implementação (migrations incrementais)

```txt
01 Usuario → 02 Estabelecimento → 03 EstabelecimentoUsuario → 04 Profissional
→ 05 ProfissionalEstabelecimento → 06 Endereco → 07 Servico → 08 ProfissionalServico
→ 09 HorarioFuncionamentoEstabelecimento → 10 HorarioAtendimentoProfissional
→ 11 Agendamento → 12 AgendamentoItem → 13 Caixa → 14 Plano → 15 Assinatura
→ 16 Pagamento → 17 LancamentoCaixa → 18 WebhookPagamento
→ 19 ComissaoProfissional → 20 MetaProfissional
```

Nota: o projeto já possui migration `InitialCreate` com todas as entidades. Novas alterações devem gerar migrations incrementais.

## Pontos em aberto (não assumir sem decisão)

- Profissional em múltiplos estabelecimentos?
- Pagamento no local vs sempre plataforma?
- Saque para profissional vinculado?
- Momento do cálculo de comissão?
- Agendamento sem pagamento confirmado?
- Troca de profissional após pagamento?
- Profissional autônomo com equipe no futuro?
