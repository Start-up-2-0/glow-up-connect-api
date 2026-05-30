# Tasks - Profissionais, equipe e acesso dentro do negocio

Este documento organiza as tasks relacionadas ao cadastro de profissionais, colaboradores e usuarios dentro do negocio, gerenciamento de equipe e controle de acesso por nivel de permissao.

## Diretriz de modelo comercial unificado

Internamente, o sistema deve tratar profissional autonomo e estabelecimento como o mesmo conceito comercial: um negocio/tenant.

A diferenca entre um autonomo e um estabelecimento e apenas operacional:

- um autonomo pode comecar sozinho;
- o mesmo negocio pode crescer e receber novos profissionais, recepcionistas, gestores ou administradores;
- agendamentos, clientes, servicos, horarios, financeiro e permissoes continuam vinculados ao mesmo negocio;
- nao deve existir migracao de dados quando um autonomo evoluir para uma equipe;
- planos e modulos dependem apenas do plano contratado, nao do tipo "autonomo" ou "estabelecimento".

Por compatibilidade com o codigo atual, a entidade principal continua sendo `Estabelecimento`, mas ela deve ser entendida como tenant comercial do negocio.

## Epic

Implementar gestao de equipe do negocio com vinculo de profissionais, recepcionistas e usuarios administrativos, garantindo que cada pessoa veja e execute apenas o que sua funcao permite.

## Objetivo do fluxo

Permitir que um negocio ativo cadastre e gerencie sua equipe:

```text
Dono/Admin do negocio
  -> convida ou cadastra usuario da equipe
  -> define nivel de acesso
  -> vincula profissional quando a pessoa atende clientes
  -> configura servicos e agenda do profissional
  -> sistema aplica permissoes em agenda, caixa, clientes e operacao
```

## Conceitos

### Usuario do negocio

Representado por `EstabelecimentoUsuario`.

Define que um usuario possui acesso administrativo ou operacional ao negocio, com uma role interna:

- `Owner`
- `Admin`
- `Manager`
- `Receptionist`
- `Profissional`

### Profissional do negocio

Representado por `ProfissionalEstabelecimento`.

Define que um perfil profissional atende dentro do negocio. Um profissional pode ter agenda, servicos, comissao, metas e agendamentos atribuidos.

### Diferenca importante

Nem todo usuario do negocio e profissional, e nem todo profissional deve ter acesso administrativo ao negocio.

Exemplos:

- Recepcionista tem acesso ao negocio, mas nao atende clientes.
- Profissional atende clientes, mas nao deve acessar caixa completo, equipe, permissoes ou dados administrativos.
- Admin pode gerenciar equipe e agenda, mas nao necessariamente atende clientes.
- Owner tem acesso total.

### Regra de acesso `Profissional`

A regra de acesso `Profissional` representa o usuario que atua atendendo clientes dentro do negocio.

Ela deve ser aplicada quando o usuario tiver vinculo ativo com um `Profissional` tambem vinculado ao negocio por `ProfissionalEstabelecimento`.

Por padrao, o profissional:

- ve apenas a propria agenda;
- ve apenas clientes vinculados aos seus agendamentos;
- pode iniciar/finalizar apenas atendimentos atribuidos a ele;
- pode visualizar seus servicos e horarios dentro da loja;
- pode visualizar sua propria comissao, quando o modulo existir;
- nao acessa caixa/faturamento geral;
- nao gerencia equipe, permissoes ou roles;
- nao edita dados cadastrais do negocio;
- nao ve agenda completa da loja, salvo permissao administrativa adicional.

Se a mesma pessoa tambem tiver uma role administrativa em `EstabelecimentoUsuario`, as permissoes efetivas devem ser a uniao segura dos acessos concedidos, respeitando bloqueios explicitos de dados sensiveis quando a regra de negocio exigir.

## Matriz inicial de acesso

