# Modelagem V1 - Glow Up Connect

Este documento descreve a primeira versão da modelagem de entidades do Glow Up Connect.

O objetivo desta modelagem é servir como base para a criação das entidades no projeto `GLOWAPI.Domain`, respeitando a Clean Architecture do projeto.

## Premissas

- Um usuário possui apenas um papel global no sistema.
- O cliente final também é um usuário, mas não deve ser tratado como uma entidade principal separada nesta fase.
- Um estabelecimento pode ter mais de um usuário responsável.
- Cada usuário responsável pode ter uma função específica dentro do estabelecimento.
- Um profissional pode ser autônomo ou vinculado a um estabelecimento.
- Um profissional vinculado a estabelecimento deve visualizar apenas seus próprios agendamentos ou os agendamentos repassados para ele.
- Um profissional autônomo possui visão administrativa da própria operação.
- Estabelecimentos e profissionais autônomos podem ter assinatura.
- O plano gratuito existe, mas será limitado.
- Assinaturas de estabelecimentos e profissionais autônomos serão pagas via gateway externo.
- Pagamentos serão feitos por gateway externo e confirmados via webhook.
- Os gateways previstos inicialmente são Mercado Pago e AbacatePay.
- A localização do usuário cliente será usada para exibir estabelecimentos e profissionais autônomos próximos.

## Visão Geral das Entidades

```txt
Usuario
Estabelecimento
EstabelecimentoUsuario
Profissional
ProfissionalEstabelecimento
Servico
ProfissionalServico
HorarioFuncionamentoEstabelecimento
HorarioAtendimentoProfissional
Endereco
Agendamento
AgendamentoItem
Caixa
LancamentoCaixa
Assinatura
Pagamento
WebhookPagamento
Plano
ComissaoProfissional
MetaProfissional
```

## Links Públicos de Agendamento

Estabelecimentos e profissionais possuem `PublicGuid` para permitir links públicos de agendamento rápido sem expor o identificador interno do banco.

### Exemplos de rota pública

```txt
/agendar/loja/{publicGuid}
/agendar/profissional/{publicGuid}
```

### Regras

- O `PublicGuid` deve ser um `Guid`.
- O `PublicGuid` deve ser único por entidade.
- O `PublicGuid` deve ser gerado automaticamente na criação da entidade.
- O `PublicGuid` deve ser imutável após a criação.
- O `PublicGuid` não deve substituir o `Id` interno usado nos relacionamentos do banco.
- O `PublicGuid` deve ser usado em links públicos para evitar exposição do identificador interno.

## Usuario

Representa qualquer pessoa autenticada no sistema.

O usuário possui apenas um papel global.

### Campos principais

```txt
Id
Nome
Email
Telefone
Senha
Role
Tentivas
Ativo
CreateAd
UpdatedAt
```

### Roles globais sugeridas

```txt
Cliente
DonoEstabelecimento
ProfissionalAutonomo
ProfissionalEstabelecimento
Admin
```

### Regras

- Um usuário não pode ter mais de uma role global.
- Um usuário com role `Cliente` agenda e paga serviços.
- Um usuário com role `DonoEstabelecimento` administra um ou mais estabelecimentos.
- Um usuário com role `ProfissionalAutonomo` administra sua própria agenda, serviços, caixa e assinatura.
- Um usuário com role `ProfissionalEstabelecimento` atua dentro de um estabelecimento e acessa apenas sua própria operação.
- Um usuário com role `Admin` representa administração interna da plataforma.

## Estabelecimento

Representa uma loja, barbearia, salão, clínica ou outro local que oferece serviços com múltiplos profissionais.

### Campos principais

```txt
Id
PublicGuid
Nome
Descricao
Logo
Telefone
Email
Ativo
CreateAd
UpdatedAt
```

### Relacionamentos

```txt
Estabelecimento 1:N EstabelecimentoUsuario
Estabelecimento 1:N ProfissionalEstabelecimento
Estabelecimento 1:N Servico
Estabelecimento 1:N HorarioFuncionamentoEstabelecimento
Estabelecimento 1:1 Endereco
Estabelecimento 1:1 Caixa
Estabelecimento 1:N Agendamento
Estabelecimento 1:N Assinatura
```

### Regras

