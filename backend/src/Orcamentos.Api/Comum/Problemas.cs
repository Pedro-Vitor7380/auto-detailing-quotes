using Microsoft.AspNetCore.Http.HttpResults;

namespace Orcamentos.Api.Comum;

/// <summary>
/// Respostas de erro no formato ProblemDetails (o mesmo do login), com mensagens prontas para o app mostrar.
/// </summary>
public static class Problemas
{
    public static ProblemHttpResult NaoEncontrado(string detalhe) =>
        TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "Não encontrado", detail: detalhe);

    public static ProblemHttpResult Conflito(string detalhe) =>
        TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: "Conflito", detail: detalhe);

    public static ProblemHttpResult Invalido(string detalhe) =>
        TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Dados inválidos", detail: detalhe);
}
