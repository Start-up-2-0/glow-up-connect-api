# Cadastro de usuario — frontend

Cria conta de **cliente** via `POST /api/usuario`.

**Proximo passo:** [confirmacao-conta.md](./confirmacao-conta.md)  
**Convencoes:** [convencoes.md](./convencoes.md) · **Indice:** [README.md](./README.md)

---

## Endpoint

```http
POST {BASE_URL}/api/usuario
Content-Type: application/json
```

**Auth:** nao  
**Resposta:** `201 Created` (body direto, sem envelope `success`)

---

## Request body

| Campo | Tipo | Obrigatorio | Regras |
|-------|------|-------------|--------|
| `nome` | string | Sim | 3–150 caracteres |
| `email` | string | Sim | E-mail valido |
| `telefone` | string | Sim | Ate 20 caracteres |
| `senha` | string | Sim | 6–100 chars; maiuscula, minuscula, numero e caractere especial |
| `avatarBase64` | string | Nao | Data URI ou base64 puro |
| `avatarContentType` | string | Nao | Obrigatorio se `avatarBase64` for base64 puro (ex.: `image/png`) |

**Regras:**

- Nao envie `role` — cadastro publico cria sempre `Cliente`.
- Avatar: max 5 MB; tipos `image/jpeg`, `image/png`, `image/webp`.
- Conta inicia com `ativo: false` ate confirmar e-mail.

---

## Exemplo request

```json
{
  "nome": "Maria Silva",
  "email": "maria@email.com",
  "telefone": "11999999999",
  "senha": "Senha123!"
}
```

Com avatar (data URI):

```json
{
  "nome": "Maria Silva",
  "email": "maria@email.com",
  "telefone": "11999999999",
  "senha": "Senha123!",
  "avatarBase64": "data:image/png;base64,iVBORw0KGgo..."
}
```

---

## Exemplo response (201)

```json
{
  "id": 42,
  "nome": "Maria Silva",
  "email": "maria@email.com",
  "telefone": "11999999999",
  "ativo": false,
  "avatarBase64": null,
  "mensagem": "Cadastro realizado. Confirme seu e-mail (link ou codigo) para ativar a conta."
}
```

---

## Comportamento no frontend

1. Apos `201`, redirecionar para tela de confirmacao (nao fazer login).
2. Exibir `mensagem` ou texto equivalente.
3. Passar `email` para a tela de confirmacao (state ou query).
4. Se `409 EMAIL_JA_CADASTRADO`, oferecer login ou recuperacao de senha.

---

## Erros

| HTTP | `code` | Acao no UI |
|------|--------|------------|
| 409 | `EMAIL_JA_CADASTRADO` | "E-mail ja cadastrado" + link login |
| 400 | `AVATAR_INVALIDO` | Erro de formato/tamanho do avatar |
| 400 | — (validation) | Erros por campo em `errors` |

---

## TypeScript

```typescript
export interface CadastrarUsuarioRequest {
  nome: string;
  email: string;
  telefone: string;
  senha: string;
  avatarBase64?: string;
  avatarContentType?: string;
}

export interface CadastrarUsuarioResponse {
  id: number;
  nome: string;
  email: string;
  telefone: string;
  ativo: boolean;
  avatarBase64: string | null;
  mensagem: string;
}
```

---

## Exemplo (fetch)

```typescript
const BASE_URL = import.meta.env.VITE_API_URL ?? "http://localhost:5127";

export async function cadastrarUsuario(
  body: CadastrarUsuarioRequest
): Promise<CadastrarUsuarioResponse> {
  const res = await fetch(`${BASE_URL}/api/usuario`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });

  if (!res.ok) throw await res.json();
  return res.json();
}
```

---

## Checklist

- [ ] Validacao local de senha (mesmas regras da API)
- [ ] Redirecionar para confirmacao apos sucesso
- [ ] Nao tentar login com `ativo: false`

---

## Referencias no codigo

| Artefato | Caminho |
|----------|---------|
| Controller | `src/GLOWAPI.API/Controllers/UsuarioController.cs` |
| DTO | `src/GLOWAPI.Application/DTOs/Usuario/CadastrarClienteDto.cs` |
| Service | `src/GLOWAPI.Application/Services/UsuarioService.cs` |
