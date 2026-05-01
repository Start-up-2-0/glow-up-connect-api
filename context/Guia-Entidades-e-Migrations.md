# Guia - Entidades e Migrations

Este guia descreve como aplicar a modelagem do arquivo `context/Modelagem-v1.md` no projeto.

O objetivo é criar as entidades aos poucos, com uma migration para cada entidade criada.

## Regra Principal

Cada entidade deve seguir este ciclo:

```txt
1. Criar entidade no Domain
2. Criar enums necessários
3. Registrar DbSet no ApplicationDbContext
4. Criar configuração Fluent API
5. Rodar build
6. Gerar migration da entidade
7. Aplicar migration no banco
8. Validar tabela criada
```

## Estrutura de Pastas Recomendada

```txt
src/
  GLOWAPI.Domain/
    Entities/
    Enums/
    Common/

  GLOWAPI.Infrastructure/
    ApplicationDbContext.cs
    Configurations/
    Data/
      Migrations/
```

## Ciclo Por Entidade

### 1. Criar entidade

Criar a entidade em:

```txt
src/GLOWAPI.Domain/Entities
```

Exemplo:

```txt
src/GLOWAPI.Domain/Entities/Usuario.cs
```

### 2. Criar enums

Quando a entidade precisar de enum, criar em:

```txt
src/GLOWAPI.Domain/Enums
```

Exemplo:

```txt
src/GLOWAPI.Domain/Enums/UserRole.cs
```

### 3. Registrar DbSet

Registrar a entidade no arquivo:

```txt
src/GLOWAPI.Infrastructure/ApplicationDbContext.cs
```

Exemplo:

```csharp
public DbSet<Usuario> Usuarios { get; set; }
```

### 4. Criar configuração Fluent API

Criar a configuração em:

```txt
src/GLOWAPI.Infrastructure/Configurations
```

Exemplo:

```txt
src/GLOWAPI.Infrastructure/Configurations/UsuarioConfiguration.cs
```

Essa configuração deve conter:

```txt
Nome da tabela
Chave primária
Campos obrigatórios
Tamanho máximo de strings
Índices
Relacionamentos
Restrições únicas
Conversões de enum, quando necessário
```

### 5. Rodar build

Antes de criar migration, validar se o projeto compila:

```powershell
dotnet build GLOWAPI.sln
```

### 6. Criar migration

Criar uma migration para a entidade recém-criada:

```powershell
dotnet ef migrations add CreateUsuarios `
  --project src/GLOWAPI.Infrastructure `
  --startup-project src/GLOWAPI.API `
  --output-dir Data/Migrations
```

### 7. Aplicar migration

Aplicar no banco configurado:

```powershell
dotnet ef database update `
  --project src/GLOWAPI.Infrastructure `
  --startup-project src/GLOWAPI.API
```

## Ordem Recomendada de Implementação

A ordem importa porque algumas entidades dependem de outras.

```txt
01 - Usuario
02 - Estabelecimento
03 - EstabelecimentoUsuario
04 - Profissional
05 - ProfissionalEstabelecimento
06 - Endereco
07 - Servico
08 - ProfissionalServico
09 - HorarioFuncionamentoEstabelecimento
10 - HorarioAtendimentoProfissional
11 - Agendamento
12 - AgendamentoItem
13 - Caixa
14 - Plano
15 - Assinatura
16 - Pagamento
17 - LancamentoCaixa
18 - WebhookPagamento
19 - ComissaoProfissional
20 - MetaProfissional
```

## Nomes Sugeridos de Migrations

```txt
CreateUsuarios
CreateEstabelecimentos
CreateEstabelecimentoUsuarios
CreateProfissionais
CreateProfissionalEstabelecimentos
CreateEnderecos
CreateServicos
CreateProfissionalServicos
CreateHorariosFuncionamentoEstabelecimento
CreateHorariosAtendimentoProfissional
CreateAgendamentos
CreateAgendamentoItens
CreateCaixas
CreatePlanos
CreateAssinaturas
CreatePagamentos
CreateLancamentosCaixa
CreateWebhookPagamentos
CreateComissoesProfissional
CreateMetasProfissional
```

## Primeira Entidade: Usuario

Campos previstos na modelagem:

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

Enums necessários:

```txt
UserRole
```

Valores sugeridos:

```txt
Cliente
DonoEstabelecimento
ProfissionalAutonomo
ProfissionalEstabelecimento
Admin
```

Configurações importantes:

```txt
Email obrigatório
Email único
Nome obrigatório
Senha obrigatória
Role obrigatório
Tentivas com valor padrão 0
Ativo com valor padrão true
CreateAd obrigatório
UpdatedAt opcional ou obrigatório, conforme regra escolhida
```

## Observações Sobre Datas

A modelagem usa:

```txt
CreateAd
UpdatedAt
```

Observação: `CreateAd` parece representar `CreatedAt`, mas deve ser mantido como `CreateAd` enquanto essa for a decisão oficial da modelagem.

## Observações Sobre PublicGuid

As entidades `Estabelecimento` e `Profissional` devem possuir:

```txt
PublicGuid
```

Regras:

```txt
Deve ser Guid
Deve ser único
Deve ser gerado automaticamente
Não deve expor o Id interno
Será usado para links públicos de agendamento
```

## Checklist Antes de Gerar Migration

Antes de rodar `dotnet ef migrations add`, validar:

```txt
Entidade criada no Domain
Enum criado, se necessário
DbSet registrado
Configuration criada
Relacionamentos configurados
Índices únicos configurados
Build passando
Nome da migration revisado
```

## Checklist Depois de Gerar Migration

Depois de gerar a migration, validar:

```txt
Tabela com nome correto
Colunas com nomes corretos
Tipos corretos para PostgreSQL
Campos obrigatórios corretos
Índices criados
Foreign keys criadas, se houver
Migration aplicada no banco
```

## Comando Base Para Todas as Migrations

Trocar apenas o nome da migration:

```powershell
dotnet ef migrations add NomeDaMigration `
  --project src/GLOWAPI.Infrastructure `
  --startup-project src/GLOWAPI.API `
  --output-dir Data/Migrations
```

## Comando Base Para Atualizar Banco

```powershell
dotnet ef database update `
  --project src/GLOWAPI.Infrastructure `
  --startup-project src/GLOWAPI.API
```
