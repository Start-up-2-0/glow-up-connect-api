using System.ComponentModel.DataAnnotations;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.DTOs.Mensageria;

public class RegistrarMensagemNotificacaoDto
{
    [Required]
    public CanalMensagemNotificacao Canal { get; set; }

    [Required]
    [MaxLength(500)]
    public string Destinatario { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Assunto { get; set; } = string.Empty;

    [Required]
    public string Conteudo { get; set; } = string.Empty;

    public string? PayloadJson { get; set; }

    public int? EstabelecimentoId { get; set; }

    public int Prioridade { get; set; }

    public DateTime? AgendadoPara { get; set; }

    public int? MaximoTentativas { get; set; }

    public string? Provedor { get; set; }
}