| Funcionalidade | Owner | Admin | Manager | Receptionist | Profissional |
|---|---:|---:|---:|---:|---:|
| Ver dados do negocio | Sim | Sim | Sim | Sim | Parcial |
| Editar dados do negocio | Sim | Sim | Nao | Nao | Nao |
| Gerenciar usuarios e permissoes | Sim | Sim | Nao | Nao | Nao |
| Convidar profissional | Sim | Sim | Sim | Nao | Nao |
| Configurar servicos da loja | Sim | Sim | Sim | Nao | Nao |
| Ver agenda geral | Sim | Sim | Sim | Sim | Nao |
| Criar/reagendar agendamento | Sim | Sim | Sim | Sim | Apenas os seus, se permitido |
| Atender cliente agendado | Nao obrigatorio | Nao obrigatorio | Nao obrigatorio | Nao | Apenas os seus |
| Ver caixa/faturamento geral | Sim | Sim | Opcional | Nao | Nao |
| Ver comissao propria | Nao obrigatorio | Nao obrigatorio | Nao obrigatorio | Nao | Sim |
| Cancelar agendamento | Sim | Sim | Sim | Sim | Apenas os seus, se permitido |
| Ver clientes do negocio | Sim | Sim | Sim | Sim | Apenas clientes dos seus agendamentos |

Observacao: `Manager` com acesso a caixa deve ser uma permissao explicita, nao comportamento padrao.

## Permissoes sugeridas

Criar uma camada de permissoes mais granular do que a role, para permitir evolucao sem quebrar o modelo.

Permissoes iniciais:

```text
Negocio.Visualizar
Negocio.Editar
Equipe.Visualizar
Equipe.Gerenciar
Profissional.Convidar
Profissional.Gerenciar
Servico.Visualizar
Servico.Gerenciar
Agenda.VisualizarGeral
Agenda.VisualizarPropria
Agenda.Criar
Agenda.Reagendar
Agenda.Cancelar
Atendimento.VisualizarProprio
Atendimento.Iniciar
Atendimento.Finalizar
Cliente.VisualizarGeral
Cliente.VisualizarProprio
Caixa.Visualizar
Caixa.Gerenciar
Comissao.VisualizarPropria
```

Observacao tecnica: se o codigo mantiver nomes como `Estabelecimento.Visualizar` por compatibilidade, a semantica deve ser de `Negocio.Visualizar`.

## Task 1 - Definir matriz de permissoes por role interna

### Descricao

Formalizar quais permissoes cada role interna do negocio possui por padrao, incluindo `Owner`, `Admin`, `Manager`, `Receptionist` e profissional vinculado.

### Criterios de aceite

- Definir permissoes padrao para cada role.
- Definir quais permissoes nunca devem ser concedidas a certas roles, como caixa para recepcionista e profissional.
- Separar role administrativa de vinculo profissional.
- Documentar comportamento esperado para acesso a agenda, clientes, caixa e atendimento.
- Garantir que a matriz nao diferencie autonomo e estabelecimento; apenas considera o negocio e o plano contratado.
- Incluir testes unitarios da matriz de permissoes.

## Task 2 - Criar servico de autorizacao do negocio

### Descricao

Criar um servico de aplicacao responsavel por verificar se o usuario autenticado possui acesso ao negocio e se possui uma permissao especifica.

### Criterios de aceite

- Verificar vinculo ativo em `EstabelecimentoUsuario`.
- Verificar role interna do usuario no negocio.
- Verificar permissoes derivadas da role.
- Suportar verificacao por `EstabelecimentoId` e, quando necessario, por `PublicGuid`.
- Retornar erro claro quando o usuario nao pertence ao negocio.
- Retornar erro claro quando o usuario pertence ao negocio, mas nao tem permissao.
- Incluir testes para usuario sem vinculo, vinculo inativo e acesso permitido.

## Task 3 - Proteger endpoints por permissao

### Descricao

Evoluir o `PermissionMiddleware` ou criar mecanismo equivalente para proteger endpoints conforme permissao exigida por modulo.

### Criterios de aceite

- Permitir declarar permissao exigida por endpoint.
- Validar usuario autenticado via `x-glow-token`.
- Validar contexto do negocio.
- Bloquear acesso sem permissao.
- Manter controllers finos.
- Retornar resposta no padrao `ApiErrorResponse`, quando aplicavel.
- Incluir testes de autorizacao.

