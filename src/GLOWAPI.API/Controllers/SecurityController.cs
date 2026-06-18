using GLOWAPI.API.Helpers;
using GLOWAPI.API.Models;
using GLOWAPI.Application.DTOs.Security;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Security;
using GLOWAPI.Application.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace GLOWAPI.API.Controllers;

[ApiController]
[Route("api/security")]
public class SecurityController : ControllerBase
{
    private readonly IRequestProofService _requestProofService;
    private readonly RequestProofOptions _options;
    private readonly IWebHostEnvironment _environment;

    public SecurityController(
        IRequestProofService requestProofService,
        IOptions<RequestProofOptions> options,
        IWebHostEnvironment environment)
    {
        _requestProofService = requestProofService;
        _options = options.Value;
        _environment = environment;
    }

    [AllowAnonymous]
    [HttpGet("request-proof")]
    public IActionResult EmitirRequestProof(
        [FromQuery] string? m,
        [FromQuery] string? p,
        [FromQuery] int count = 0)
    {
        if (!_options.Enabled)
        {
            return Ok(ApiSuccessResponse<RequestProofBootstrapDto>.From(
                "Request proof desabilitado.",
                new RequestProofBootstrapDto()));
        }

        var contextId = RequestProofContextCookieHelper.ObterOuCriarContextId(
            Request,
            Response,
            _options,
            _environment);

        var quantidade = count > 0 ? count : _options.DefaultPoolSize;
        var proofs = _requestProofService.EmitirProofs(contextId, m, p, quantidade);

        return Ok(ApiSuccessResponse<RequestProofBootstrapDto>.From(
            "Request proofs emitidos.",
            new RequestProofBootstrapDto { Proofs = proofs }));
    }
}
