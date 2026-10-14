using Microsoft.AspNetCore.Identity;
using Orcamentos.Domain.Entidades;

namespace Orcamentos.Infrastructure.Seguranca;

/// <summary>
/// Transforma senhas em hash e confere senhas digitadas contra o hash salvo.
/// Usa o PasswordHasher do ASP.NET Core Identity (PBKDF2-SHA512, salt aleatório,
/// 100.000 iterações), em vez de inventarmos nossa própria criptografia.
/// </summary>
public class HasherDeSenha
{
    public const int TamanhoMinimo = 8;

    private readonly PasswordHasher<Usuario> _hasher = new();

    public string GerarHash(string senha) => _hasher.HashPassword(null!, senha);

    public bool Verificar(string hash, string senhaDigitada) =>
        _hasher.VerifyHashedPassword(null!, hash, senhaDigitada) != PasswordVerificationResult.Failed;
}
