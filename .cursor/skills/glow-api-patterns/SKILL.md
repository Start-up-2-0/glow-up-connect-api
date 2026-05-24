---
name: glow-api-patterns
description: >-
  Padroes de controller fino, service com regra de negocio e tratamento de erros
  via excecoes de dominio no glow-up-connect-api. Use ao criar ou refatorar
  controllers, services, endpoints e ExceptionMiddleware neste repositorio.
---

# GLOWAPI — Padroes de camada e erros

## Controller (API)

- Recebe request HTTP, chama **um** service, retorna `IActionResult`
- **Nao** contem regra de negocio, validacao de dominio ou orquestracao entre services
- **Nao** injeta repositorios nem services de infraestrutura (ex.: `IAuthSessionService`), exceto em controllers de auth
- Pode receber `CancellationToken` e repassar ao service
- Referencia: [`UsuarioController`](../../src/GLOWAPI.API/Controllers/UsuarioController.cs)

```csharp
[HttpPost]
public async Task<IActionResult> CriarUsuario([FromBody] CriarUsuarioDto request, CancellationToken cancellationToken)
{
    var usuario = await _usuarioService.CriarUsuarioAsync(request, cancellationToken);
    return StatusCode(StatusCodes.Status201Created, usuario);
}
```

## Service (Application)

- Contem regras de negocio e orquestracao entre repositorios e outros services da camada Application
- Valida pre-condicoes e **lanca excecoes de dominio** (`GLOWAPI.Domain.Exceptions`)
- **Nao** usar `try/catch` generico em todo metodo — o [`ExceptionMiddleware`](../../src/GLOWAPI.API/Middlewares/ExceptionMiddleware.cs) traduz excecoes para HTTP
- Seguir [`AuthService`](../../src/GLOWAPI.Application/Services/AuthService.cs) e [`UsuarioService`](../../src/GLOWAPI.Application/Services/UsuarioService.cs) como referencia
- Operacoes do usuario autenticado: injetar `ICurrentUserContext` no service e expor metodos `*Atual*` ou `*PerfilAtual*`

## Excecoes de dominio

| Classe base | Uso |
|---|---|
| `DomainException` | Regras de negocio gerais (usuario, agendamento, etc.) |
| `AuthenticationException` | Auth, sessao, token |

Criar excecoes tipadas com `ErrorCode` estavel:

```csharp
public class UsuarioNaoEncontradoException : DomainException
{
    public const string ErrorCode = "USUARIO_NAO_ENCONTRADO";
    public UsuarioNaoEncontradoException()
        : base("Usuário não encontrado ou inativo.", ErrorCode) { }
}
```

Registrar mapeamento no `ExceptionMiddleware`:

- `UsuarioNaoEncontradoException` → 404
- `EmailJaCadastradoException` → 409
- `AuthenticationException` → 401/403 (ja existente)

Evitar em codigo novo: `KeyNotFoundException`, `InvalidOperationException` para regras de negocio.

## Quando usar try/catch no service

Somente quando necessario:

- Compensacao de transacao
- Converter excecao de infraestrutura em excecao de dominio na fronteira
- Retry explicito

Nunca envolver todo metodo em `try/catch` so para relancar outra excecao.

## Perfil autenticado (/me)

- Rotas: `GET/PUT/DELETE /api/{recurso}/me`
- Service obtem `UserId` e `SessionId` via `ICurrentUserContext`
- Controller nao conhece sessao nem revogacao de token

## DTOs de entrada

- DTOs de input ficam em `GLOWAPI.Application/DTOs/{Entidade}/` (ex.: `Usuario/`, `Auth/`)
- Controller e service usam o **mesmo DTO** — controller passa o objeto inteiro: `await _service.CriarUsuarioAsync(dto, ct)`
- **Proibido** desmontar DTO campo a campo na controller
- **Proibido** assinaturas de service com lista de primitivos quando existe um DTO
- DataAnnotations no DTO da Application; ASP.NET valida no `[FromBody]`
- DTOs de response (ex.: `UsuarioResponseDto`, `LoginResponseDto`) tambem ficam em `Application/DTOs/`

Referencia: [`CriarUsuarioDto`](../../src/GLOWAPI.Application/DTOs/Usuario/CriarUsuarioDto.cs), [`UsuarioController`](../../src/GLOWAPI.API/Controllers/UsuarioController.cs)

## Checklist ao criar endpoint

1. DTO de entrada em `Application/DTOs/{Entidade}/`
2. Metodo na interface `I*Service`
3. Regras e orquestracao no `*Service`
4. Excecoes de dominio para falhas previsiveis
5. Controller fino (1 service, mapeamento HTTP)
6. Testes unitarios do service + integracao se auth/envolver middleware
