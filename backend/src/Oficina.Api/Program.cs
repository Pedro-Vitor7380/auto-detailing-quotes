using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ===== 1. Registro de serviços (injeção de dependência) =====
builder.Services.AddOpenApi();

var app = builder.Build();

// ===== 2. Pipeline: o caminho que cada requisição HTTP percorre =====
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();              // gera /openapi/v1.json (a descrição da API)
    app.MapScalarApiReference();   // página interativa em /scalar para testar a API
}

// ===== 3. Endpoints =====
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", horario = DateTimeOffset.Now }))
    .WithName("Health")
    .WithSummary("Verifica se a API está no ar");

app.Run();
