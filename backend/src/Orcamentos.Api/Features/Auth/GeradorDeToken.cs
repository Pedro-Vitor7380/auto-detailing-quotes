using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Orcamentos.Domain.Entidades;

namespace Orcamentos.Api.Features.Auth;

public record TokenGerado(string Token, DateTimeOffset ExpiraEm);

/// <summary>
/// Cria o JWT de um usuário autenticado.
/// </summary>
public class GeradorDeToken(IOptions<JwtOpcoes> opcoes)
{
    /// <summary>Nome da claim que guarda a oficina do usuário (claim própria, não é padrão do JWT).</summary>
    public const string ClaimOficinaId = "oficina_id";

    public TokenGerado Gerar(Usuario usuario)
    {
        var jwt = opcoes.Value;
        var expiraEm = DateTimeOffset.UtcNow.AddHours(jwt.ExpiracaoHoras);

        var descritor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Emissor,
            Audience = jwt.Audiencia,
            Expires = expiraEm.UtcDateTime,
            SigningCredentials = new SigningCredentials(jwt.ObterChaveDeAssinatura(), SecurityAlgorithms.HmacSha256),
            // Claims: as "informações do crachá". Qualquer um consegue LER o conteúdo de um JWT,
            // então nunca coloque aqui nada secreto (senha, dados sensíveis).
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = usuario.Id.ToString(),
                [JwtRegisteredClaimNames.Email] = usuario.Email,
                [JwtRegisteredClaimNames.Name] = usuario.Nome,
                [ClaimOficinaId] = usuario.OficinaId.ToString(),
            },
        };

        return new TokenGerado(new JsonWebTokenHandler().CreateToken(descritor), expiraEm);
    }
}
