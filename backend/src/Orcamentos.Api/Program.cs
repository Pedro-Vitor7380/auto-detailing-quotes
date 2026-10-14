using Orcamentos.Infrastructure;
using Orcamentos.Infrastructure.Dados;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ===== 1. Registro de serviços (injeção de dependência) =====
builder.Services.AddOpenApi();
builder.Services.AddInfraestrutura(builder.Configuration);

var app = builder.Build();

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

app.Run();
