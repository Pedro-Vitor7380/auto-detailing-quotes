namespace Orcamentos.Domain.Comum;

/// <summary>
/// Lançada quando uma regra de negócio é violada (ex.: nome vazio, e-mail inválido).
/// A API vai convertê-la em uma resposta HTTP 400 com a mensagem para o usuário.
/// </summary>
public class RegraDeNegocioException(string mensagem) : Exception(mensagem);