- Um estabelecimento pode possuir mais de um usuário responsável.
- Cada estabelecimento deve possuir um `PublicGuid` único.
- O `PublicGuid` será usado para geração de link público de agendamento rápido.
- O `PublicGuid` não deve expor o `Id` interno do banco.
- Um estabelecimento pode ter vários profissionais vinculados.
- Um estabelecimento possui horário de funcionamento próprio.
- Um estabelecimento possui endereço próprio.
- Um estabelecimento possui caixa financeiro próprio.
- Um estabelecimento pode ter assinatura ativa.

## EstabelecimentoUsuario

Representa o vínculo entre um usuário e um estabelecimento.

Serve para permitir múltiplos responsáveis por uma loja, com permissões diferentes dentro dela.

### Campos principais

```txt
Id
EstabelecimentoId
UsuarioId
RoleNoEstabelecimento
Ativo
CreateAd
UpdatedAt
```

### Roles dentro do estabelecimento sugeridas

```txt
Owner
Admin
Manager
Receptionist
```

### Regras

- Um estabelecimento pode ter mais de um usuário vinculado.
- Cada usuário vinculado possui uma função dentro do estabelecimento.
- A role global do usuário continua sendo única.
- Esta tabela não transforma um usuário em profissional.
- Profissionais vinculados ao estabelecimento devem ser representados em `ProfissionalEstabelecimento`.

## Profissional

Representa o perfil profissional de um usuário.

Pode ser autônomo ou vinculado a estabelecimento.

### Campos principais

```txt
Id
PublicGuid
UsuarioId
NomePublico
Biografia
TipoProfissional
Ativo
CreateAd
UpdatedAt
```

### Tipos sugeridos

```txt
Autonomo
VinculadoEstabelecimento
```

### Relacionamentos

```txt
Profissional 1:1 Usuario
Profissional 1:N ProfissionalEstabelecimento
Profissional 1:N ProfissionalServico
Profissional 1:N HorarioAtendimentoProfissional
Profissional 1:1 Endereco, quando autônomo
Profissional 1:1 Caixa, quando autônomo
Profissional 1:N AgendamentoItem
```

### Regras

- Um profissional autônomo funciona como dono da própria operação.
- Cada profissional deve possuir um `PublicGuid` único.
- O `PublicGuid` será usado para geração de link público de agendamento rápido.
- Profissionais autônomos podem usar o `PublicGuid` como link principal de agendamento.
- Profissionais vinculados a estabelecimento podem usar o `PublicGuid` para agendamento direto com eles, quando habilitado.
- O `PublicGuid` não deve expor o `Id` interno do banco.
- Um profissional autônomo possui serviços, horários, endereço, caixa e assinatura próprios.
- Um profissional vinculado a estabelecimento possui horários e serviços dentro da loja.
- Um profissional vinculado a estabelecimento enxerga apenas seus próprios agendamentos ou os agendamentos repassados para ele.

## ProfissionalEstabelecimento

Representa o vínculo entre um profissional e um estabelecimento.

### Campos principais

```txt
Id
ProfissionalId
EstabelecimentoId
Ativo
DataEntrada
DataSaida
PodeReceberAgendamento
CreateAd
UpdatedAt
```

### Relacionamentos

```txt
ProfissionalEstabelecimento N:1 Profissional
ProfissionalEstabelecimento N:1 Estabelecimento
ProfissionalEstabelecimento 1:N ComissaoProfissional
ProfissionalEstabelecimento 1:N MetaProfissional
```

### Regras

- Um profissional pode ser vinculado a um estabelecimento.
- O vínculo define se ele pode receber agendamentos pela loja.
- Regras de comissão e meta podem ser configuradas por vínculo.

## Servico

Representa um serviço oferecido por estabelecimento ou profissional autônomo.

Exemplos: corte masculino, manicure, limpeza de pele, sobrancelha.

### Campos principais

```txt
Id
EstabelecimentoId
ProfissionalAutonomoId
Nome
Descricao
PrecoBase
DuracaoMinutos
Ativo
CreateAd
UpdatedAt
```

### Relacionamentos

```txt
Servico N:1 Estabelecimento, quando serviço de loja
Servico N:1 Profissional, quando serviço de autônomo
Servico 1:N ProfissionalServico
Servico 1:N AgendamentoItem
```

### Regras

- Serviço de estabelecimento deve possuir `EstabelecimentoId`.
- Serviço de profissional autônomo deve possuir `ProfissionalAutonomoId`.
- O mesmo serviço da loja pode ser vinculado a vários profissionais.
- Preço e duração podem ser sobrescritos por profissional em `ProfissionalServico`, se necessário.

