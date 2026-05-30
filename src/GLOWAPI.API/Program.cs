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
    });

builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.EnvironmentName);
builder.Services.AddApplication();
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));
builder.Services.Configure<AvatarOptions>(builder.Configuration.GetSection(AvatarOptions.SectionName));
builder.Services.Configure<MensageriaOptions>(builder.Configuration.GetSection(MensageriaOptions.SectionName));
builder.Services.Configure<MensageriaEmailOptions>(
    builder.Configuration.GetSection(MensageriaEmailOptions.SectionName));
builder.Services.Configure<MercadoPagoOptions>(
    builder.Configuration.GetSection(MercadoPagoOptions.SectionName));
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

HostedConfigurationValidator.ValidarSeAmbienteHospedado(
    builder.Configuration,
    builder.Environment.EnvironmentName);

builder.Services.AddHostedService<MensagemNotificacaoWorker>();
builder.Services.AddHostedService<MensagemNotificacaoRecuperacaoWorker>();

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

if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "GLOWAPI v1"));
}

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("Frontend");

app.UseMiddleware<GlowTokenAuthenticationMiddleware>();
app.UseMiddleware<PermissionMiddleware>();
app.MapControllers();

app.Run();

public partial class Program;
