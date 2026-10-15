using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Orcamentos.Infrastructure.Dados;
using Orcamentos.Infrastructure.Seguranca;

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

        // O Npgsql tenta negociar criptografia via Kerberos (GSSAPI) por padrão. Não usamos Kerberos,
        // e a biblioteca não existe no container: sem isto, cada conexão registra um erro falso no log.
        var conexao = new NpgsqlConnectionStringBuilder(connectionString) { GssEncryptionMode = GssEncryptionMode.Disable };

        services.AddDbContext<AppDbContext>(options => options
            .UseNpgsql(conexao.ConnectionString)
            .UseSnakeCaseNamingConvention()); // tabelas e colunas em snake_case: padrão do PostgreSQL

        services.AddSingleton<HasherDeSenha>();

        return services;
    }
}
