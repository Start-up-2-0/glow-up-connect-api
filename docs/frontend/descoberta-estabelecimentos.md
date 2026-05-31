# Descoberta de estabelecimentos proximos

Documentacao para o frontend cliente listar estabelecimentos por geolocalizacao.

**Base URL:** substitua `{BASE_URL}` pela URL da API.

---

## Fluxo recomendado

1. Cliente abre a tela `/explorar`.
2. Frontend solicita permissao de localizacao (`navigator.geolocation`).
3. Com `latitude` e `longitude`, chama a API.
4. Renderiza cards ordenados por `distanciaKm`.
5. Clique no card redireciona para `/loja/{publicGuid}` e usa as rotas publicas de agendamento existentes.

---

## Endpoint

### Listar estabelecimentos proximos

```
GET {BASE_URL}/api/publico/estabelecimentos/proximos
```

**Auth:** nao requer `x-glow-token`.

| Query | Obrigatorio | Default | Descricao |
|-------|-------------|---------|-----------|
| `latitude` | sim | — | -90 a 90 |
| `longitude` | sim | — | -180 a 180 |
| `raioKm` | nao | `10` | Maximo 50 |
| `pagina` | nao | `1` | Pagina atual |
| `tamanhoPagina` | nao | `20` | Maximo 50 |

**Exemplo:**

```
GET /api/publico/estabelecimentos/proximos?latitude=-22.9056&longitude=-47.0608&raioKm=10&pagina=1&tamanhoPagina=20
```

### Resposta 200

```json
{
  "success": true,
  "message": "Estabelecimentos proximos listados com sucesso.",
  "data": {
    "cidade": "Campinas",
    "estado": "SP",
    "raioKm": 10,
    "total": 1,
    "itens": [
      {
        "publicGuid": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
        "nome": "Barbearia Glow",
        "logo": "https://cdn.exemplo/logo.png",
        "descricao": "Cortes masculinos e barba.",
        "distanciaKm": 1.42,
        "endereco": {
          "logradouro": "Rua Barao de Jaguara",
          "bairro": "Centro",
          "cidade": "Campinas",
          "estado": "SP"
        }
      }
    ]
  }
}
```

### Erros comuns

| HTTP | code | Quando |
|------|------|--------|
| 400 | `LOCALIZACAO_CLIENTE_INVALIDA` | Coordenadas invalidas ou reverse geocode falhou |
| 400 | `LOCALIZACAO_CLIENTE_INVALIDA` | `raioKm` fora do intervalo ou pagina invalida |

---

## Geolocation no browser (exemplo)

```javascript
navigator.geolocation.getCurrentPosition(
  async ({ coords }) => {
    const params = new URLSearchParams({
      latitude: String(coords.latitude),
      longitude: String(coords.longitude),
      raioKm: "10",
    });

    const response = await fetch(
      `${API_BASE_URL}/api/publico/estabelecimentos/proximos?${params}`
    );
    const body = await response.json();
    if (!body.success) throw new Error(body.message);
    renderLista(body.data.itens);
  },
  () => {
    // fallback futuro: busca manual por cidade
    mostrarErroLocalizacao();
  },
  { enableHighAccuracy: true, timeout: 10000 }
);
```

---

## Proximo passo apos selecionar loja

Use as rotas ja existentes em `/api/publico/agendar/loja/{publicGuid}/...`:

- `GET .../servicos`
- `GET .../profissionais`
- `GET .../disponibilidade`
- `POST ...` (visitante) ou `POST /api/agendamentos` (cliente logado)

---

## Observacoes

- Apenas estabelecimentos **ativos** com endereco **geocodificado** aparecem na busca.
- A API resolve a cidade do cliente via Nominatim (reverse geocode) e filtra por cidade + raio.
- Estabelecimentos com endereco incompleto nao entram na listagem ate o assinante completar o cadastro ([endereco-assinante.md](./endereco-assinante.md)).
