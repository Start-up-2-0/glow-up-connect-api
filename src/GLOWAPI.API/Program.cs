using System.Text.Json.Serialization;
using GLOWAPI.Application;
using GLOWAPI.Application.Options;
using GLOWAPI.API.Configuration;
using GLOWAPI.API.Middlewares;
using GLOWAPI.API.Workers;
using GLOWAPI.Infrastructure;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.Configure<GeocodificacaoOptions>(
    builder.Configuration.GetSection(GeocodificacaoOptions.SectionName));
builder.Services.Configure<AssinaturaCobrancaOptions>(
    builder.Configuration.GetSection(AssinaturaCobrancaOptions.SectionName));
builder.Services.Configure<AssinaturaCobrancaWorkerOptions>(
    builder.Configuration.GetSection(AssinaturaCobrancaWorkerOptions.SectionName));
builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection(CorsOptions.SectionName));
builder.Services.Configure<RateLimitOptions>(builder.Configuration.GetSection(RateLimitOptions.SectionName));
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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("GlowToken", new OpenApiSecurityScheme
    {
        Name = "x-glow-token",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "Token de autenticação customizado emitido no login"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "GlowToken"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

await app.ApplyPendingMigrationsAsync();

app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<IpBurstRateLimitMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts();
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
