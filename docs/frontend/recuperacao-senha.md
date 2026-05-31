# Recuperacao de senha — frontend

Fluxo **esqueci a senha** e **redefinir senha**.

**Convencoes:** [convencoes.md](./convencoes.md) · **Indice:** [README.md](./README.md)

---

## Status atual

> Endpoints existem mas retornam **501 Not Implemented**.  
> Nao integrar em producao ate o backend liberar a feature.

```http
POST /api/auth/forgot-password
POST /api/auth/reset-password
```

Resposta atual (501):

```json
{
  "success": false,
  "message": "Recuperação de senha ainda não implementada.",
  "code": "NOT_IMPLEMENTED"
}
```

---

## Contrato previsto

Baseado no design interno (`docs/mensageria.md`). **Sujeito a alteracao** quando implementado.

### Passo 1 — Solicitar e-mail

**Tela frontend:** `/esqueci-senha`

```http
POST {BASE_URL}/api/auth/forgot-password
Content-Type: application/json
```

```json
{ "email": "maria@email.com" }
```

Resposta esperada (200) — mensagem generica:

```json
{
  "success": true,
  "message": "Se o e-mail estiver cadastrado, enviaremos instrucoes para redefinir a senha."
}
```

Link no e-mail (previsto):

```text
{Auth:FrontendBaseUrl}/resetar-senha?token=<token-opaco>
```

Validade prevista: **30 minutos**.

---

### Passo 2 — Redefinir senha

**Tela frontend:** `/resetar-senha?token=...`

```http
POST {BASE_URL}/api/auth/reset-password
Content-Type: application/json
```

```json
{
  "token": "token-opaco-da-url",
  "senha": "NovaSenha123!",
  "confirmarSenha": "NovaSenha123!"
}
```

Regras de `senha`: mesmas do [cadastro.md](./cadastro.md).

Resposta esperada (200):

```json
{
  "success": true,
  "message": "Senha redefinida com sucesso. Voce ja pode fazer login."
}
```

---

## Erros previstos

| HTTP | `code` | Quando |
|------|--------|--------|
| 400 | `RESET_SENHA_INVALIDO` | Token expirado ou invalido |
| 400 | — (validation) | Senhas nao conferem ou complexidade invalida |

---

## Preparacao no frontend (agora)

1. Tela **Esqueci minha senha** — pode exibir "em breve" ou desabilitar submit.
2. Tela **Redefinir senha** em `/resetar-senha?token=...`.
3. Tratar `501 NOT_IMPLEMENTED` ate o backend entregar a feature.

---

## TypeScript (previsto)

```typescript
export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  token: string;
  senha: string;
  confirmarSenha: string;
}
```

---

## Checklist

- [ ] Tela `/esqueci-senha` preparada
- [ ] Tela `/resetar-senha?token=` preparada
- [ ] Tratar `501` graciosamente
- [ ] Apos implementacao: mesma UX de mensagem generica do reenvio de confirmacao

---

## Referencias no codigo

| Artefato | Caminho |
|----------|---------|
| Controller (stub) | `src/GLOWAPI.API/Controllers/AuthController.cs` |
| Design mensageria | `docs/mensageria.md` (secao recuperacao de senha) |
