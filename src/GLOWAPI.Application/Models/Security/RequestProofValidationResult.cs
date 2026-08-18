namespace GLOWAPI.Application.Models.Security;

public enum RequestProofFailureCode
{
    None,
    Ausente,
    Invalido,
    Expirado,
    Replay,
    ContextoInvalido,
    MetodoPathInvalido
}

public sealed class RequestProofValidationResult
{
    public bool Sucesso => Codigo == RequestProofFailureCode.None;

    public RequestProofFailureCode Codigo { get; init; }

    public static RequestProofValidationResult Ok() =>
        new() { Codigo = RequestProofFailureCode.None };

    public static RequestProofValidationResult Falha(RequestProofFailureCode codigo) =>
        new() { Codigo = codigo };
}
