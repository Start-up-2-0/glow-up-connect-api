namespace GLOWAPI.Application.Options;

public class AssinaturaCobrancaOptions
{
    public const string SectionName = "AssinaturaCobranca";

    public int DiasAntecedenciaGeracaoCobranca { get; set; } = 7;
    public int DiasAntecedenciaAlertaFatura { get; set; } = 7;
    public int DiasToleranciaInadimplencia { get; set; } = 10;

    /// <summary>
    /// Quando true, a promocao de lancamento (trial 14 dias) fica indisponivel
    /// mesmo com campanha ativa no banco. Util em staging para testar cobranca imediata.
    /// </summary>
    public bool DesativarPromocaoLancamento { get; set; }

    /// <summary>
    /// Validade do link de checkout inicial (Checkout Pro) em minutos, no horário de Brasília.
    /// Em sandbox o Glow usa no mínimo 30 minutos: 5 minutos estoura no desafio/login do MP.
    /// </summary>
    public int MinutosExpiracaoCheckout { get; set; } = 5;
}
