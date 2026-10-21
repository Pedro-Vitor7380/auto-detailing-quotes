using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using QuestPDF.Infrastructure;
using Orcamentos.Infrastructure.Dados;
using Orcamentos.Infrastructure.Notificacoes;
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

        // QuestPDF: licença Community, gratuita para empresas com faturamento anual abaixo de US$ 1 milhão.
        // Sem esta linha, a biblioteca recusa gerar o PDF (é a forma de ela confirmar que a licença foi lida).
        QuestPDF.Settings.License = LicenseType.Community;

        // Notificações: só ligadas se as chaves VAPID estiverem configuradas (ficam fora do Git, como a do JWT).
        // Sem elas, a API responde "não configurado" e o app esconde o botão de ativar.
        var publica = configuration["Notificacoes:ChavePublica"];
        var privada = configuration["Notificacoes:ChavePrivada"];
        if (!string.IsNullOrWhiteSpace(publica) && !string.IsNullOrWhiteSpace(privada))
        {
            services.AddSingleton(new ChavesVapid(publica, privada, configuration["Notificacoes:Contato"] ?? "mailto:contato@exemplo.com"));
            // AddHttpClient: o .NET reaproveita as conexões (criar um HttpClient por envio esgota as portas do servidor).
            services.AddHttpClient<EnviadorDeNotificacoes>();
        }

        return services;
    }
}
