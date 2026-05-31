using System.ComponentModel.DataAnnotations;

namespace GLOWAPI.Application.DTOs.Auth;

public class ConfirmarWhatsAppRequestDto
{
    public string? Token { get; set; }

    public string? Codigo { get; set; }

    public string? Telefone { get; set; }
}
