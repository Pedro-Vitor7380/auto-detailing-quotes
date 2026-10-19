using Orcamentos.Infrastructure.Dados;

namespace Orcamentos.Api.Features.Auth;

/// <summary>
/// Descobre a oficina da requisição lendo a claim "oficina_id" do token JWT.
/// É o que alimenta o filtro global do AppDbContext.
/// </summary>
public class OficinaAtualDoToken(IHttpContextAccessor httpContextAccessor) : IOficinaAtual
{
    public Guid? Id =>
        Guid.TryParse(httpContextAccessor.HttpContext?.User.FindFirst(GeradorDeToken.ClaimOficinaId)?.Value, out var id)
            ? id
            : null;
}

public static class OficinaAtualExtensions
{
    /// <summary>
    /// Para endpoints que exigem login: lá sempre existe uma oficina no token.
    /// Se não existir, é erro de programação (endpoint liberado sem querer), não do usuário.
    /// </summary>
    public static Guid IdObrigatorio(this IOficinaAtual oficinaAtual) =>
        oficinaAtual.Id ?? throw new InvalidOperationException("Requisição sem oficina no token.");
}