## ProfissionalServico

Representa quais serviços um profissional executa.

### Campos principais

```txt
Id
ProfissionalId
ServicoId
Preco
DuracaoMinutos
Ativo
CreateAd
UpdatedAt
```

### Regras

- Permite que um serviço de loja seja executado por vários profissionais.
- Permite que cada profissional tenha preço ou duração específica para o mesmo serviço.
- Para profissional autônomo, também pode ser usado para indicar seus serviços ativos.

## HorarioFuncionamentoEstabelecimento

Representa os dias e horários em que o estabelecimento funciona.

### Campos principais

```txt
Id
EstabelecimentoId
DiaSemana
HoraInicio
HoraFim
Ativo
CreateAd
UpdatedAt
```

### Regras

- Um estabelecimento pode possuir vários horários de funcionamento.
- O agendamento dentro de uma loja deve respeitar o horário de funcionamento do estabelecimento.
- Mesmo que o profissional esteja disponível, a loja precisa estar aberta.

## HorarioAtendimentoProfissional

Representa os dias e horários em que o profissional atende.

### Campos principais

```txt
Id
ProfissionalId
EstabelecimentoId
DiaSemana
HoraInicio
HoraFim
Ativo
CreateAd
UpdatedAt
```

### Regras

- Para profissional autônomo, `EstabelecimentoId` fica vazio.
- Para profissional vinculado a loja, `EstabelecimentoId` indica onde aquele horário vale.
- O agendamento deve respeitar o horário do profissional.
- O agendamento dentro de loja deve respeitar tanto o horário do profissional quanto o horário do estabelecimento.

## Endereco

Representa o endereço físico de um estabelecimento ou profissional autônomo.

### Campos principais

```txt
Id
EstabelecimentoId
ProfissionalAutonomoId
Cep
Logradouro
Numero
Complemento
Bairro
Cidade
Estado
CreateAd
UpdatedAt
```

### Regras

- Estabelecimento possui endereço.
- Profissional autônomo pode possuir endereço.
- Cliente final usa localização atual, não necessariamente endereço cadastrado.

## Agendamento

Representa uma reserva feita por um usuário cliente.

O agendamento pode conter um ou mais itens de serviço.

### Campos principais

```txt
Id
UsuarioClienteId
EstabelecimentoId
ProfissionalAutonomoId
Status
ValorTotal
Observacao
CreateAd
UpdatedAt
CanceladoEm
```

### Status sugeridos

```txt
PendentePagamento
Confirmado
EmAtendimento
Concluido
Cancelado
Expirado
Reembolsado
```

### Relacionamentos

```txt
Agendamento N:1 Usuario
Agendamento N:1 Estabelecimento, quando agendamento de loja
Agendamento N:1 Profissional, quando agendamento de autônomo
Agendamento 1:N AgendamentoItem
Agendamento 1:N Pagamento
```

### Regras

- Agendamento de loja deve possuir `EstabelecimentoId`.
- Agendamento de profissional autônomo deve possuir `ProfissionalAutonomoId`.
- Um agendamento pode conter serviços com profissionais diferentes dentro da mesma loja.
- Um agendamento só pode ser confirmado após regra de disponibilidade e pagamento.
- O profissional vinculado à loja só deve visualizar itens atribuídos a ele.

## AgendamentoItem

Representa um serviço específico dentro de um agendamento.

Permite agendar múltiplos serviços e múltiplos profissionais no mesmo agendamento.

### Campos principais

```txt
Id
AgendamentoId
ServicoId
ProfissionalId
Inicio
Fim
Valor
Status
RepassadoDeProfissionalId
CreateAd
UpdatedAt
```

### Regras

- Cada item aponta para um serviço e um profissional.
- Itens diferentes podem ter profissionais diferentes.
- Um item pode ser repassado para outro profissional.
- O horário do item deve respeitar a disponibilidade do profissional.
- Em loja, o horário do item também deve respeitar o horário de funcionamento do estabelecimento.

## Caixa

Representa a posição financeira interna de um estabelecimento ou profissional autônomo.

### Campos principais

```txt
Id
EstabelecimentoId
ProfissionalAutonomoId
SaldoTotal
SaldoDisponivel
SaldoRetido
CreateAd
UpdatedAt
```

### Regras

