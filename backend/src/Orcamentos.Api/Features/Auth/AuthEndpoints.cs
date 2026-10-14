using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Orcamentos.Domain.Comum;
using Orcamentos.Domain.Entidades;
using Orcamentos.Infrastructure.Dados;
using Orcamentos.Infrastructure.Seguranca;

namespace Orcamentos.Api.Features.Auth;

public record LoginRequest(string Email, string Senha);
public record UsuarioResponse(Guid Id, string Nome, string Email, Guid OficinaId);
public record LoginResponse(string Token, DateTimeOffset ExpiraEm, UsuarioResponse Usuario);

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/auth").WithTags("Autenticação");

        grupo.MapPost("/login", Login)
            .AllowAnonymous()
            .WithSummary("Confere e-mail e senha e devolve um token JWT");

        grupo.MapGet("/me", UsuarioLogado)
            .WithSummary("Dados do usuário dono do token (exige login)");

        return app;
    }

    private static async Task<Results<Ok<LoginResponse>, ProblemHttpResult>> Login(
        LoginRequest request, AppDbContext db, HasherDeSenha hasher, GeradorDeToken gerador, CancellationToken ct)
    {
        Usuario? usuario = null;
        if (TentarNormalizarEmail(request.Email, out var email))
            usuario = await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);

        // Sempre verifica uma senha, mesmo sem usuário (ver HasherDeSenha.HashFalso).
        var senhaConfere = hasher.Verificar(usuario?.SenhaHash ?? hasher.HashFalso, request.Senha ?? "");

        // A mesma mensagem para "e-mail não existe" e "senha errada": não revelamos qual dos dois falhou.
        if (usuario is null || !senhaConfere)
            return TypedResults.Problem(statusCode: StatusCodes.Status401Unauthorized,
                title: "Não autorizado", detail: "E-mail ou senha inválidos.");

        var token = gerador.Gerar(usuario);
        return TypedResults.Ok(new LoginResponse(token.Token, token.ExpiraEm, ParaResponse(usuario)));
    }

    private static async Task<Results<Ok<UsuarioResponse>, UnauthorizedHttpResult>> UsuarioLogado(
        ClaimsPrincipal user, AppDbContext db, CancellationToken ct)
    {
        // Só chega aqui com token válido. O "sub" do token é o Id do usuário.
        if (!Guid.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id))
            return TypedResults.Unauthorized();

        var usuario = await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);

        // Token válido, mas o usuário foi apagado depois que o token foi emitido.
        return usuario is null ? TypedResults.Unauthorized() : TypedResults.Ok(ParaResponse(usuario));
    }

    private static UsuarioResponse ParaResponse(Usuario u) => new(u.Id, u.Nome, u.Email, u.OficinaId);

    private static bool TentarNormalizarEmail(string? email, out string normalizado)
    {
        try
        {
            normalizado = Usuario.NormalizarEmail(email ?? "");
            return true;
        }
        catch (RegraDeNegocioException)
        {
            normalizado = "";
            return false;
        }
    }
}
