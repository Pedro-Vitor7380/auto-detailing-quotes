using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Orcamentos.Infrastructure.Dados;

namespace Orcamentos.Api.Features.Auth;

public static class AuthConfiguracao
{
    public static IServiceCollection AddAutenticacao(this IServiceCollection services)
    {
        services.AddOptions<JwtOpcoes>()
            .BindConfiguration(JwtOpcoes.Secao)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<GeradorDeToken>();

        // A oficina do usuário logado, usada pelo filtro global do banco (cada oficina só vê os seus dados).
        services.AddHttpContextAccessor();
        services.AddScoped<IOficinaAtual, OficinaAtualDoToken>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Configura a validação do token usando as JwtOpcoes (lidas da configuração).
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOpcoes>>((bearer, jwt) =>
            {
                bearer.MapInboundClaims = false; // mantém os nomes das claims como estão no token ("sub", "email"...)
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Value.Emissor,
                    ValidAudience = jwt.Value.Audiencia,
                    IssuerSigningKey = jwt.Value.ObterChaveDeAssinatura(),
                    ClockSkew = TimeSpan.FromMinutes(1), // tolerância para relógios levemente diferentes
                };
            });

        // Seguro por padrão: TODO endpoint exige login, a menos que seja marcado com AllowAnonymous().
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        return services;
    }
}