- Loja possui caixa próprio.
- Profissional autônomo possui caixa próprio.
- Profissional vinculado a loja não possui caixa geral próprio dentro da loja, mas pode ter comissão e lançamentos associados.
- O saldo disponível deve considerar retenções, chargebacks, pagamentos pendentes e regras de saque.

## LancamentoCaixa

Representa cada movimentação financeira do caixa.

### Campos principais

```txt
Id
CaixaId
AgendamentoId
PagamentoId
ProfissionalId
Tipo
Valor
Descricao
CreateAd
```

### Tipos sugeridos

```txt
EntradaAgendamento
ComissaoProfissional
TaxaPlataforma
Assinatura
Saque
Estorno
Chargeback
AjusteManual
```

### Regras

- Todo valor financeiro relevante deve gerar lançamento.
- Pagamento confirmado deve gerar entrada no caixa.
- Comissões de profissionais vinculados devem ser rastreáveis.
- Estornos e chargebacks devem impactar saldo.

## ComissaoProfissional

Representa a regra de comissão de um profissional dentro de um estabelecimento.

### Campos principais

```txt
Id
ProfissionalEstabelecimentoId
TipoComissao
Percentual
ValorFixo
Ativo
InicioVigencia
FimVigencia
CreateAd
UpdatedAt
```

### Tipos sugeridos

```txt
Percentual
ValorFixo
Mista
```

### Regras

- Comissão é configurada por vínculo profissional-estabelecimento.
- Pode haver histórico de comissão por vigência.
- A comissão deve ser considerada nos lançamentos do caixa.

## MetaProfissional

Representa metas de atendimento ou faturamento para profissionais vinculados a estabelecimento.

### Campos principais

```txt
Id
ProfissionalEstabelecimentoId
TipoMeta
QuantidadeAtendimentos
ValorFaturamento
InicioPeriodo
FimPeriodo
Status
CreateAd
UpdatedAt
```

### Tipos sugeridos

```txt
Atendimentos
Faturamento
Mista
```

### Regras

- Meta pode ser por número de atendimentos.
- Meta pode ser por valor financeiro gerado.
- O cálculo deve considerar agendamentos concluídos e pagos.

## Plano

Representa os planos disponíveis na plataforma.

### Campos principais

```txt
Id
Nome
Descricao
Preco
Periodo
LimiteProfissionais
LimiteServicos
LimiteAgendamentos
Ativo
CreateAd
UpdatedAt
```

### Regras

- Plano gratuito será limitado.
- Plano pago será controlado por assinatura.
- Limites devem ser usados para restringir funcionalidades.

## Assinatura

Representa a assinatura de um estabelecimento ou profissional autônomo.

A assinatura será cobrada por gateway externo, tanto para estabelecimentos quanto para profissionais autônomos.

Os gateways previstos inicialmente para assinatura são:

```txt
MercadoPago
AbacatePay
```

### Campos principais

```txt
Id
PlanoId
EstabelecimentoId
ProfissionalAutonomoId
Status
Inicio
Fim
RenovacaoAutomatica
Gateway
GatewaySubscriptionId
GatewayCustomerId
UltimoPagamentoId
CreateAd
UpdatedAt
CanceladoEm
```

### Status sugeridos

```txt
Ativa
PendentePagamento
Cancelada
Expirada
Suspensa
Trial
```

### Regras

- Assinatura pode pertencer a estabelecimento.
- Assinatura pode pertencer a profissional autônomo.
- Uma assinatura não deve pertencer aos dois ao mesmo tempo.
- Assinatura define limites e recursos disponíveis.
- Assinatura deve ser criada, renovada, suspensa ou cancelada de acordo com eventos do gateway.
- A cobrança da assinatura deve gerar um pagamento vinculado a `AssinaturaId`.
- O status da assinatura deve ser atualizado por webhook do gateway.
- `GatewaySubscriptionId` deve armazenar o identificador da assinatura no gateway.
- `GatewayCustomerId` deve armazenar o identificador do cliente/assinante no gateway, quando existir.
- `UltimoPagamentoId` pode apontar para o pagamento mais recente da assinatura.

## Pagamento

Representa um pagamento processado por gateway externo.

Os gateways previstos inicialmente são:

```txt
MercadoPago
AbacatePay
```

### Campos principais

```txt
Id
AgendamentoId
AssinaturaId
Gateway
GatewayPaymentId
MetodoPagamento
Status
Valor
Moeda
PagoEm
ExpiraEm
CreateAd
UpdatedAt
```

