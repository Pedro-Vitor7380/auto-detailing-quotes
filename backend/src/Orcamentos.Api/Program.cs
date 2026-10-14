using Orcamentos.Api.Comandos;
using Orcamentos.Api.Features.Auth;
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
builder.Services.AddAutenticacao();

var app = builder.Build();

// ===== Comandos de terminal (executam e encerram, sem subir o servidor web) =====
if (ehComando)
    return await CriarUsuarioComando.ExecutarAsync(app.Services);

// ===== 2. Pipeline: o caminho que cada requisição HTTP percorre =====
app.UseAuthentication();   // lê o token do cabeçalho Authorization e descobre QUEM é o usuário
app.UseAuthorization();    // decide se esse usuário PODE acessar o endpoint

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();              // gera /openapi/v1.json (a descrição da API)
    app.MapScalarApiReference().AllowAnonymous();   // página interativa em /scalar para testar a API
}

// ===== 3. Endpoints =====
app.MapGet("/api/health", async (AppDbContext db, CancellationToken ct) =>
    {
        var bancoOk = await db.Database.CanConnectAsync(ct);
        var resposta = new { status = bancoOk ? "ok" : "erro", banco = bancoOk ? "ok" : "inacessível", horario = DateTimeOffset.Now };

        return bancoOk ? Results.Ok(resposta) : Results.Json(resposta, statusCode: StatusCodes.Status503ServiceUnavailable);
    })
    .AllowAnonymous()
    .WithName("Health")
    .WithSummary("Verifica se a API e o banco de dados estão no ar");

app.MapAuthEndpoints();

await app.RunAsync();
return 0;
