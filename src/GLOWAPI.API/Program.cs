using System.Text.Json.Serialization;
using GLOWAPI.Application;
using GLOWAPI.Application.Options;
using GLOWAPI.API.Configuration;
using GLOWAPI.API.Middlewares;
using GLOWAPI.API.Workers;
using GLOWAPI.Infrastructure;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

ProxyOriginConfiguration.ConfigurarProxyOrigin(builder);
RequestProofConfiguration.ConfigurarRequestProof(builder);
KestrelMtlsConfiguration.ConfigurarKestrel(builder);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.EnvironmentName);
builder.Services.AddApplication();
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));
builder.Services.Configure<AvatarOptions>(builder.Configuration.GetSection(AvatarOptions.SectionName));
builder.Services.Configure<MensageriaOptions>(builder.Configuration.GetSection(MensageriaOptions.SectionName));
builder.Services.Configure<MensageriaEmailOptions>(
    builder.Configuration.GetSection(MensageriaEmailOptions.SectionName));
builder.Services.Configure<MensageriaWhatsAppOptions>(
    builder.Configuration.GetSection(MensageriaWhatsAppOptions.SectionName));
builder.Services.AddOptions<MercadoPagoOptions>()
    .Bind(builder.Configuration.GetSection(MercadoPagoOptions.SectionName))
    .PostConfigure(options => MercadoPagoCheckoutProUrlDefaults.Aplicar(options, builder.Configuration));
builder.Services.Configure<WebhookPagamentoOptions>(
    builder.Configuration.GetSection(WebhookPagamentoOptions.SectionName));
builder.Services.Configure<GeocodificacaoOptions>(
    builder.Configuration.GetSection(GeocodificacaoOptions.SectionName));
builder.Services.Configure<AssinaturaCobrancaOptions>(
    builder.Configuration.GetSection(AssinaturaCobrancaOptions.SectionName));
builder.Services.Configure<AssinaturaCobrancaWorkerOptions>(
    builder.Configuration.GetSection(AssinaturaCobrancaWorkerOptions.SectionName));
builder.Services.Configure<ExclusaoContaOptions>(
    builder.Configuration.GetSection(ExclusaoContaOptions.SectionName));
builder.Services.Configure<AvaliacaoAgregadoWorkerOptions>(
    builder.Configuration.GetSection(AvaliacaoAgregadoWorkerOptions.SectionName));
builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection(CorsOptions.SectionName));
builder.Services.Configure<RateLimitOptions>(builder.Configuration.GetSection(RateLimitOptions.SectionName));
builder.Services.Configure<CaptchaOptions>(builder.Configuration.GetSection(CaptchaOptions.SectionName));
builder.Services.Configure<SwaggerOptions>(builder.Configuration.GetSection(SwaggerOptions.SectionName));
var corsOrigins = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>()?.AllowedOrigins
    ?? CorsOptions.DefaultOrigins;

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

HostedConfigurationValidator.ValidarSeAmbienteHospedado(
    builder.Configuration,
    builder.Environment.EnvironmentName);

builder.Services.AddHostedService<MensagemNotificacaoWorker>();
builder.Services.AddHostedService<MensagemNotificacaoRecuperacaoWorker>();
builder.Services.AddHostedService<AssinaturaCobrancaWorker>();
builder.Services.AddHostedService<AvaliacaoAgregadoWorker>();
builder.Services.AddHostedService<GLOWAPI.Application.Services.ContasVencimentoBackgroundService>();
builder.Services.AddHostedService<RetencaoDadosWorker>();
builder.Services.AddHostedService<CompactacaoImagensWorker>();

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("GlowToken", new OpenApiSecurityScheme
    {
        Name = "x-glow-token",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "Fallback legado. Preferencial: cookie HttpOnly guc_access (Path=/api) + withCredentials"
    });

    options.AddSecurityDefinition("GlowAccessCookie", new OpenApiSecurityScheme
    {
        Name = "guc_access",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Cookie,
        Description = "Access token em cookie HttpOnly (emitido no login/refresh)"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "GlowAccessCookie"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

var mtlsStartup = MtlsOptionsResolver.Resolver(app.Configuration);
if (mtlsStartup.Enabled)
{
    app.Logger.LogInformation(
        "GlowAPI mTLS ativo: HTTP publico na porta {PublicPort}, mTLS na porta {MutualTlsPort}",
        mtlsStartup.PublicPort,
        mtlsStartup.MutualTlsPort);
}
else
{
    app.Logger.LogWarning(
        "GlowAPI mTLS inativo: apenas HTTP na porta {PublicPort}",
        mtlsStartup.PublicPort);
}

var migrateOnly = args.Any(static a => string.Equals(a, "--migrate-only", StringComparison.OrdinalIgnoreCase));

await app.ApplyPendingMigrationsAsync(force: migrateOnly);

if (migrateOnly)
{
    app.Logger.LogInformation("Modo --migrate-only concluido; encerrando sem iniciar a API.");
    return;
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<PublicPortPathGuardMiddleware>();
app.UseMiddleware<ProxyOriginMiddleware>();
app.UseMiddleware<RequestProofMiddleware>();
app.UseMiddleware<IpBurstRateLimitMiddleware>();

// Railway termina TLS no edge; mTLS interno fica na :8443. Redirect HTTP->HTTPS quebraria o healthcheck em /health.
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseRouting();

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseCors("Frontend");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "GLOWAPI v1"));
}
else if (app.Environment.IsStaging())
{
    app.UseMiddleware<SwaggerAccessMiddleware>();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "GLOWAPI v1"));
}

app.UseMiddleware<GlowTokenAuthenticationMiddleware>();
app.UseMiddleware<EmailConfirmationAccessMiddleware>();
app.UseMiddleware<PermissionMiddleware>();
app.MapControllers();

app.Run();

public partial class Program;
