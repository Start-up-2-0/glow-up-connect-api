# Login e sessao — frontend

Login, renovacao de token, logout e perfil do usuario.

**Pre-requisito:** conta confirmada — [confirmacao-conta.md](./confirmacao-conta.md)  
**Convencoes:** [convencoes.md](./convencoes.md) · **Indice:** [README.md](./README.md)

---

## Login

### `POST /api/auth/login`

**Auth:** nao

### Request

| Campo | Tipo | Obrigatorio |
|-------|------|-------------|
| `email` | string | Sim |
| `senha` | string | Sim |

```json
{
  "email": "maria@email.com",
  "senha": "Senha123!"
}
```

### Response (200)

```json
{
  "success": true,
  "message": "Login realizado com sucesso",
  "data": {
    "token": "eyJ1aWQi...",
    "refreshToken": "opaque-refresh-token",
    "expiresAt": "2026-05-30T15:30:00Z",
    "refreshExpiresAt": "2026-06-06T15:15:00Z",
    "usuario": {
      "id": 42,
      "nome": "Maria Silva",
      "email": "maria@email.com",
      "role": 1,
      "avatarBase64": null
    }
  }
}
```

### Valores de `usuario.role`

| Valor | Significado |
|-------|-------------|
| `1` | Cliente |
| `2` | DonoEstabelecimento |
| `3` | ProfissionalAutonomo |
| `4` | ProfissionalEstabelecimento |
| `5` | Admin |

### Duracao dos tokens (padrao)

| Token | Config | Padrao |
|-------|--------|--------|
| Access (`data.token`) | `Auth:SessionMinutes` | 15 min |
| Refresh (`data.refreshToken`) | `Auth:RefreshTokenDays` | 7 dias |

### Erros

| HTTP | `code` | Acao no UI |
|------|--------|------------|
| 401 | `INVALID_CREDENTIALS` | Erro generico no formulario |
| 403 | `EMAIL_NAO_CONFIRMADO` | Redirecionar para [confirmacao-conta.md](./confirmacao-conta.md) |
| 403 | `USER_BLOCKED` | Conta bloqueada (~15 min apos 5 tentativas) |
| 403 | `USER_INACTIVE` | Conta desativada — suporte |

**Bloqueio:** 5 senhas erradas (`Auth:MaxLoginAttempts`) → bloqueio 15 min (`Auth:LockoutMinutes`).

---

## Apos o login

1. Persistir `token`, `refreshToken`, `expiresAt`, `refreshExpiresAt`.
2. Enviar `x-glow-token: data.token` em rotas autenticadas.
3. Renovar com refresh antes de expirar.
4. Em `401 TOKEN_EXPIRED`, tentar refresh; se falhar, ir para login.

---

## Refresh

### `POST /api/auth/refresh`

**Auth:** nao

```json
{ "refreshToken": "..." }
```

### Response (200)

```json
{
  "success": true,
  "message": "Token renovado com sucesso",
  "data": {
    "token": "novo-access-token",
    "refreshToken": "novo-refresh-token",
    "expiresAt": "...",
    "refreshExpiresAt": "..."
  }
}
```

O refresh **rotaciona** ambos os tokens — sempre substituir os valores armazenados.

---

## Logout

### `POST /api/auth/logout`

**Header:** `x-glow-token: <token>`

### Response (200)

```json
{
  "success": true,
  "message": "Logout realizado com sucesso"
}
```

Apos logout, o token deixa de funcionar imediatamente.

---

## Perfil

### `GET /api/usuario/me`

**Header:** `x-glow-token: <token>`

Retorna dados do usuario logado (sem envelope `success` — ver DTO `UsuarioResponseDto`).

---

## TypeScript

```typescript
export interface LoginRequest {
  email: string;
  senha: string;
}

export interface LoginResponse {
  success: true;
  message: string;
  data: {
    token: string;
    refreshToken: string;
    expiresAt: string;
    refreshExpiresAt: string;
    usuario: {
      id: number;
      nome: string;
      email: string;
      role: 1 | 2 | 3 | 4 | 5;
      avatarBase64: string | null;
    };
  };
}

export interface RefreshTokenRequest {
  refreshToken: string;
}
```

---

## Exemplo (fetch)

```typescript
const BASE_URL = import.meta.env.VITE_API_URL ?? "http://localhost:5127";

export async function login(body: LoginRequest): Promise<LoginResponse> {
  const res = await fetch(`${BASE_URL}/api/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  const data = await res.json();
  if (!res.ok) throw data;
  return data;
}

export function authHeaders(token: string): HeadersInit {
  return {
    "Content-Type": "application/json",
    "x-glow-token": token,
  };
}

export async function refreshToken(refreshToken: string) {
  const res = await fetch(`${BASE_URL}/api/auth/refresh`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ refreshToken }),
  });
  const data = await res.json();
  if (!res.ok) throw data;
  return data;
}

export async function logout(token: string) {
  const res = await fetch(`${BASE_URL}/api/auth/logout`, {
    method: "POST",
    headers: authHeaders(token),
  });
  const data = await res.json();
  if (!res.ok) throw data;
  return data;
}
```

---

## Checklist

- [ ] Persistir e rotacionar tokens no refresh
- [ ] Interceptor com `x-glow-token`
- [ ] Tratar `EMAIL_NAO_CONFIRMADO` → confirmacao
- [ ] Tratar `USER_BLOCKED` e `INVALID_CREDENTIALS`

---

## Referencias no codigo

| Artefato | Caminho |
|----------|---------|
| Controller | `src/GLOWAPI.API/Controllers/AuthController.cs` |
| Service | `src/GLOWAPI.Application/Services/AuthService.cs` |
| DTO login | `src/GLOWAPI.Application/DTOs/Auth/LoginRequestDto.cs` |
| Testes | `tests/GLOWAPI.Tests/Integration/AuthControllerTests.cs` |