## Task 4 - Cadastro de usuario da equipe do negocio

### Descricao

Criar fluxo para adicionar um usuario ao negocio com uma role interna administrativa ou operacional.

### Criterios de aceite

- Permitir adicionar usuario existente por e-mail ou telefone.
- Permitir convidar usuario ainda nao cadastrado, se fizer parte do escopo.
- Definir role interna inicial.
- Criar vinculo em `EstabelecimentoUsuario`.
- Impedir duplicidade de vinculo ativo para o mesmo usuario e negocio.
- Apenas `Owner` ou `Admin` podem gerenciar usuarios.
- Validar limites do plano contratado, quando aplicavel.
- Incluir testes de sucesso, duplicidade e permissao negada.

## Task 5 - Convite de profissional para o negocio

### Descricao

Criar fluxo para vincular um profissional ao negocio, criando `ProfissionalEstabelecimento` e permitindo que ele atenda clientes.

### Criterios de aceite

- Permitir vincular profissional existente.
- Permitir criar perfil profissional para usuario existente, se ainda nao existir.
- Criar vinculo em `ProfissionalEstabelecimento`.
- Definir `PodeReceberAgendamento`.
- Impedir vinculo duplicado ativo para o mesmo profissional e negocio.
- Apenas roles autorizadas podem convidar profissional.
- Validar limites do plano contratado, quando aplicavel.
- Incluir testes de sucesso, duplicidade e permissao negada.

## Task 6 - Configuracao de acesso do profissional

### Descricao

Definir quais acessos um profissional vinculado possui dentro do negocio, garantindo que ele veja apenas sua propria operacao.

### Criterios de aceite

- Profissional nao pode acessar caixa/faturamento geral.
- Profissional nao pode gerenciar equipe.
- Profissional nao pode editar dados do negocio.
- Profissional visualiza apenas seus proprios agendamentos.
- Profissional atende apenas clientes agendados com ele.
- Profissional visualiza apenas clientes relacionados aos seus agendamentos.
- Profissional pode visualizar comissao propria, quando existir.
- Incluir testes de escopo por profissional.

## Task 7 - Agenda geral do negocio

### Descricao

Criar endpoints/casos de uso para visualizacao da agenda geral do negocio por usuarios autorizados, como owner, admin, manager e recepcionista.

### Criterios de aceite

- Listar agendamentos do negocio.
- Permitir filtro por profissional, data, status e cliente.
- Bloquear acesso de profissional comum a agenda geral.
- Recepcionista pode visualizar agenda geral.
- Manager pode visualizar agenda geral.
- Incluir testes de filtro e permissao.

## Task 8 - Agenda propria do profissional

### Descricao

Criar fluxo para o profissional visualizar e gerenciar apenas os agendamentos atribuidos a ele.

### Criterios de aceite

- Listar apenas agendamentos em que o profissional e responsavel pelo item.
- Nao retornar agendamentos de outros profissionais.
- Permitir visualizar detalhes necessarios para atendimento.
- Restringir dados financeiros sensiveis.
- Permitir mudanca de status de atendimento, quando autorizado.
- Incluir testes garantindo isolamento entre profissionais.

## Task 9 - Atendimento do cliente pelo profissional

### Descricao

Criar fluxo para profissional iniciar, acompanhar e finalizar atendimento de clientes agendados com ele.

### Criterios de aceite

- Profissional so pode iniciar atendimento de agendamento atribuido a ele.
- Profissional nao pode atender cliente atribuido a outro profissional.
- Validar status do agendamento antes de iniciar/finalizar.
- Registrar datas ou status relevantes do atendimento.
- Permitir que owner/admin acompanhem o status.
- Incluir testes dos fluxos permitido e negado.

## Task 10 - Papel da recepcionista

### Descricao

Definir e implementar permissoes da recepcionista no negocio.

### Criterios de aceite

- Recepcionista pode visualizar agenda geral.
- Recepcionista pode criar agendamento para cliente.
- Recepcionista pode reagendar ou cancelar agendamento, conforme regra definida.
- Recepcionista nao pode acessar caixa/faturamento.
- Recepcionista nao pode gerenciar equipe/permissoes.
- Recepcionista nao pode alterar dados sensiveis do negocio.
- Incluir testes especificos para recepcionista.

