# Confirmacao WhatsApp

Fluxo **WhatsApp + e-mail backup** (estilo Bode): token `base64(telefoneNormalizado)`, link publico `/c/{token}`, confirmacao inbound no webhook Evolution.

Ver documentacao espelhada no frontend: `glow-up-connect-app/docs/frontend/confirmacao-whatsapp.md`.

## Servicos principais

| Arquivo | Responsabilidade |
|---------|------------------|
| `TelefoneHelper.cs` | `NormalizarParaConfirmacaoInbound`, `GerarTokenConfirmacao` |
| `ConfirmacaoWhatsAppService.cs` | Usuario: disparo + inbound |
| `ConfirmacaoWhatsAppEstabelecimentoService.cs` | Estabelecimento: disparo + inbound |
| `WebhookWhatsAppService.cs` | Orquestra confirmacao inbound |
| `ConfirmacaoWhatsAppInstrucoesBuilder.cs` | Monta DTO de resposta |

## Testes

```powershell
dotnet test --filter "FullyQualifiedName~ConfirmacaoWhatsApp|FullyQualifiedName~TelefoneHelper|FullyQualifiedName~WebhookWhatsApp"
```
