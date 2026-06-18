namespace GLOWAPI.Application.DTOs.Mensageria;

public class WhatsAppConfirmacaoInstrucoesDto
{
    public string NumeroPlataforma { get; set; } = string.Empty;
    public string TokenConfirmacao { get; set; } = string.Empty;
    public string LinkConfirmacao { get; set; } = string.Empty;
    public string LinkWhatsApp { get; set; } = string.Empty;
    public bool WhatsAppEnviado { get; set; }
    public bool EmailEnviado { get; set; }
}
