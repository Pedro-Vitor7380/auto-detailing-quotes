using Orcamentos.Api.Comandos;
using Orcamentos.Infrastructure;
using Orcamentos.Infrastructure.Dados;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var ehComando = args.FirstOrDefault() == CriarUsuarioComando.Nome;

// Em modo comando, esconde os logs informativos (ex.: SQL do EF) para não poluir as perguntas.
// AddFilter (e não SetMinimumLevel), porque os níveis do appsettings.json teriam prioridade.
if (ehComando)
    builder.Logging.AddFilter(nivel => nivel >= LogLevel.Warning);

// ===== 1. Registro de serviços (injeção de dependência) =====
builder.Services.AddOpenApi();
builder.Services.AddInfraestrutura(builder.Configuration);

var app = builder.Build();

// ===== Comandos de terminal (executam e encerram, sem subir o servidor web) =====
if (ehComando)
    return await CriarUsuarioComando.ExecutarAsync(app.Services);

// ===== 2. Pipeline: o caminho que cada requisição HTTP percorre =====
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();              // gera /openapi/v1.json (a descrição da API)
    app.MapScalarApiReference();   // página interativa em /scalar para testar a API
}

// ===== 3. Endpoints =====
app.MapGet("/api/health", async (AppDbContext db, CancellationToken ct) =>
    {
        var bancoOk = await db.Database.CanConnectAsync(ct);
        var resposta = new { status = bancoOk ? "ok" : "erro", banco = bancoOk ? "ok" : "inacessível", horario = DateTimeOffset.Now };

        return bancoOk ? Results.Ok(resposta) : Results.Json(resposta, statusCode: StatusCodes.Status503ServiceUnavailable);
    })
    .WithName("Health")
    .WithSummary("Verifica se a API e o banco de dados estão no ar");

await app.RunAsync();
return 0;
