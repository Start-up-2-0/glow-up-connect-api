using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.DTOs.Mensageria;

public class MensagemNotificacaoResponseDto
{
    public Guid Guid { get; set; }
    public StatusMensagemNotificacao Status { get; set; }
    public CanalMensagemNotificacao Canal { get; set; }
    public DateTime CriadoEm { get; set; }

    public static MensagemNotificacaoResponseDto From(MensagemNotificacao mensagem) => new()
    {
        Guid = mensagem.Guid,
        Status = mensagem.Status,
        Canal = mensagem.Canal,
        CriadoEm = mensagem.CriadoEm
    };
}
