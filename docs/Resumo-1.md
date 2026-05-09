# Resumo das Alterações e Aprendizados no Projeto GLOWAPI

Data: 08/05/2026

Este resumo registra, de forma simples, o que foi feito no projeto GLOWAPI durante a implementação inicial da CRUD de usuários, o motivo de cada alteração e o contexto dos principais problemas encontrados.

## Contexto do projeto

O projeto é uma API em .NET 8 usando Entity Framework Core, PostgreSQL e Swagger.

A primeira funcionalidade trabalhada foi a CRUD de usuários, com os seguintes objetivos:

- criação de conta;
- leitura dos dados do usuário para tela de perfil;
- edição de dados cadastrais;
- desativação da conta usando método `DELETE`;
- uso de práticas básicas de segurança, como hash de senha e preparação para JWT.

## O que foi alterado antes dos testes finais

### 1. Entidade Usuario

**O que foi alterado:**  
A entidade `Usuario` foi ajustada para usar nomes mais corretos e seguros:

- `Senha` foi alterado para `SenhaHash`;
- `Tentivas` foi corrigido para `Tentativas`;
- `CreateAd` foi corrigido para `CreatedAt`.

**Por quê:**  
Para evitar salvar senha em texto puro, corrigir erros de digitação e deixar o modelo mais claro.

## 2. Configuração do Usuario no Entity Framework

**O que foi alterado:**  
O arquivo `UsuarioConfiguration.cs` foi ajustado para mapear os novos nomes da entidade:

- `SenhaHash`;
- `Tentativas`;
- `CreatedAt`.

Também foram mantidas regras como:

- `Nome` obrigatório;
- `Email` obrigatório e único;
- tamanho máximo para campos;
- `Ativo` com valor padrão `true`;
- conversão do enum `UserRole`.

**Por quê:**  
Porque o Entity Framework usa essa configuração para criar e consultar corretamente as colunas no banco.

## 3. Pacotes de segurança

**O que foi alterado:**  
Foram adicionados pacotes para segurança:

- `BCrypt.Net-Next` no projeto `GLOWAPI.Application`;
- `Microsoft.AspNetCore.Authentication.JwtBearer` no projeto `GLOWAPI.API`.

**Por quê:**  
O BCrypt permite salvar senha como hash.  
O JWT prepara a API para autenticação com token em endpoints protegidos.

## 4. Interface IUsuarioService

**O que foi alterado:**  
Foi criada a interface `IUsuarioService` com métodos para:

- criar usuário;
- buscar usuário por ID;
- buscar usuário por e-mail;
- atualizar usuário;
- desativar usuário;
- verificar senha.

**Por quê:**  
Para definir um contrato claro da regra de negócio de usuários e separar a Controller da lógica interna.

## 5. Serviço UsuarioService

**O que foi alterado:**  
Foi criado o `UsuarioService`, responsável pela regra de negócio da CRUD.

Ele faz:

- verificação de e-mail duplicado;
- geração de hash da senha com BCrypt;
- criação de usuário;
- busca de usuário;
- atualização de nome e telefone;
- desativação lógica da conta;
- atualização do campo `UpdatedAt`.

**Por quê:**  
Para concentrar a lógica de negócio fora da Controller e manter o código mais organizado.

## 6. Ajuste dos métodos do repositório

**O que foi alterado:**  
Durante a criação do serviço, foi necessário ajustar os nomes dos métodos usados no repositório.

O código inicialmente tentou usar nomes como:

- `GetByIdAsync`;
- `AddAsync`;
- `SaveChangesAsync`;
- `Update`.

Mas o projeto já usava nomes em português:

- `ObterPorIdAsync`;
- `AdicionarAsync`;
- `SalvarAlteracoesAsync`;
- `Atualizar`.

**Por quê:**  
Para o `UsuarioService` ficar compatível com as interfaces e repositórios que já existiam no projeto.

## 7. Dependency Injection da Application

**O que foi alterado:**  
Foi criado/ajustado o arquivo:

```txt
src/GLOWAPI.Application/DependencyInjection.cs
```

Nele foi registrado:

```csharp
services.AddScoped<IUsuarioService, UsuarioService>();
```

Também foi necessário adicionar o pacote:

```txt
Microsoft.Extensions.DependencyInjection
```

**Por quê:**  
Para permitir que o .NET injete `UsuarioService` automaticamente nas Controllers.

## 8. Program.cs

**O que foi alterado:**  
O `Program.cs` foi ajustado para registrar:

- Controllers;
- Infrastructure;
- Application;
- Swagger;
- autenticação JWT;
- autorização.

Também foi corrigido o warning da chave JWT usando uma validação:

```csharp
var jwtKey = builder.Configuration["Jwt:Key"] 
    ?? throw new InvalidOperationException("Jwt:Key não configurado.");
```

**Por quê:**  
Para conectar as camadas do projeto, habilitar o Swagger e preparar os endpoints protegidos com `[Authorize]`.

## 9. UsuarioController

**O que foi alterado:**  
Foi criada a `UsuarioController` com os endpoints:

- `POST api/Usuario` para criar conta;
- `GET api/Usuario/{id}` para buscar dados do usuário;
- `PUT api/Usuario/{id}` para editar dados;
- `DELETE api/Usuario/{id}` para desativar conta.

