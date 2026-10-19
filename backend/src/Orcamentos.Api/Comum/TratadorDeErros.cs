using Microsoft.AspNetCore.Diagnostics;
using Orcamentos.Domain.Comum;

namespace Orcamentos.Api.Comum;

/// <summary>
/// Transforma uma RegraDeNegocioException (lançada pelo Domínio) numa resposta 400 com a mensagem da regra.
/// Assim os endpoints não precisam de try/catch: o Domínio valida e o erro chega pronto ao app.
/// Também trata o corpo da requisição ilegível (JSON quebrado, id em formato errado).
/// </summary>
public class TratadorDeErros(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception exception, CancellationToken ct)
    {
        var detalhe = exception switch
        {
            RegraDeNegocioException regra => regra.Message,
            // Em Development o ASP.NET lança esta exceção (para aparecer no log); em produção ela já viraria 400.
            BadHttpRequestException => "Não foi possível ler os dados enviados.",
            _ => null,
        };

        // Outras exceções seguem para o tratamento padrão (500, sem detalhes para o usuário).
        if (detalhe is null)
            return false;

        http.Response.StatusCode = StatusCodes.Status400BadRequest;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            Exception = exception,
            ProblemDetails = { Status = StatusCodes.Status400BadRequest, Title = "Dados inválidos", Detail = detalhe },
        });
    }
}
