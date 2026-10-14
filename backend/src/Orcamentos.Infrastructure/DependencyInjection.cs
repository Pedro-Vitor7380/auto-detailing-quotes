using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orcamentos.Infrastructure.Dados;

namespace Orcamentos.Infrastructure;

/// <summary>
/// Registra na injeção de dependência tudo que a camada de Infraestrutura oferece.
/// Assim o Program.cs só precisa chamar builder.Services.AddInfraestrutura(...).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfraestrutura(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Banco");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Connection string 'Banco' não configurada. Em desenvolvimento, use: " +
                "dotnet user-secrets set \"ConnectionStrings:Banco\" \"<valor>\" --project src/Orcamentos.Api");

        services.AddDbContext<AppDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()); // tabelas e colunas em snake_case: padrão do PostgreSQL

        return services;
    }
}
