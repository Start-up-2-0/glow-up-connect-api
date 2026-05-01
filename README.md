# GLOWAPI 

Uma API robusta desenvolvida em **.NET 8** utilizando os princípios de **Clean Architecture** (Arquitetura Limpa). O projeto foi estruturado para ser escalável, testável e independente de provedores de banco de dados.

## Arquitetura

O projeto segue a divisão em 4 camadas principais:

- **Domain:** Entidades, interfaces globais e lógica de domínio pura.
- **Application:** Casos de uso, DTOs, Mappers e lógica de negócio.
- **Infrastructure:** Implementação do Entity Framework Core, acesso a dados e serviços externos.
- **API:** Ponto de entrada (REST), Controllers e configurações de Middleware.

## Estrutura de Pastas

```text
GLOWAPI/
├── src/
│   ├── GLOWAPI.Domain/                 # Camada de Domínio (O Coração)
│   │   ├── Entities/                   # Classes que representam as tabelas do banco
│   │   ├── Interfaces/                 # Contratos de repositórios e serviços
│   │   ├── Exceptions/                 # Exceções personalizadas do domínio
│   │   └── Common/                     # Classes base (ex: BaseEntity)
│   │
│   ├── GLOWAPI.Application/            # Camada de Aplicação (Regras de Negócio)
│   │   ├── DTOs/                       # Objetos de transferência de dados (Request/Response)
│   │   ├── Interfaces/                 # Interfaces de serviços da aplicação
│   │   ├── Services/                   # Implementação da lógica de negócio
│   │   ├── Mappers/                    # Configurações de mapeamento (AutoMapper)
│   │   └── Validators/                 # Validações de entrada (FluentValidation)
│   │
│   ├── GLOWAPI.Infrastructure/         # Camada de Infraestrutura (Implementação Técnica)
│   │   ├── Data/
│   │   │   ├── Context/                # ApplicationDbContext
│   │   │   ├── Migrations/             # Histórico de migrações do banco
│   │   │   └── Repositories/           # Implementação real dos repositórios
│   │   ├── Configurations/             # Mapeamento Fluent API (Tamanhos de campos, etc)
│   │   └── DependencyInjection.cs      # Registro dos serviços desta camada
│   │
│   └── GLOWAPI.API/                    # Camada de Apresentação (Interface REST)
│       ├── Controllers/                # Endpoints da API
│       ├── Middlewares/                # Filtros de erro e logs
│       ├── appsettings.json            # Configurações de conexão e variáveis
│       └── Program.cs                  # Configuração e inicialização da API
│
├── tests/                              # (Opcional) Testes Unitários e de Integração
└── GLOWAPI.sln                         # Arquivo de solução do Visual Studio/Dotnet

```

## Tecnologias Utilizadas

- **Runtime:** .NET 8
- **ORM:** Entity Framework Core
- **Banco de Dados:** PostgreSQL
- **Documentação:** Swagger (OpenAPI)

## Configuração de Ambiente

O projeto usa **PostgreSQL** como banco principal.

### Desenvolvimento

Em desenvolvimento, a conexão fica em `src/GLOWAPI.API/appsettings.Development.json`:

```json
{
  "Database": {
    "Provider": "PostgreSQL"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=glow_up_connect_dev;Username=postgres;Password=postgres"
  }
}
```

### Produção

Em produção, a connection string não deve ficar em arquivo versionado. Quando `ASPNETCORE_ENVIRONMENT=Production`, a aplicação olha apenas para a variável:

```bash
POSTGSL="Host=<host>;Port=5432;Database=<database>;Username=<user>;Password=<password>;SSL Mode=Require;Trust Server Certificate=true"
```

Em produção, `ConnectionStrings__DefaultConnection`, `POSTGRESQL` ou qualquer outra variável de conexão são ignoradas para evitar fallback acidental.

## Como Executar o Projeto

### 1. Pré-requisitos
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Ferramenta de linha de comando do EF Core:
  ```bash
  dotnet tool install --global dotnet-ef
  ```
