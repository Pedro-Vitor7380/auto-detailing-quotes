using System.Net.Mail;
using Orcamentos.Domain.Comum;

namespace Orcamentos.Domain.Entidades;

/// <summary>
/// Pessoa que acessa o sistema. Pertence a uma oficina.
/// </summary>
public class Usuario : EntidadeDaOficina
{
    public string Nome { get; private set; } = null!;
    public string Email { get; private set; } = null!;

    /// <summary>
    /// Senha já transformada em hash. O Domínio nunca vê a senha original:
    /// quem gera o hash é a camada de API/Infraestrutura (passo 7).
    /// </summary>
    public string SenhaHash { get; private set; } = null!;

    public Usuario(Guid oficinaId, string nome, string email, string senhaHash)
        : base(oficinaId)
    {
        AlterarNome(nome);
        AlterarEmail(email);
        DefinirSenhaHash(senhaHash);
    }

    // Construtor vazio exigido pelo Entity Framework para recriar o objeto a partir do banco.
    private Usuario() { }

    public void AlterarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new RegraDeNegocioException("O nome do usuário é obrigatório.");

        Nome = nome.Trim();
    }

    public void AlterarEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !MailAddress.TryCreate(email.Trim(), out _))
            throw new RegraDeNegocioException("E-mail inválido.");

        // Minúsculo para que "Pedro@Gmail.com" e "pedro@gmail.com" sejam o mesmo login.
        Email = email.Trim().ToLowerInvariant();
    }

    public void DefinirSenhaHash(string senhaHash)
    {
        if (string.IsNullOrWhiteSpace(senhaHash))
            throw new RegraDeNegocioException("O hash da senha é obrigatório.");

        SenhaHash = senhaHash;
    }
}
