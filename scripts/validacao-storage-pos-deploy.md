# Validação pós-deploy — redução de storage

Execute após publicar a versão com compactação de imagens e retenção de 30 dias.

## 1. Baseline (antes ou logo após deploy)

Conecte ao MySQL de staging/produção e rode:

```bash
mysql -h <host> -u <user> -p <database> < scripts/diagnostico-storage.sql
```

Guarde o resultado (principalmente `total_mb` das imagens e `size_mb` por tabela).

## 2. Recompactação única do legado

No Railway (ou variáveis de ambiente da API), habilite temporariamente:

```env
CompactacaoImagensWorker__Habilitado=true
CompactacaoImagensWorker__TamanhoLote=50
CompactacaoImagensWorker__IntervaloProcessamentoMs=60000
```

Monitore os logs até aparecer:

`Worker de compactacao de imagens concluiu varredura completa.`

Depois **desabilite**:

```env
CompactacaoImagensWorker__Habilitado=false
```

## 3. Retenção contínua

Confirme que está ativo (default):

```env
RetencaoDados__Habilitado=true
RetencaoDados__DiasRetencao=30
RetencaoDados__IntervaloProcessamentoMs=3600000
```

## 4. Comparar resultado

Rode novamente `scripts/diagnostico-storage.sql` e compare:

- `total_mb` em Usuarios / Estabelecimentos / Profissionais
- `webhook_payload_mb` e `logs_notificacao_mb` após 1–2 ciclos do worker de retenção

## 5. Novos uploads

Novas fotos passam a respeitar:

- `Avatar__MaxSizeBytes=524288` (512 KB)
- `Avatar__PersistenciaMaxLadoPx=384`
- `Avatar__PersistenciaQualidadeJpeg=72`
