# Convencoes da API — frontend

Padroes comuns a todos os fluxos de auth e cadastro.

Voltar ao indice: [README.md](./README.md)

---

## Content-Type

Requisicoes com body:

```http
Content-Type: application/json
```

---

## Nomes de propriedades

JSON em **camelCase** (`nome`, `refreshToken`). A API aceita case-insensitive na deserializacao.

---

## Autenticacao

A API **nao usa JWT Bearer**. Token emitido no login:

```http
x-glow-token: <token>
```

Configuravel em `Auth:TokenHeaderName` (padrao: `x-glow-token`).

---

## Formatos de resposta

### Cadastro — body direto (sem envelope)

`POST /api/usuario` retorna `201` com:

```json
{
  "id": 1,
  "nome": "Maria Silva",
  "email": "maria@email.com",
  "telefone": "11999999999",
  "ativo": false,
  "avatarBase64": null,
  "mensagem": "Cadastro realizado. Confirme seu e-mail (link ou codigo) para ativar a conta."
}
```

### Auth — envelope padrao

Sucesso:

```json
{
  "success": true,
  "message": "Mensagem legivel",
  "data": { }
}
```

Alguns endpoints (confirmar e-mail, logout) retornam sucesso **sem** `data`.

Erro de negocio/autenticacao:

```json
{
  "success": false,
  "message": "Descricao legivel",
  "code": "CODIGO_ERRO"
}
```

### Validacao de modelo (400)

Resposta ASP.NET (`ValidationProblemDetails`):

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Senha": ["Senha deve conter: letra maiuscula, minuscula, numero e caractere especial"]
  }
}
```

---

## Erro padrao (TypeScript)

```typescript
export interface ApiErrorResponse {
  success: false;
  message: string;
  code: string;
  details?: unknown;
}
```

---

## Codigos de erro comuns (auth)

| HTTP | `code` | Fluxo |
|------|--------|-------|
| 401 | `INVALID_CREDENTIALS` | Login |
| 403 | `EMAIL_NAO_CONFIRMADO` | Login |
| 403 | `USER_BLOCKED` | Login |
| 403 | `USER_INACTIVE` | Login |
| 401 | `TOKEN_EXPIRED` | Rotas autenticadas |
| 401 | `INVALID_TOKEN` | Rotas autenticadas |
| 400 | `CONFIRMACAO_EMAIL_INVALIDA` | Confirmacao |
| 409 | `EMAIL_JA_CADASTRADO` | Cadastro |
| 400 | `AVATAR_INVALIDO` | Cadastro |
| 400 | `RESET_SENHA_INVALIDO` | Recuperacao de senha |
