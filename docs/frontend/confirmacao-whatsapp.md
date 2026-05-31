# Confirmacao WhatsApp

Fluxo **e-mail + inbound**: ao solicitar confirmacao, a API envia um **e-mail** com codigo e link `wa.me` para o WhatsApp da plataforma. O usuario envia a mensagem; o webhook confirma e libera alertas.

**Regra geral:** alertas so sao enfileirados quando o telefone foi **confirmado** (`whatsAppConfirmado`) e o usuario/negocio aceitou alertas (`whatsAppOptIn`).

| Perfil | Telefone confirmado | E-mail(s) destino |
|--------|---------------------|-------------------|
| Cliente / profissional | `Usuario.Telefone` | `Usuario.Email` |
| Estabelecimento | `Estabelecimento.Telefone` | Conta logada + e-mail comercial (deduplicados) |

**Pre-requisito:** conta ativa (e-mail de cadastro confirmado) e telefone no perfil.

---

## Fluxo principal

```mermaid
sequenceDiagram
    participant App as Frontend
    participant API as GLOWAPI
    participant Mail as Email
    participant User as Usuario
    participant WA as WhatsAppPlataforma
    participant Webhook as WebhookEvolution

    App->>API: POST solicitar-confirmacao
    API->>Mail: Email codigo + link wa.me
    API-->>App: linkWhatsApp + emailEnviado
    User->>Mail: Abre e-mail no celular
    User->>WA: Clica Confirmar no WhatsApp
    WA->>Webhook: messages.upsert
    Webhook->>API: Confirma telefone + optIn
    App->>API: GET /usuario/me poll
```

1. Cadastre o telefone no perfil.
2. Chame `solicitar-confirmacao` — a API envia e-mail e retorna instrucoes.
3. No celular, abra o e-mail e toque em **Confirmar no WhatsApp** (link `wa.me` com mensagem `GLOW {codigo}` pre-preenchida).
4. Envie a mensagem **do numero cadastrado no perfil**.
5. O webhook confirma automaticamente (`whatsAppOptIn = true`).

---

## Cliente — campos no perfil

`GET /api/usuario/me`:

```json
{
  "telefone": "11988887777",
  "whatsAppConfirmado": true,
  "whatsAppOptIn": true,
  "whatsAppPendenteConfirmacao": false
}
```

---

## Cliente — solicitar confirmacao

`POST /api/usuario/me/whatsapp/solicitar-confirmacao`

**Headers:** `x-glow-token`

**200:**

```json
{
  "message": "Verifique seu e-mail para confirmar o WhatsApp.",
  "data": {
    "numeroPlataforma": "5511999999999",
    "codigoConfirmacao": "482913",
    "mensagemSugerida": "GLOW 482913",
    "linkWhatsApp": "https://wa.me/5511999999999?text=GLOW%20482913",
    "emailEnviado": true,
    "expiraEm": "2026-05-31T12:00:00Z"
  }
}
```

Dispara e-mail automaticamente ao alterar `telefone` em `PUT /api/usuario/me`.

---

## Estabelecimento — solicitar confirmacao

`POST /api/estabelecimentos/{estabelecimentoId}/whatsapp/solicitar-confirmacao`

**Headers:** `x-glow-token` + permissao `NegocioEditar`

Envia e-mail para:
- e-mail da conta logada; e
- e-mail comercial do estabelecimento (`Estabelecimento.Email`), se diferente.

Mesmo formato de resposta do cliente.

---

## Profissional e equipe

Mesmo fluxo do cliente via `Usuario.Email` e `Usuario.Telefone`.

Profissional autonomo: ao mudar telefone, sincroniza `Usuario` + telefone comercial e dispara e-mails correspondentes.

---

## Reenviar instrucoes por e-mail

`POST /api/auth/reenviar-confirmacao-whatsapp` (sem auth)

```json
{
  "email": "maria@email.com"
}
```

**200:** resposta generica (nao revela se o e-mail existe ou ja foi confirmado).

---

## Webhook Evolution API

`POST /api/webhooks/whatsapp/evolution` (sem auth)

Configure na instancia Evolution:
- **URL:** `https://sua-api/api/webhooks/whatsapp/evolution`
- **Eventos:** `MESSAGES_UPSERT`
- Ignora mensagens `fromMe: true`

---

## Fallback manual (opcional)

- `POST /api/auth/confirmar-whatsapp` com `{ codigo, telefone }`
- `POST /api/estabelecimentos/{id}/whatsapp/confirmar` com `{ codigo }`

Preferir sempre e-mail + link wa.me em producao.

---

## Opt-in / opt-out

Apos confirmacao inbound, `whatsAppOptIn` vem `true`. Para desativar alertas:

`POST /api/usuario/me/whatsapp/opt-in` ou `POST /api/estabelecimentos/{id}/whatsapp/opt-in`

```json
{ "optIn": false }
```

---

## Alertas de agendamento

| Destinatario | Condicao |
|--------------|----------|
| Cliente | `Usuario.PodeReceberAlertasWhatsApp()` |
| Telefone comercial | `Estabelecimento.PodeReceberAlertasWhatsApp()` |
| Profissional | `Profissional.Usuario.PodeReceberAlertasWhatsApp()` |

Eventos: agendamento **confirmado**, **cancelado** ou **remarcado**.

---

## Configuracao backend (Railway)

| Variavel | Exemplo |
|----------|---------|
| `Mensageria__WhatsApp__ApiUrl` | URL Evolution API |
| `Mensageria__WhatsApp__ApiKey` | chave da instancia |
| `Mensageria__WhatsApp__InstanceName` | `glow-staging` |
| `Mensageria__WhatsApp__NumeroPlataforma` | `5511999999999` |
| `Mensageria__WhatsApp__WebhookApiKey` | validacao do webhook |
| `Mensageria__WhatsApp__Habilitado` | `true` |
| `Mensageria__Email__Habilitado` | `true` (envio real de e-mail) |

Em dev com e-mail desabilitado, use `linkWhatsApp` da resposta JSON para testar manualmente.

---

## Checklist frontend

- [ ] Tela "Verifique seu e-mail" apos solicitar confirmacao
- [ ] Exibir `linkWhatsApp` como atalho secundario (deep link)
- [ ] Poll `GET /api/usuario/me` ate `whatsAppConfirmado = true`
- [ ] Toggle opt-in apos confirmacao
- [ ] Tratar expiracao (reenviar via e-mail)
