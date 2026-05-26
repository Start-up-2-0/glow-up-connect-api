# Mensageria e templates de e-mail

Este guia explica como usar a mensageria do Glow Up Connect no codigo, como criar novas mensagens, como implementar provedores e como criar templates HTML quando o canal for e-mail.

Para detalhes de arquitetura interna, concorrencia, lock SQL, retry e deploy, veja tambem [`context/Mensageria-assincrona.md`](../context/Mensageria-assincrona.md).

## Indice

- [Visao geral](#visao-geral)
- [Quando usar mensageria](#quando-usar-mensageria)
- [Componentes principais](#componentes-principais)
- [Fluxo de processamento](#fluxo-de-processamento)
- [Como enfileirar uma mensagem](#como-enfileirar-uma-mensagem)
- [Campos do DTO](#campos-do-dto)
- [Texto simples vs HTML](#texto-simples-vs-html)
- [Como criar um template de e-mail](#como-criar-um-template-de-e-mail)
- [Template atual de confirmacao de e-mail](#template-atual-de-confirmacao-de-e-mail)
- [Como criar uma nova notificacao transacional](#como-criar-uma-nova-notificacao-transacional)
- [Como cancelar uma mensagem](#como-cancelar-uma-mensagem)
- [Como implementar um novo provedor](#como-implementar-um-novo-provedor)
- [Configuracao](#configuracao)
- [Testes recomendados](#testes-recomendados)
- [Troubleshooting](#troubleshooting)

## Visao geral

A mensageria e uma fila persistida no PostgreSQL, processada por workers dentro da propria API.

Fluxo resumido:

```text
Service de negocio
  -> IMensagemNotificacaoService.RegistrarAsync
  -> MensagensNotificacao no banco com status Pendente
  -> MensagemNotificacaoWorker reserva lote
  -> IProvedorMensagem envia
  -> MensagemNotificacaoLog registra tentativa
```

Use a fila para toda comunicacao assincroma com usuario, como:

- confirmacao de e-mail;
- reenvio de confirmacao;
- recuperacao de senha;
- lembrete de agendamento;
- notificacao de pagamento;
- comunicados de estabelecimento ou profissional.

Nao chame provedores externos diretamente a partir de services de negocio. O padrao do projeto e sempre enfileirar a mensagem.

## Quando usar mensageria

Use mensageria quando a comunicacao:

- depende de servico externo, como Resend, SMS ou WhatsApp;
- pode falhar e deve ter retry;
- nao precisa bloquear a resposta HTTP principal;
- precisa de historico/auditoria de tentativas;
- deve ser enviada em horario futuro;
- pode ser cancelada antes do envio;
- contem dados que ajudam suporte e investigacao futura.

Nao use mensageria para:

- validacoes sincronas de regra de negocio;
- resposta imediata ao usuario na mesma chamada HTTP;
- eventos internos que nao envolvem entrega externa;
- chamadas que precisam retornar um resultado do provedor antes de concluir a operacao.

Exemplo correto:

```text
Cadastro de cliente cria usuario -> enfileira e-mail -> retorna 201
```

Exemplo a evitar:

```text
Cadastro de cliente chama Resend diretamente -> espera envio externo -> retorna 201
```

## Componentes principais

| Componente | Responsabilidade |
|---|---|
| `IMensagemNotificacaoService` | API de aplicacao para registrar ou cancelar mensagens |
| `RegistrarMensagemNotificacaoDto` | Contrato usado para enfileirar uma mensagem |
| `MensagemNotificacaoService` | Cria mensagens pendentes no banco |
| `MensagemNotificacaoWorker` | Processa lotes periodicamente |
| `MensagemNotificacaoRecuperacaoWorker` | Libera mensagens travadas em processamento |
| `MensagemNotificacaoProcessadorService` | Reserva lote, resolve provedor, envia e registra logs |
| `IProvedorMensagem` | Interface para provedores por canal |
| `ProvedorMensagemEmail` | Envia e-mail via Resend |
| `EmailConteudoHtml` | Preserva HTML completo ou converte texto simples em HTML minimo |

## Fluxo de processamento

Estados principais em `StatusMensagemNotificacao`:

| Status | Quando acontece | Terminal |
|---|---|---|
| `Pendente` | Mensagem recem-criada, aguardando worker | Nao |
| `Processando` | Worker reservou a mensagem para envio | Nao |
| `Enviado` | Provedor retornou sucesso | Sim |
| `Reprocessar` | Falhou, mas ainda ha tentativas restantes | Nao |
| `Falhou` | Esgotou tentativas | Sim |
| `Cancelado` | Cancelada antes do envio | Sim |

Sequencia comum:

```text
Pendente -> Processando -> Enviado
```

Sequencia com falha e retry:

```text
Pendente -> Processando -> Reprocessar -> Processando -> Enviado
```

Sequencia com falha final:

```text
Pendente -> Processando -> Reprocessar -> Processando -> Falhou
```

Cada tentativa cria um registro em `MensagensNotificacaoLogs`, com payload de request, payload de response, status, erro e tempo de execucao.

## Ordem de processamento

O worker reserva mensagens respeitando:

```text
Prioridade DESC, CriadoEm ASC
```

Na pratica:

- prioridade maior sai primeiro;
- em mesma prioridade, a mensagem mais antiga sai primeiro;
- mensagens com `AgendadoPara` no futuro nao entram no lote;
- mensagens em `Processando` nao sao pegas por outro worker.

Sugestao de prioridades:

| Prioridade | Uso recomendado |
|---|---|
| `3` | Seguranca e acesso: confirmacao de e-mail, reset de senha, alertas criticos |
| `2` | Transacional comum: pagamento, agendamento confirmado, cancelamento |
| `1` | Lembretes e comunicados normais |
| `0` | Baixa prioridade, campanhas ou tarefas nao urgentes |

## Como enfileirar uma mensagem

Injete `IMensagemNotificacaoService` no service de negocio.

```csharp
using GLOWAPI.Application.DTOs.Mensageria;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Enums;

public class MeuService
{
    private readonly IMensagemNotificacaoService _mensagemNotificacaoService;

    public MeuService(IMensagemNotificacaoService mensagemNotificacaoService)
    {
        _mensagemNotificacaoService = mensagemNotificacaoService;
    }

    public async Task EnviarAvisoAsync(string email, CancellationToken cancellationToken)
    {
        await _mensagemNotificacaoService.RegistrarAsync(new RegistrarMensagemNotificacaoDto
        {
            Canal = CanalMensagemNotificacao.Email,
            Destinatario = email,
            Assunto = "Aviso importante",
            Conteudo = "Sua mensagem em texto simples.",
            Prioridade = 1
        }, cancellationToken);
    }
}
```

O envio real acontece depois, pelo worker. O service de negocio deve apenas persistir a intencao de envio.

### Exemplo com agendamento futuro

Use `AgendadoPara` quando a mensagem deve sair depois.

```csharp
await _mensagemNotificacaoService.RegistrarAsync(new RegistrarMensagemNotificacaoDto
{
    Canal = CanalMensagemNotificacao.Email,
    Destinatario = cliente.Email,
    Assunto = "Lembrete do seu agendamento",
    Conteudo = html,
    Prioridade = 1,
    AgendadoPara = agendamento.Inicio.AddHours(-24)
}, cancellationToken);
```

### Exemplo com maximo de tentativas especifico

Use `MaximoTentativas` quando uma mensagem tem regra diferente do padrao global.

```csharp
await _mensagemNotificacaoService.RegistrarAsync(new RegistrarMensagemNotificacaoDto
{
    Canal = CanalMensagemNotificacao.Email,
    Destinatario = usuario.Email,
    Assunto = "Codigo de seguranca",
    Conteudo = html,
    Prioridade = 3,
    MaximoTentativas = 3
}, cancellationToken);
```

## Campos do DTO

| Campo | Uso |
|---|---|
| `Canal` | Canal da mensagem: `Email`, `WhatsApp`, `Sms` |
| `Destinatario` | E-mail, telefone ou identificador de destino |
| `Assunto` | Assunto do e-mail; pode ser vazio para outros canais |
| `Conteudo` | Corpo da mensagem; pode ser texto simples ou HTML completo |
| `PayloadJson` | Dados estruturados auxiliares para provedores futuros |
| `EstabelecimentoId` | Vinculo opcional com estabelecimento |
| `Prioridade` | Maior prioridade processa antes |
| `AgendadoPara` | Data futura para envio agendado |
| `MaximoTentativas` | Sobrescreve o maximo padrao de tentativas |
| `Provedor` | Forca/identifica provedor quando houver mais de uma opcao |

### Regras de preenchimento

- `Canal`, `Destinatario` e `Conteudo` sao obrigatorios.
- `Destinatario` aceita ate 500 caracteres.
- `Assunto` aceita ate 500 caracteres.
- `PayloadJson` deve ser JSON valido quando usado.
- `AgendadoPara` deve ser salvo em UTC.
- `Prioridade` deve ser pequena e previsivel; evite usar numeros aleatorios altos.
- `Provedor` so deve ser usado quando o resolver/provedor realmente considerar esse valor.

### Quando usar PayloadJson

Use `PayloadJson` para dados estruturados que um provedor pode precisar alem do texto final.

Exemplo:

```csharp
PayloadJson = JsonSerializer.Serialize(new
{
    agendamentoId = agendamento.Id,
    template = "lembrete-agendamento-v1",
    profissional = profissional.NomePublico,
    inicio = agendamento.Inicio
})
```

Evite colocar senha, token plano, cartao, documento ou qualquer segredo em `PayloadJson`. Logs podem armazenar payloads sanitizados, mas a regra segura e nao persistir segredo desnecessario.

## Texto simples vs HTML

O `ProvedorMensagemEmail` usa `EmailConteudoHtml.ConteudoParaHtml`.

Comportamento:

- se `Conteudo` comeca com `<!doctype html` ou `<html`, o HTML e preservado;
- caso contrario, o texto e escapado e quebras de linha viram `<br/>`.

Exemplo com texto simples:

```csharp
Conteudo = """
Ola Maria,

Seu agendamento foi confirmado.
"""
```

Exemplo com HTML completo:

```csharp
Conteudo = MeuTemplateEmail.Criar(nome, link)
```

### Compatibilidade

Texto simples e util para mensagens internas, testes e canais futuros. Para e-mails enviados a usuarios finais, prefira HTML completo com template proprio para manter identidade visual e confiabilidade.

Se o conteudo HTML nao comecar com `<!doctype html>` ou `<html`, ele sera tratado como texto e escapado. Isso evita injecao acidental de HTML, mas tambem significa que fragmentos como `<strong>texto</strong>` nao serao renderizados como HTML.

## Como criar um template de e-mail

Templates transacionais devem ficar na camada `Application`, em:

```text
src/GLOWAPI.Application/Mensageria/
```

Padrao recomendado:

```csharp
using System.Net;

namespace GLOWAPI.Application.Mensageria;

public static class MeuTemplateEmail
{
    public static string Criar(string nome, string link)
    {
        var nomeSeguro = WebUtility.HtmlEncode(nome);
        var linkSeguro = WebUtility.HtmlEncode(link);

        return $$"""
            <!doctype html>
            <html lang="pt-BR">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>Titulo do e-mail</title>
            </head>
            <body style="margin:0; padding:0; background:#f4f1fb; font-family:Arial, Helvetica, sans-serif;">
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                <tr>
                  <td align="center" style="padding:32px 12px;">
                    <table role="presentation" width="600" cellspacing="0" cellpadding="0" border="0" style="width:600px; max-width:600px; background:#ffffff;">
                      <tr>
                        <td style="padding:32px;">
                          <h1 style="margin:0; font-size:26px;">Ola {{nomeSeguro}}</h1>
                          <p style="font-size:15px; line-height:24px;">Mensagem principal.</p>
                          <a href="{{linkSeguro}}" style="display:inline-block; background:#6f5af0; color:#ffffff; padding:14px 22px; border-radius:10px; text-decoration:none;">
                            Acao principal
                          </a>
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }
}
```

Regras para templates:

- sempre escapar dados recebidos de usuario com `WebUtility.HtmlEncode`;
- usar tabelas HTML para estrutura principal;
- usar CSS inline para estilos essenciais;
- incluir `meta viewport` para mobile;
- manter largura maxima entre `560px` e `640px`;
- usar `word-break:break-all` em links longos;
- evitar JavaScript, formularios, fontes externas obrigatorias e CSS complexo;
- incluir fallback textual visivel para links e codigos;
- manter o HTML completo iniciando com `<!doctype html>`.

### Estrutura recomendada de template

Use uma estrutura consistente:

```text
preheader invisivel
wrapper externo com background
container central de 560-640px
header com marca
conteudo principal
CTA principal
fallback de link/codigo
aviso de seguranca ou contexto
suporte
footer legal/copyright
```

### Preheader

O preheader e o texto que muitos clientes exibem ao lado do assunto. Ele deve ficar escondido no corpo, mas presente no HTML.

```html
<div style="display:none; max-height:0; overflow:hidden; opacity:0; color:transparent;">
  Confirme seu cadastro no Glow Up Connect.
</div>
```

### Botao

Use link `<a>` com estilo inline. Nao use `<button>`, porque clientes de e-mail nao tratam botao como navegacao confiavel.

```html
<a href="{{linkSeguro}}" target="_blank" style="display:inline-block; background:#6f5af0; color:#ffffff; text-decoration:none; padding:14px 24px; border-radius:10px; font-weight:700;">
  Confirmar e-mail
</a>
```

### Codigo de confirmacao

Para codigos, use fonte monoespacada, alto contraste e centralizacao.

```html
<div style="font-size:36px; line-height:44px; font-weight:800; letter-spacing:10px; font-family:'Courier New', Courier, monospace; text-align:center;">
  {{codigoSeguro}}
</div>
```

### Links longos

Sempre permitir quebra:

```html
<a href="{{linkSeguro}}" style="word-break:break-all;">{{linkSeguro}}</a>
```

### Responsividade

Inclua `meta viewport` e uma media query simples:

```html
<meta name="viewport" content="width=device-width, initial-scale=1">
<style>
  @media only screen and (max-width: 620px) {
    .email-shell { width: 100% !important; }
    .content-pad { padding: 28px 22px !important; }
  }
</style>
```

Mesmo com media query, estilos essenciais devem ficar inline. Alguns clientes ignoram `<style>`.

### Acessibilidade e legibilidade

- Use texto claro e direto.
- Mantenha contraste suficiente entre texto e fundo.
- Nao dependa apenas de cor para status; inclua texto explicativo.
- Evite paragrafos longos.
- Use tamanho minimo aproximado de `12px` para texto auxiliar e `14px/15px` para corpo.

## Template atual de confirmacao de e-mail

O template de confirmacao fica em:

```text
src/GLOWAPI.Application/Mensageria/ConfirmacaoEmailTemplate.cs
```

Uso atual:

```csharp
var conteudo = ConfirmacaoEmailTemplate.Criar(
    usuario.Nome,
    link,
    codigoPlano,
    _authOptions.ConfirmacaoEmailHoras);
```

Ele ja possui:

- card centralizado e responsivo;
- botao de confirmacao;
- codigo em destaque;
- aviso de seguranca;
- secao de suporte;
- footer;
- estados visuais para codigo ativo, expirado, invalido e confirmacao realizada.

Arquivos relacionados:

| Arquivo | Papel |
|---|---|
| `ConfirmacaoEmailTemplate.cs` | Gera o HTML transacional |
| `ConfirmacaoEmailService.cs` | Gera token/codigo e enfileira e-mail |
| `ProvedorMensagemEmail.cs` | Envia HTML pelo Resend |
| `ConfirmacaoEmailTemplateTests.cs` | Testa estrutura visual e estados |
| `ConfirmacaoEmailServiceTests.cs` | Testa que o service enfileira o template |

Estados disponiveis:

```csharp
ConfirmacaoEmailEstado.AguardandoConfirmacao
ConfirmacaoEmailEstado.CodigoExpirado
ConfirmacaoEmailEstado.CodigoInvalido
ConfirmacaoEmailEstado.ConfirmacaoRealizada
```

Exemplo:

```csharp
var html = ConfirmacaoEmailTemplate.Criar(
    nome: "Maria",
    linkConfirmacao: "https://app.glowupconnect.com/confirmar-email?token=...",
    codigo: "482913",
    validadeHoras: 24,
    estado: ConfirmacaoEmailEstado.AguardandoConfirmacao);
```

Observacao: hoje os estados visuais alternativos existem no builder para reaproveitamento futuro. O e-mail enviado pelo fluxo de cadastro usa o estado `AguardandoConfirmacao`.

## Como criar uma nova notificacao transacional

1. Crie o template em `GLOWAPI.Application/Mensageria`, se precisar de HTML.
2. Injete `IMensagemNotificacaoService` no service de negocio.
3. Monte o conteudo usando o template ou texto simples.
4. Chame `RegistrarAsync`.
5. Adicione testes unitarios cobrindo o template e o service que enfileira.

Exemplo:

```csharp
var html = LembreteAgendamentoTemplate.Criar(
    cliente.Nome,
    profissional.NomePublico,
    agendamentoInicio);

await _mensagemNotificacaoService.RegistrarAsync(new RegistrarMensagemNotificacaoDto
{
    Canal = CanalMensagemNotificacao.Email,
    Destinatario = cliente.Email,
    Assunto = "Lembrete do seu agendamento",
    Conteudo = html,
    EstabelecimentoId = agendamento.EstabelecimentoId,
    Prioridade = 1,
    AgendadoPara = agendamentoInicio.AddHours(-24)
}, cancellationToken);
```

### Exemplo: recuperacao de senha

Fluxo recomendado:

```text
AuthService/ForgotPassword
  -> gera token seguro e salva hash
  -> monta link com Auth:FrontendBaseUrl
  -> monta template HTML
  -> enfileira mensagem com prioridade 3
```

Exemplo de chamada:

```csharp
var link = $"{_authOptions.FrontendBaseUrl.TrimEnd('/')}/resetar-senha?token={Uri.EscapeDataString(tokenPlano)}";
var html = RecuperacaoSenhaTemplate.Criar(usuario.Nome, link, validadeMinutos: 30);

await _mensagemNotificacaoService.RegistrarAsync(new RegistrarMensagemNotificacaoDto
{
    Canal = CanalMensagemNotificacao.Email,
    Destinatario = usuario.Email,
    Assunto = "Redefina sua senha",
    Conteudo = html,
    Prioridade = 3,
    MaximoTentativas = 3
}, cancellationToken);
```

### Exemplo: lembrete multicanal futuro

Quando WhatsApp/SMS forem implementados de verdade, o service de negocio pode enfileirar mais de uma mensagem para o mesmo evento:

```csharp
await _mensagemNotificacaoService.RegistrarAsync(new RegistrarMensagemNotificacaoDto
{
    Canal = CanalMensagemNotificacao.Email,
    Destinatario = cliente.Email,
    Assunto = "Lembrete do seu agendamento",
    Conteudo = htmlEmail,
    Prioridade = 1,
    AgendadoPara = envioEm
}, cancellationToken);

await _mensagemNotificacaoService.RegistrarAsync(new RegistrarMensagemNotificacaoDto
{
    Canal = CanalMensagemNotificacao.Sms,
    Destinatario = cliente.Telefone,
    Conteudo = "Glow Up Connect: voce tem um agendamento amanha as 14h.",
    Prioridade = 1,
    AgendadoPara = envioEm
}, cancellationToken);
```

## Como cancelar uma mensagem

Use `CancelarPorGuidAsync` quando a mensagem ainda puder ser cancelada.

```csharp
await _mensagemNotificacaoService.CancelarPorGuidAsync(mensagemGuid, cancellationToken);
```

Regras:

- mensagens `Pendente` e `Reprocessar` podem ser canceladas;
- mensagens `Enviado`, `Falhou` ou `Cancelado` nao devem ser reenviadas/canceladas manualmente sem regra explicita;
- tentar cancelar mensagem enviada gera conflito.

Tambem existe endpoint:

```http
DELETE /api/mensagens-notificacao/{guid}
```

### Quando cancelar

Casos comuns:

- agendamento cancelado antes do lembrete ser enviado;
- usuario alterou e-mail antes da confirmacao;
- acao foi substituida por uma mensagem mais nova;
- estabelecimento desativou uma campanha ou comunicado.

Para cancelar, guarde o `Guid` retornado por `RegistrarAsync` quando a regra de negocio precisar referenciar a mensagem depois.

## Como implementar um novo provedor

Crie uma classe em:

```text
src/GLOWAPI.Infrastructure/Mensageria/Provedores/
```

Implemente `IProvedorMensagem`:

```csharp
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Mensageria;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;

public class ProvedorMensagemPush : IProvedorMensagem
{
    public CanalMensagemNotificacao CanalSuportado => CanalMensagemNotificacao.Push;

    public async Task<ResultadoEnvioMensagem> EnviarAsync(
        MensagemNotificacao mensagem,
        CancellationToken cancellationToken = default)
    {
        // Chamar SDK/API externa aqui.
        return new ResultadoEnvioMensagem(
            Sucesso: true,
            RequestPayload: "{}",
            ResponsePayload: "{}",
            RespostaProvedor: "provider-id",
            MensagemErro: null,
            TempoExecucaoMs: 10);
    }
}
```

Registre em `src/GLOWAPI.Infrastructure/DependencyInjection.cs`:

```csharp
services.AddScoped<IProvedorMensagem, ProvedorMensagemPush>();
```

Se for um canal novo, tambem sera necessario:

- adicionar o valor em `CanalMensagemNotificacao`;
- criar migration se o mapeamento do enum exigir ajuste no banco;
- adicionar options de configuracao;
- adicionar testes unitarios do provedor;
- documentar variaveis de ambiente.

### Como o resolver escolhe o provedor

O `ProvedorMensagemResolver` recebe todos os `IProvedorMensagem` registrados no DI e procura um provedor compativel com `CanalSuportado`.

Implicacoes:

- deve existir exatamente um provedor padrao por canal, a menos que o resolver seja evoluido;
- se registrar dois provedores para o mesmo canal sem regra adicional, pode haver ambiguidade;
- `Provedor` no DTO deve ser usado apenas quando a resolucao por nome for implementada ou quando o provedor atual tratar esse campo.

### Payloads e logs do provedor

Ao retornar `ResultadoEnvioMensagem`, preencha:

| Campo | Como usar |
|---|---|
| `Sucesso` | `true` somente quando o provedor aceitou o envio |
| `RequestPayload` | JSON sem segredo com dados enviados ao provedor |
| `ResponsePayload` | JSON/resumo da resposta do provedor |
| `RespostaProvedor` | ID externo da mensagem, quando existir |
| `MensagemErro` | Mensagem de erro legivel quando falhar |
| `TempoExecucaoMs` | Tempo total da chamada externa |

Nao inclua tokens de API, headers de autorizacao, senhas ou codigo secreto em payloads de log.

## Configuracao

Configuracao principal:

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

Configuracao de e-mail:

```json
"Mensageria:Email": {
  "Provedor": "resend",
  "Habilitado": true,
  "From": "Glow Up Connect <noreply@seudominio.com>"
}
```

Variaveis/segredos:

```bash
RESEND_APITOKEN="re_..."
Mensageria__Email__From="Glow Up Connect <noreply@seudominio.com>"
Mensageria__Email__Habilitado="true"
Auth__FrontendBaseUrl="https://app.glowupconnect.com"
```

Em desenvolvimento local, prefira User Secrets:

```bash
dotnet user-secrets set "RESEND_APITOKEN" "re_..." --project src/GLOWAPI.API
dotnet user-secrets set "Mensageria:Email:From" "Glow Up Connect <noreply@seudominio.com>" --project src/GLOWAPI.API
dotnet user-secrets set "Mensageria:Email:Habilitado" "true" --project src/GLOWAPI.API
```

### Ambientes

| Ambiente | Recomendacao |
|---|---|
| Development | Pode deixar `Mensageria:Email:Habilitado=false` para nao enviar e-mails reais |
| Testing | Manter `Mensageria:Habilitado=false` nos testes de integracao |
| Staging | Habilitar e-mail com dominio/remetente de homologacao |
| Production | Habilitar e-mail com API key e remetente de producao |

### Variaveis obrigatorias para e-mail real

Para e-mail via Resend funcionar:

- `Mensageria:Email:Habilitado=true`;
- `Mensageria:Email:From` preenchido;
- `RESEND_APITOKEN` preenchido;
- dominio/remetente verificado no Resend;
- worker ativo com `Mensageria:Habilitado=true`.

## Testes recomendados

Para templates:

- conferir que o HTML inicia com `<!doctype html>`;
- conferir que dados dinamicos sao escapados;
- conferir presenca do CTA principal;
- conferir codigo/link em destaque;
- conferir estados visuais, quando existirem.

Exemplo:

```csharp
[Fact]
public void Criar_DeveEscaparNomeDoUsuario()
{
    var html = MeuTemplateEmail.Criar("Joao <script>", "https://app.test");

    html.Should().Contain("Joao &lt;script&gt;");
}
```

Para services de negocio:

- mockar `IMensagemNotificacaoService`;
- capturar o `RegistrarMensagemNotificacaoDto`;
- validar `Canal`, `Destinatario`, `Assunto`, `Conteudo`, `Prioridade` e `AgendadoPara`.

Exemplo:

```csharp
RegistrarMensagemNotificacaoDto? mensagem = null;
_mensagemService
    .Setup(m => m.RegistrarAsync(It.IsAny<RegistrarMensagemNotificacaoDto>(), It.IsAny<CancellationToken>()))
    .Callback<RegistrarMensagemNotificacaoDto, CancellationToken>((dto, _) => mensagem = dto);

await service.ExecutarAsync(cancellationToken);

mensagem.Should().NotBeNull();
mensagem!.Canal.Should().Be(CanalMensagemNotificacao.Email);
mensagem.Conteudo.Should().Contain("<!doctype html>");
```

Para provedores:

- mockar SDK/API externa;
- validar payload, idempotency key quando existir e tratamento de erro;
- validar retorno `ResultadoEnvioMensagem`.

Rodar:

```powershell
dotnet test
```

## Troubleshooting

### Mensagem ficou `Pendente`

Verifique:

- `Mensageria:Habilitado` esta `true`;
- API esta rodando com workers registrados em `Program.cs`;
- `AgendadoPara` nao esta no futuro;
- banco usado pela API e o mesmo onde a mensagem foi criada;
- worker nao esta falhando no log da aplicacao.

### Mensagem ficou `Reprocessar`

Verifique:

- erro em `MensagemNotificacao.MensagemErro`;
- ultimos registros de `MensagensNotificacaoLogs`;
- configuracao do provedor;
- credenciais e variaveis de ambiente;
- se o destino e valido.

### E-mail nao envia

Verifique:

- `Mensageria:Email:Habilitado=true`;
- `RESEND_APITOKEN` existe no ambiente;
- `Mensageria:Email:From` existe e usa remetente verificado;
- conta/dominio no Resend esta ativo;
- logs de `ProvedorMensagemEmail`.

### HTML apareceu como texto

O conteudo provavelmente nao comeca com `<!doctype html>` ou `<html`.

Garanta:

```csharp
return """
    <!doctype html>
    <html lang="pt-BR">
    ...
    </html>
    """;
```

### Link do e-mail aponta para lugar errado

Verifique `Auth:FrontendBaseUrl`.

Em Railway:

```bash
Auth__FrontendBaseUrl="https://seu-front.com"
```

### Testes falham no Windows com Event Log

Em alguns ambientes sandbox, testes de integracao podem falhar com:

```text
Cannot open log for source '.NET Runtime'
```

Isso e ambiental. Rode `dotnet test` fora do sandbox ou ajuste providers de logging no teste, se a correcao fizer parte da tarefa.

## Checklist antes de abrir PR

- A notificacao passa pela fila, nao pelo provedor direto.
- O template escapa dados dinamicos.
- O HTML usa tabelas e estilos inline.
- Texto simples continua funcionando quando nao houver template.
- Variaveis de ambiente novas estao documentadas.
- Testes cobrem template/service/provedor conforme o escopo.
- `dotnet test` passa.
