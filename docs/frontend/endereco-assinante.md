# Endereco completo do assinante

Documentacao para o painel do assinante (estabelecimento ou profissional autonomo) preencher endereco completo e habilitar visibilidade na busca por proximidade.

---

## Campos obrigatorios

O backend exige endereco completo nos DTOs de perfil e assinatura:

| Campo | Obrigatorio | Observacao |
|-------|-------------|------------|
| `cep` | sim | 8 digitos (com ou sem hifen) |
| `logradouro` | sim | Rua, avenida etc. |
| `numero` | sim | Numero do imovel |
| `bairro` | sim | |
| `cidade` | sim | |
| `estado` | sim | UF com 2 caracteres (ex.: `SP`) |
| `complemento` | nao | Sala, bloco, andar |

**Breaking change:** o campo `local` foi removido. Use `logradouro`.

---

## Fluxo recomendado com ViaCEP

1. Assinante digita o CEP.
2. Frontend consulta ViaCEP e preenche logradouro, bairro, cidade e UF.
3. Assinante informa apenas **numero** e **complemento** (opcional).
4. Envia o perfil para a API.
5. Backend geocodifica automaticamente (Nominatim) e grava lat/lng — **sem campo de coordenadas no formulario**.

```javascript
async function buscarCep(cep) {
  const apenasDigitos = cep.replace(/\D/g, "");
  if (apenasDigitos.length !== 8) return null;

  const response = await fetch(`https://viacep.com.br/ws/${apenasDigitos}/json/`);
  const data = await response.json();
  if (data.erro) return null;

  return {
    logradouro: data.logradouro ?? "",
    bairro: data.bairro ?? "",
    cidade: data.localidade ?? "",
    estado: data.uf ?? "",
  };
}
```

---

## Endpoints que recebem endereco

| Fluxo | Metodo | Rota |
|-------|--------|------|
| Atualizar perfil estabelecimento | PUT | `/api/estabelecimentos/{id}/perfil` |
| Atualizar perfil autonomo | PUT | `/api/profissionais-autonomos/{id}/perfil` |
| Criar assinatura + estabelecimento | POST | `/api/assinaturas/iniciar` (body `estabelecimento.endereco`) |
| Criar assinatura + autonomo | POST | `/api/assinaturas/iniciar` (body `profissionalAutonomo.endereco`) |

### Exemplo de payload (`endereco`)

```json
{
  "cep": "13010-000",
  "logradouro": "Rua Barao de Jaguara",
  "numero": "100",
  "bairro": "Centro",
  "cidade": "Campinas",
  "estado": "SP",
  "complemento": "Sala 2"
}
```

### Resposta (`endereco` no perfil)

```json
{
  "cep": "13010000",
  "logradouro": "Rua Barao de Jaguara",
  "numero": "100",
  "bairro": "Centro",
  "cidade": "Campinas",
  "estado": "SP",
  "complemento": "Sala 2",
  "enderecoCompleto": true
}
```

- `enderecoCompleto: true` — endereco valido e geocodificado; aparece na busca por proximidade.
- `enderecoCompleto: false` — perfil salvo, mas ainda nao visivel na descoberta (CEP/endereco incompleto ou geocode pendente/falhou).

---

## Erros de validacao

Erros retornam HTTP 400 com `code` especifico do fluxo (ex.: validacao de assinatura) ou mensagem descritiva.

Exemplos de mensagens:

- `CEP e obrigatorio.`
- `CEP deve conter 8 digitos.`
- `Estado deve ser a UF com 2 caracteres.`

---

## UX sugerida

- Exibir aviso quando `enderecoCompleto === false`: *"Complete seu endereco para aparecer na busca por proximidade dos clientes."*
- Bloquear submit ate CEP, logradouro, numero, bairro, cidade e UF estarem preenchidos.
- Nao exibir campos de latitude/longitude para o assinante.
