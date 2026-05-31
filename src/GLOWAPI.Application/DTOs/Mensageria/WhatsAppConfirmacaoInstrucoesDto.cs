namespace GLOWAPI.Application.DTOs.Mensageria;

public class WhatsAppConfirmacaoInstrucoesDto
{
    public string NumeroPlataforma { get; set; } = string.Empty;
    public string CodigoConfirmacao { get; set; } = string.Empty;
    public string MensagemSugerida { get; set; } = string.Empty;
    public string LinkWhatsApp { get; set; } = string.Empty;
    public bool EmailEnviado { get; set; }
    public DateTime ExpiraEm { get; set; }
}
