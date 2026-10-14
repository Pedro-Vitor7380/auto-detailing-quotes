using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Orcamentos.Api.Features.Auth;

/// <summary>
/// Configurações do JWT, lidas da seção "Jwt" (appsettings.json + User Secrets / variáveis de ambiente).
/// As validações rodam na inicialização: se faltar algo, a API nem sobe.
/// </summary>
public class JwtOpcoes
{
    public const string Secao = "Jwt";

    /// <summary>Quem emitiu o token (a nossa API).</summary>
    [Required]
    public string Emissor { get; init; } = "";

    /// <summary>Para quem o token foi emitido (o nosso app).</summary>
    [Required]
    public string Audiencia { get; init; } = "";

    /// <summary>Chave secreta que assina os tokens. HMAC-SHA256 exige pelo menos 256 bits (32 bytes).</summary>
    [Required, MinLength(32, ErrorMessage = "Jwt:Chave precisa ter pelo menos 32 caracteres.")]
    public string Chave { get; init; } = "";

    [Range(1, 720)]
    public int ExpiracaoHoras { get; init; } = 168;

    public SymmetricSecurityKey ObterChaveDeAssinatura() => new(Encoding.UTF8.GetBytes(Chave));
}
