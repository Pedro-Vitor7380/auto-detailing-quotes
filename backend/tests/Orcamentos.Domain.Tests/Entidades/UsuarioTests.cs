using Orcamentos.Domain.Comum;
using Orcamentos.Domain.Entidades;

namespace Orcamentos.Domain.Tests.Entidades;

public class UsuarioTests
{
    private static readonly Guid OficinaId = Guid.CreateVersion7();

    [Fact]
    public void Criar_ComDadosValidos_PreencheOsCampos()
    {
        var usuario = new Usuario(OficinaId, "  Pedro  ", "pedro@exemplo.com", "hash");

        Assert.Equal(OficinaId, usuario.OficinaId);
        Assert.Equal("Pedro", usuario.Nome);
        Assert.NotEqual(Guid.Empty, usuario.Id);
    }

    [Fact]
    public void Criar_ComEmailMaiusculo_GuardaEmMinusculo()
    {
        var usuario = new Usuario(OficinaId, "Pedro", " Pedro@Exemplo.COM ", "hash");

        Assert.Equal("pedro@exemplo.com", usuario.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("sem-arroba")]
    [InlineData("@exemplo.com")]
    public void Criar_ComEmailInvalido_LancaRegraDeNegocio(string email)
    {
        var erro = Assert.Throws<RegraDeNegocioException>(
            () => new Usuario(OficinaId, "Pedro", email, "hash"));

        Assert.Equal("E-mail inválido.", erro.Message);
    }

    [Fact]
    public void NormalizarEmail_DevolveOMesmoFormatoGuardadoNoUsuario()
    {
        var usuario = new Usuario(OficinaId, "Pedro", " Pedro@Exemplo.COM ", "hash");

        Assert.Equal(usuario.Email, Usuario.NormalizarEmail("PEDRO@exemplo.com  "));
    }

    [Fact]
    public void Criar_SemNome_LancaRegraDeNegocio()
    {
        Assert.Throws<RegraDeNegocioException>(
            () => new Usuario(OficinaId, " ", "pedro@exemplo.com", "hash"));
    }

    [Fact]
    public void Criar_SemOficina_LancaRegraDeNegocio()
    {
        Assert.Throws<RegraDeNegocioException>(
            () => new Usuario(Guid.Empty, "Pedro", "pedro@exemplo.com", "hash"));
    }
}