**Por quê:**  
Para expor a CRUD de usuários pela API e permitir testes pelo Swagger.

## 10. Swagger

**O que foi alterado:**  
O Swagger foi configurado e usado para testar a API.

**Por quê:**  
Para visualizar os endpoints no navegador e testar as rotas sem precisar usar Postman ou outro cliente HTTP.

## Ajustes feitos no ambiente local

### 1. Instalação do runtime correto

**O que aconteceu:**  
A aplicação não rodava porque o projeto era `.NET 8`, mas o ambiente tinha problemas com o runtime necessário.

Foi verificado:

```bash
dotnet --list-runtimes
dotnet --list-sdks
```

Depois foi necessário garantir a instalação do runtime ASP.NET Core 8.

**Por quê:**  
Projetos Web em .NET precisam do runtime `Microsoft.AspNetCore.App`, não apenas do runtime base `Microsoft.NETCore.App`.

## 2. Porta correta do Swagger

**O que aconteceu:**  
Inicialmente foi tentado acessar `localhost:5000`, mas o projeto estava configurado em outra porta.

As portas corretas estavam no `launchSettings.json`:

- `http://localhost:5127`;
- `https://localhost:7196`.

**Por quê:**  
O Swagger só abre na porta em que a API realmente está rodando.

## 3. Configuração da connection string

**O que foi alterado:**  
A string de conexão foi configurada no `appsettings.json`:

```txt
Host=localhost;Port=5432;Database=glowapi_db;Username=meu_usuario;Password=minha_senha
```

**Por quê:**  
A API estava tentando conectar com valores genéricos, como `seu_usuario`, e o PostgreSQL recusava a conexão.

## 4. Criação do banco e usuário no PostgreSQL

**O que foi alterado:**  
Foi criado o banco:

```sql
CREATE DATABASE glowapi_db;
```

E também o usuário:

```sql
CREATE USER meu_usuario WITH PASSWORD 'minha_senha';
GRANT ALL PRIVILEGES ON DATABASE glowapi_db TO meu_usuario;
```

**Por quê:**  
Para a API ter um banco real e um usuário válido para conexão.

## 5. Permissões no schema public

**O que foi alterado:**  
Foram dadas permissões ao usuário no schema `public`.

**Por quê:**  
O usuário conseguia acessar o banco, mas não conseguia criar tabelas ao aplicar as migrations.

## 6. Aplicação das migrations

**O que foi alterado:**  
As migrations foram aplicadas com:

```bash
dotnet ef database update --project src/GLOWAPI.Infrastructure --startup-project src/GLOWAPI.API
```

**Por quê:**  
Para criar no PostgreSQL as tabelas definidas pelo Entity Framework.

## 7. Correção de nomes antigos no banco

**O que foi alterado:**  
Foi aplicada a migration `RenameUsuarioCreateAdToCreatedAt`, que corrigiu:

- `Tentivas` para `Tentativas`;
- `Senha` para `SenhaHash`;
- `CreateAd` para `CreatedAt`.

**Por quê:**  
O código atual esperava os nomes corrigidos, mas o banco tinha colunas antigas.

## Erros encontrados e aprendizado

### role "seu_usuario" does not exist

**Significado:**  
A API estava tentando conectar com um usuário que não existia no PostgreSQL.

**Aprendizado:**  
A connection string precisa apontar para um usuário real do banco.

## relation "Usuarios" does not exist

**Significado:**  
A API conectou no banco, mas a tabela `Usuarios` ainda não existia.

**Aprendizado:**  
Criar o banco não cria as tabelas. É preciso aplicar migrations.

## permission denied for schema public

**Significado:**  
O usuário tinha acesso ao banco, mas não podia criar tabelas no schema `public`.

**Aprendizado:**  
No PostgreSQL, permissão no banco e permissão no schema são coisas diferentes.

## column u.CreatedAt does not exist

**Significado:**  
O código esperava a coluna `CreatedAt`, mas o banco ainda tinha `CreateAd`.

**Aprendizado:**  
Quando a entidade muda, o banco precisa ser atualizado com migration.

## Erro no campo role do Swagger

**Significado:**  
Foi enviado `"role": "1"` como string, mas o enum esperava um valor válido.

**Aprendizado:**  
O correto é enviar um número sem aspas, como:

```json
"role": 1
```

ou um nome válido do enum, como:

```json
"role": "Cliente"
```

## Resultado final

Ao final, foi possível:

- rodar a API;
- abrir o Swagger;
- configurar a conexão com PostgreSQL;
- criar banco e usuário;
- aplicar migrations;
- corrigir divergências entre código e banco;
- testar o endpoint `POST api/Usuario`;
- salvar um usuário no banco com senha em hash.

## Resumo para apresentação

Foi implementada e validada a base da CRUD de usuários em uma API .NET 8 com PostgreSQL, Entity Framework Core e Swagger.

O trabalho envolveu ajustes na entidade `Usuario`, configuração do EF Core, criação do service, controller, injeção de dependência, preparação para JWT, configuração do banco local, aplicação de migrations e correção de erros de ambiente.

O principal resultado foi confirmar que a API consegue receber uma requisição pelo Swagger, processar a criação de usuário, gerar hash da senha e salvar o registro no PostgreSQL.

