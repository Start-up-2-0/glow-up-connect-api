namespace GLOWAPI.Application.Models.Manutencao;

public sealed class RetencaoDadosResultado
{
    public int WebhooksRemovidos { get; set; }
    public int MensagensNotificacaoLogsRemovidos { get; set; }
    public int MensagensNotificacaoRemovidas { get; set; }
    public int LogsAutenticacaoRemovidos { get; set; }
    public int SessoesAutenticacaoRemovidas { get; set; }
    public int AssinaturasHistoricoPayloadsAnulados { get; set; }
    public int PagamentosHistoricoPayloadsAnulados { get; set; }
    public int AgendamentosHistoricoPayloadsAnulados { get; set; }

    public int TotalAfetados =>
        WebhooksRemovidos
        + MensagensNotificacaoLogsRemovidos
        + MensagensNotificacaoRemovidas
        + LogsAutenticacaoRemovidos
        + SessoesAutenticacaoRemovidas
        + AssinaturasHistoricoPayloadsAnulados
        + PagamentosHistoricoPayloadsAnulados
        + AgendamentosHistoricoPayloadsAnulados;
}
