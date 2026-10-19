using Orcamentos.Api.Comandos;
using Orcamentos.Api.Comum;
using Orcamentos.Api.Features.Auth;
using Orcamentos.Api.Features.Catalogo;
using Orcamentos.Api.Features.Clientes;
using Orcamentos.Api.Features.Orcamentos;
using Orcamentos.Infrastructure;
using Orcamentos.Infrastructure.Dados;
using System.Text.Json.Serialization;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
// Comandos de terminal: "dotnet run -- <nome>" executa uma tarefa administrativa e encerra.
Func<IServiceProvider, Task<int>>? comando = args.FirstOrDefault() switch
{
    CriarUsuarioComando.Nome => CriarUsuarioComando.ExecutarAsync,
    AlterarSenhaComando.Nome => AlterarSenhaComando.ExecutarAsync,
    AlterarOficinaComando.Nome => AlterarOficinaComando.ExecutarAsync,
    // Este recebe o caminho do arquivo (2º argumento): a lambda "encaixa" o argumento extra no mesmo formato.
    ImportarCatalogoComando.Nome => servicos => ImportarCatalogoComando.ExecutarAsync(servicos, args.ElementAtOrDefault(1)),
    _ => null,
};

// Em modo comando, esconde os logs informativos (ex.: SQL do EF) para não poluir as perguntas.
// AddFilter (e não SetMinimumLevel), porque os níveis do appsettings.json teriam prioridade.
if (comando is not null)
    builder.Logging.AddFilter(nivel => nivel >= LogLevel.Warning);

// ===== 1. Registro de serviços (injeção de dependência) =====
builder.Services.AddOpenApi();
builder.Services.AddInfraestrutura(builder.Configuration);
builder.Services.AddAutenticacao();

// Erros no formato ProblemDetails; regras de negócio violadas viram 400 com a mensagem da regra.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<TratadorDeErros>();

// Enums no JSON como texto ("Rascunho"), e não como número (0): o app e quem lê a API entendem na hora.
builder.Services.ConfigureHttpJsonOptions(json => json.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Relógio: "hoje" no fuso da oficina. TimeProvider.System é o relógio real do .NET.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<Relogio>();

// CORS: quais sites (origens) o NAVEGADOR deve deixar chamar esta API.
// Em produção, a origem do front-end (ex.: https://app.vercel.app) vem da configuração.
// O navegador envia a origem sem barra no final ("https://app.vercel.app"). Normalizamos a configuração
// porque um "/" a mais ao colar o endereço fazia a comparação falhar (aconteceu no primeiro deploy).
var origensPermitidas = (builder.Configuration.GetSection("Cors:OrigensPermitidas").Get<string[]>() ?? [])
    .Select(origem => origem.Trim().TrimEnd('/'))
    .Where(origem => origem.Length > 0)
    .ToArray();
builder.Services.AddCors(cors => cors.AddDefaultPolicy(politica => politica
    .WithOrigins(origensPermitidas)
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

// ===== Comandos de terminal (executam e encerram, sem subir o servidor web) =====
if (comando is not null)
    return await comando(app.Services);

// ===== 2. Pipeline: o caminho que cada requisição HTTP percorre =====
app.UseExceptionHandler(); // primeiro: captura exceções de tudo o que vem depois
app.UseCors();             // antes da autenticação: a "pré-consulta" do navegador (OPTIONS) não leva token
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
app.MapCategoriasEndpoints();
app.MapServicosEndpoints();
app.MapClientesEndpoints();
app.MapOrcamentosEndpoints();

await app.RunAsync();
return 0;
