using System.ComponentModel.DataAnnotations;

namespace GLOWAPI.Application.DTOs.Auth;

public class ConfirmarEmailRequestDto
{
    public string? Token { get; set; }

    public string? Codigo { get; set; }
}
