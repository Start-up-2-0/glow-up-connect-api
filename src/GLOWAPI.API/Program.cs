using GLOWAPI.Infrastructure;
// using GLOWAPI.Application; // Descomente quando criar o DependencyInjection da Application

var builder = WebApplication.CreateBuilder(args);

// 1. Adicionar serviços ao container
builder.Services.AddControllers();

// Configura o Swagger/OpenAPI para documentação
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 2. Chamar os métodos de extensão das outras camadas
// Isso mantém o Program.cs limpo e focado na Web API
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.EnvironmentName);
// builder.Services.AddApplication(); // Registrará MediatR, AutoMapper, etc.

var app = builder.Build();

// 3. Configurar o pipeline de requisições HTTP (Middleware)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "GLOWAPI v1");
    });
}

// Redirecionamento HTTPS e Autorização
app.UseHttpsRedirection();

app.UseAuthorization();

// Mapeia os controllers para as rotas
app.MapControllers();

app.Run();
