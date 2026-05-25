# Mensageria assincrona — GLOWAPI

## Visao geral

Fila de notificacoes persistida em PostgreSQL, processada por **BackgroundServices** no mesmo host da **GLOWAPI.API**. Canais iniciais: Email, WhatsApp e SMS (provedores stub na fase 1).

## Fluxo completo

```txt
1. Modulo de negocio chama IMensagemNotificacaoService.RegistrarAsync
2. Mensagem salva com Status = Pendente
3. MensagemNotificacaoWorker reserva lote (FOR UPDATE SKIP LOCKED)
4. Status -> Processando + InstanciaWorker
5. Provedor do canal envia mensagem
6. Sucesso -> Enviado + log de tentativa
7. Falha -> Reprocessar (com AgendadoPara) ou Falhou (max tentativas)
8. MensagemNotificacaoRecuperacaoWorker libera mensagens Processando travadas
```

## Ciclo de vida dos status

| Status | Descricao |
|--------|-----------|
| Pendente | Aguardando primeiro processamento |
| Processando | Reservada por um worker |
| Enviado | Entregue com sucesso (terminal) |
| Reprocessar | Falha com tentativas restantes; aguarda AgendadoPara |
| Falhou | Esgotou MaximoTentativas (terminal) |
| Cancelado | Cancelada antes do envio (terminal) |

Regras:

- Nunca reprocessar `Enviado` ou `Cancelado`
- Respeitar `AgendadoPara` na reserva do lote
- Prioridade maior processada primeiro (`Prioridade DESC`, `CriadoEm ASC`)

## Lock e concorrencia

Reserva atomica em transacao:

```sql
SELECT * FROM "MensagensNotificacao"
WHERE "Status" IN ('Pendente', 'Reprocessar')
  AND "Tentativas" < "MaximoTentativas"
  AND ("AgendadoPara" IS NULL OR "AgendadoPara" <= @utcNow)
ORDER BY "Prioridade" DESC, "CriadoEm" ASC
LIMIT @batchSize
FOR UPDATE SKIP LOCKED
```

Permite multiplos workers sem duplicar envio: instancias concorrentes ignoram linhas ja bloqueadas.

`InstanciaWorker` identifica qual worker reservou o lote (`{prefixo}-{machine}-{guid}`).

## Recuperacao de travadas

`MensagemNotificacaoRecuperacaoWorker` executa periodicamente:

- Mensagens em `Processando` com `ProcessamentoIniciadoEm` anterior a `TimeoutProcessamentoMinutos`
- Atualizadas para `Reprocessar` (InstanciaWorker e ProcessamentoIniciadoEm limpos)

## Retry e backoff

Em falha de envio:

- `Tentativas` incrementa
- Se `Tentativas >= MaximoTentativas` -> `Falhou`
- Caso contrario -> `Reprocessar` com `AgendadoPara = utcNow + min(BackoffBase * 2^Tentativas, BackoffMaximo)`

Configuracao em `Mensageria` no [`appsettings.json`](../src/GLOWAPI.API/appsettings.json) da API (unica fonte).

## Batch processing

- `TamanhoLote` (padrao 10) mensagens por ciclo
- `IntervaloProcessamentoMs` entre ciclos do worker principal
- Falha em uma mensagem do lote nao interrompe as demais

## Configuracao (appsettings)

```json
"Mensageria": {
  "Habilitado": true,
  "TamanhoLote": 10,
  "IntervaloProcessamentoMs": 2000,
  "MaximoTentativasPadrao": 5,
  "BackoffBaseSegundos": 30,
  "BackoffMaximoSegundos": 3600,
  "TimeoutProcessamentoMinutos": 15,
  "IntervaloRecuperacaoMs": 60000,
  "InstanciaWorkerPrefixo": "glow-worker",
  "MascararDadosSensiveisEmLogs": true
}
```

Subsecoes futuras: `Mensageria:Email`, `Mensageria:WhatsApp`, `Mensageria:Sms`.

## Adicionar novo provedor

1. Implementar `IProvedorMensagem` em `Infrastructure/Mensageria/Provedores/`
2. Definir `CanalSuportado`
3. Registrar em `Infrastructure/DependencyInjection.cs`: `services.AddScoped<IProvedorMensagem, SeuProvedor>();`
4. Se novo canal: adicionar valor em `CanalMensagemNotificacao` + migration

## Integracao com outros modulos

Injetar `IMensagemNotificacaoService` no service de negocio:

```csharp
await _mensagemNotificacaoService.RegistrarAsync(new RegistrarMensagemNotificacaoDto
{
    Canal = CanalMensagemNotificacao.Email,
    Destinatario = usuario.Email,
    Assunto = "Confirmacao",
    Conteudo = "...",
    EstabelecimentoId = estabelecimentoId,
    Prioridade = 1
}, cancellationToken);
```

Nao usar fire-and-forget: persistir antes de retornar.

Endpoints HTTP opcionais:

- `POST /api/mensagens-notificacao`
- `DELETE /api/mensagens-notificacao/{guid}`

## Workers e deploy

| Componente | Papel |
|------------|-------|
| GLOWAPI.API (HTTP) | Enfileira, cancela e expoe endpoints |
| `MensagemNotificacaoWorker` | Processamento continuo em lote |
| `MensagemNotificacaoRecuperacaoWorker` | Recupera mensagens travadas em `Processando` |

Os workers ficam em `src/GLOWAPI.API/Workers/` e sao registrados em `Program.cs` via `AddHostedService`.

Execucao local:

```powershell
dotnet run --project src/GLOWAPI.API
```

Deploy: um unico servico com [`Dockerfile`](../Dockerfile) no Railway; variavel `POSTGSL` em staging/producao. Cada replica da API executa os workers; concorrencia e segura com `FOR UPDATE SKIP LOCKED`.

Desabilitar processamento: `Mensageria:Habilitado = false` (util em testes de integracao).

## Observabilidade

- Logs estruturados com `MensagemGuid`, `Canal`, `Tentativa`, `InstanciaWorker`
- Historico por tentativa em `MensagensNotificacaoLogs`
- Dados sensiveis mascarados quando `MascararDadosSensiveisEmLogs = true`

## Extensao futura

- Push, Webhook, notificacao interna (novos valores de canal + provedores)
- Cache Redis apenas se volume exigir; fila primaria permanece no banco
