namespace GLOWAPI.Application.DTOs.Estabelecimentos;

public class ConfirmarWhatsAppEstabelecimentoRequestDto
{
    public string Codigo { get; set; } = string.Empty;
}

public class WhatsAppOptInEstabelecimentoRequestDto
{
    public bool OptIn { get; set; }
}