### Status sugeridos

```txt
Pendente
Autorizado
Pago
Recusado
Cancelado
Estornado
Chargeback
Expirado
```

### Regras

- Pagamento pode estar ligado a agendamento.
- Pagamento pode estar ligado a assinatura.
- Pagamento de assinatura deve representar cobranças do plano contratado por estabelecimento ou profissional autônomo.
- Pagamento confirmado deve atualizar status do agendamento ou assinatura.
- Pagamento confirmado deve gerar lançamentos financeiros.
- Alterações de status devem poder ser recebidas por webhook.

## WebhookPagamento

Representa eventos recebidos do gateway externo.

Os webhooks previstos inicialmente devem suportar eventos de:

```txt
MercadoPago
AbacatePay
```

### Campos principais

```txt
Id
Gateway
EventId
EventType
Payload
Processado
ProcessadoEm
ErroProcessamento
CreateAd
```

### Regras

- Todo webhook recebido deve ser registrado.
- Webhook deve ser idempotente.
- Eventos duplicados não devem gerar lançamento financeiro duplicado.
- Erros de processamento devem ser rastreáveis.

## Relacionamentos Principais

```txt
Usuario 1:1 Profissional
Usuario 1:N EstabelecimentoUsuario
Usuario 1:N Agendamento

Estabelecimento 1:N EstabelecimentoUsuario
Estabelecimento 1:N ProfissionalEstabelecimento
Estabelecimento 1:N Servico
Estabelecimento 1:N Agendamento
Estabelecimento 1:1 Caixa
Estabelecimento 1:1 Endereco

Profissional 1:N ProfissionalEstabelecimento
Profissional 1:N ProfissionalServico
Profissional 1:N HorarioAtendimentoProfissional
Profissional 1:N AgendamentoItem
Profissional 1:1 Caixa, quando autônomo
Profissional 1:1 Endereco, quando autônomo

Servico 1:N ProfissionalServico
Servico 1:N AgendamentoItem

Agendamento 1:N AgendamentoItem
Agendamento 1:N Pagamento

Caixa 1:N LancamentoCaixa
Plano 1:N Assinatura
Assinatura 1:N Pagamento
```

## Regras Críticas Para Implementação

### Papel único por usuário

Um usuário deve possuir apenas uma role global.

Troca de papel, caso exista futuramente, deve ser tratada como fluxo explícito e auditável.

### Profissional autônomo

Profissional autônomo deve ter:

```txt
Usuario
Profissional
Endereco
HorarioAtendimentoProfissional
Servico
Caixa
Assinatura
```

Ele não depende de estabelecimento.

### Profissional vinculado a estabelecimento

Profissional vinculado deve ter:

```txt
Usuario
Profissional
ProfissionalEstabelecimento
HorarioAtendimentoProfissional
ProfissionalServico
```

Ele só visualiza agendamentos atribuídos a ele.

### Estabelecimento

Estabelecimento deve ter:

```txt
Usuarios responsáveis
Profissionais vinculados
Serviços
Horários de funcionamento
Endereço
Caixa
Assinatura
```

### Agendamento com múltiplos serviços

Um agendamento pode ter múltiplos itens.

Cada item pode ter:

```txt
Servico
Profissional
Inicio
Fim
Valor
Status
```

Isso permite que um cliente agende serviços diferentes com profissionais diferentes dentro da mesma loja.

### Financeiro

Pagamento externo não deve ser confundido com caixa interno.

```txt
Pagamento = transação com gateway
Caixa = saldo interno
LancamentoCaixa = movimentação financeira rastreável
```

### Webhook

Webhooks devem ser registrados antes do processamento.

O processamento deve ser idempotente para evitar duplicidade em pagamentos e lançamentos.

## Pontos Ainda Em Aberto

- Um profissional pode estar vinculado a mais de um estabelecimento ao mesmo tempo?
- Cliente poderá pagar no local ou sempre pela plataforma?
- Haverá saque para profissionais vinculados à loja ou apenas repasse interno registrado no caixa da loja?
- A comissão do profissional será calculada no momento do pagamento, da conclusão do atendimento ou do fechamento de caixa?
- O agendamento poderá ser criado sem pagamento confirmado?
- O estabelecimento poderá alterar o profissional de um item após o pagamento?
- Profissional autônomo poderá ter equipe no futuro ou sempre será operação individual?


