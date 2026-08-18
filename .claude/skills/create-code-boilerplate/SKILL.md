---
name: create-code-boilerplate
description: Gera boilerplate de código C# para diferentes tipos de arquivos.
arguments:
  type:
    type: string
    enum: [class, interface, enum, record, controller, service, repository, options, model]
    description: O tipo de boilerplate a ser gerado (ex: class, interface, controller).
  name:
    type: string
    description: O nome do arquivo/classe/interface (ex: MyClass, IMyInterface).
  filepath:
    type: string
    optional: true
    description: O caminho completo onde o arquivo deve ser criado (ex: glow-up-connect-api/src/GLOWAPI.Application/Services/MyService.cs).
  namespace:
    type: string
    optional: true
    description: O namespace para o código gerado. Se não fornecido e filepath for, será inferido.
---

### `create-code-boilerplate`

Esta skill gera um boilerplate de código C# para o tipo especificado.

**Uso:**

\`\`\`
/create-code-boilerplate type="class" name="MinhaNovaClasse" filepath="glow-up-connect-api/src/GLOWAPI.Application/Models/MinhaNovaClasse.cs"
\`\`\`

\`\`\`javascript
const { type, name, filepath, namespace: explicitNamespace } = args;

if (!type || !name) {
  throw new Error("Os argumentos 'type' e 'name' são obrigatórios.");
}

let inferredNamespace = explicitNamespace;
if (!inferredNamespace && filepath) {
  const srcIndex = filepath.indexOf("src/");
  if (srcIndex !== -1) {
    const relevantPath = filepath.substring(srcIndex + 4); // After "src/"
    const namespaceParts = relevantPath.split('/');
    if (namespaceParts.length > 1) {
      namespaceParts.pop(); // Remove file name
      inferredNamespace = namespaceParts.join('.');
    }
  }
  // Default fallback if inference fails or path is not standard
  if (!inferredNamespace) {
      inferredNamespace = "GLOWAPI.Application"; // Default if cannot infer better
  }
} else if (!inferredNamespace) {
    inferredNamespace = "GLOWAPI.Application"; // Default if no filepath
}


let codeContent = '';
switch (type) {
  case 'class':
    codeContent = `namespace ${inferredNamespace};

public class ${name}
{
    // Propriedades e métodos aqui
}
`;
    break;
  case 'interface':
    codeContent = `namespace ${inferredNamespace};

public interface I${name}
{
    // Métodos e propriedades aqui
}
`;
    break;
  case 'enum':
    codeContent = `namespace ${inferredNamespace};

public enum ${name}
{
    Value1,
    Value2,
    // ...
}
`;
    break;
  case 'record':
    codeContent = `namespace ${inferredNamespace};

public record ${name}(string PropriedadeExemplo);
`;
    break;
  case 'controller':
    codeContent = `using Microsoft.AspNetCore.Mvc;

namespace ${inferredNamespace};

[ApiController]
[Route("[controller]")]
public class ${name}Controller : ControllerBase
{
    private readonly ILogger<${name}Controller> _logger;

    public ${name}Controller(ILogger<${name}Controller> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Get()
    {
        _logger.LogInformation("Endpoint GET de ${name}Controller acessado.");
        return Ok("Hello from ${name}Controller!");
    }
}
`;
    break;
  case 'service':
    codeContent = `namespace ${inferredNamespace};

public interface I${name}Service
{
    Task<string> GetAsync(string id);
}

public class ${name}Service : I${name}Service
{
    public ${name}Service()
    {
        // Construtor
    }

    public Task<string> GetAsync(string id)
    {
        // Lógica do serviço
        return Task.FromResult($"Hello from ${name}Service with id: {id}");
    }
}
`;
    break;
  case 'repository':
    codeContent = `using ${inferredNamespace.replace('.Infrastructure', '.Domain')}.Entities; // Ajuste o namespace da entidade conforme necessário

namespace ${inferredNamespace};

public interface I${name}Repository
{
    Task<${name}Entity?> GetByIdAsync(Guid id);
    Task AddAsync(${name}Entity entity);
    Task UpdateAsync(${name}Entity entity);
    Task DeleteAsync(Guid id);
}

public class ${name}Repository : I${name}Repository
{
    // Exemplo de contexto de banco de dados, ajuste conforme o projeto
    // private readonly ApplicationDbContext _dbContext;

    public ${name}Repository(/* ApplicationDbContext dbContext */)
    {
        // _dbContext = dbContext;
    }

    public Task<${name}Entity?> GetByIdAsync(Guid id)
    {
        // Implementação para buscar por ID
        return Task.FromResult<${name}Entity?>(null);
    }

    public Task AddAsync(${name}Entity entity)
    {
        // Implementação para adicionar
        return Task.CompletedTask;
    }

    public Task UpdateAsync(${name}Entity entity)
    {
        // Implementação para atualizar
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        // Implementação para deletar
        return Task.CompletedTask;
    }
}
`;
    break;
  case 'options':
    codeContent = `namespace ${inferredNamespace};

public class ${name}Options
{
    public const string ${name} = "${name}"; // Nome da seção no appsettings.json

    public string ExemploConfiguracao { get; set; } = string.Empty;
}
`;
    break;
  case 'model':
    codeContent = `namespace ${inferredNamespace};

public class ${name}
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
}
`;
    break;
  default:
    throw new Error(`Tipo de boilerplate '${type}' não suportado.`);
}

if (filepath) {
  await Write({
    file_path: filepath,
    content: codeContent
  });
  return `Arquivo '${filepath}' do tipo '${type}' criado com sucesso!`;
} else {
  return `Boilerplate de '${type}' com nome '${name}' e namespace '${inferredNamespace}':\n` + "```csharp\n" + codeContent + "```";
}