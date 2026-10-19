namespace Orcamentos.Infrastructure.Dados;

/// <summary>
/// Diz de qual oficina é a requisição em andamento. A API implementa lendo o token de login.
/// A Infraestrutura só conhece esta interface, não sabe nada de HTTP nem de JWT.
/// </summary>
public interface IOficinaAtual
{
    /// <summary>Null quando não há usuário logado (ex.: login, health, comandos de terminal).</summary>
    Guid? Id { get; }
}