## Task 11 - Controle de acesso ao caixa/faturamento

### Descricao

Proteger endpoints de caixa, faturamento, lancamentos e saldo para que apenas roles autorizadas acessem informacoes financeiras do negocio.

### Criterios de aceite

- Owner pode acessar caixa completo.
- Admin pode acessar caixa completo, se definido pela matriz.
- Manager so acessa caixa se houver permissao explicita.
- Recepcionista nao acessa caixa.
- Profissional nao acessa caixa geral.
- Profissional pode acessar apenas comissao propria, se modulo existir.
- Incluir testes para cada role.

## Task 12 - Gerenciamento de servicos executados por profissional

### Descricao

Criar fluxo para associar servicos do negocio aos profissionais que podem executa-los, usando `ProfissionalServico`.

### Criterios de aceite

- Permitir vincular servico da loja a um profissional.
- Permitir preco/duracao especificos por profissional, quando aplicavel.
- Impedir profissional executar servico nao vinculado.
- Validar limite de servicos/profissionais conforme assinatura.
- Apenas roles autorizadas podem gerenciar vinculos.
- Incluir testes de vinculo e restricao.

## Task 13 - Horarios de atendimento do profissional na loja

### Descricao

Criar fluxo para configurar horarios em que um profissional atende dentro do negocio.

### Criterios de aceite

- Criar horarios por profissional e negocio.
- Validar que o horario do profissional respeita funcionamento da loja.
- Permitir ativar/inativar horarios.
- Impedir conflito de horarios, quando aplicavel.
- Apenas roles autorizadas podem gerenciar horarios.
- Incluir testes de validacao.

## Task 14 - Alteracao de role e desativacao de acesso

### Descricao

Criar fluxo para alterar role interna de um usuario do negocio e desativar acessos.

### Criterios de aceite

- Permitir alterar role de usuario vinculado.
- Impedir que o ultimo `Owner` ativo seja removido ou rebaixado.
- Permitir inativar vinculo de usuario.
- Permitir inativar vinculo de profissional.
- Ao inativar profissional, impedir novos agendamentos com ele.
- Manter historico minimo por `UpdatedAt` e status ativo/inativo.
- Incluir testes de ultimo owner e inativacao.

## Task 15 - Auditoria de acoes sensiveis

### Descricao

Registrar acoes sensiveis realizadas dentro do negocio, especialmente alteracoes de permissao, equipe, caixa e agenda.

### Criterios de aceite

- Registrar usuario que executou a acao.
- Registrar negocio afetado.
- Registrar tipo da acao.
- Registrar data/hora.
- Registrar payload resumido sem dados sensiveis.
- Auditar mudanca de role, convite, remocao de acesso, cancelamento de agenda e acesso financeiro.
- Incluir testes de registro de auditoria quando houver acao sensivel.

## Task 16 - Notificacoes de convite e alteracao de acesso

### Descricao

Criar notificacoes assincronas para convites e alteracoes relevantes de acesso usando a mensageria existente.

### Criterios de aceite

- Enfileirar notificacao ao convidar usuario da equipe.
- Enfileirar notificacao ao convidar profissional.
- Enfileirar notificacao quando role/permissao for alterada.
- Enfileirar notificacao quando acesso for removido.
- Usar `IMensagemNotificacaoService`.
- Nao chamar provedor diretamente.
- Incluir testes validando enfileiramento.

## Task 17 - Testes integrados de acesso por perfil

### Descricao

Criar testes integrados cobrindo os principais fluxos de acesso dentro do negocio.

### Criterios de aceite

- Testar owner acessando todos os modulos do negocio.
- Testar admin gerenciando equipe.
- Testar recepcionista criando e visualizando agendamentos sem acessar caixa.
- Testar profissional vendo apenas propria agenda.
- Testar profissional impedido de acessar caixa geral.
- Testar usuario sem vinculo impedido de acessar negocio.
- Testar usuario com vinculo inativo impedido de acessar negocio.
