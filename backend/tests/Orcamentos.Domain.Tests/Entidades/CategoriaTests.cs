using Orcamentos.Domain.Comum;
using Orcamentos.Domain.Entidades;

namespace Orcamentos.Domain.Tests.Entidades;

public class CategoriaTests
{
    [Fact]
    public void Criar_RemoveEspacosDoNome()
    {
        var categoria = new Categoria(Guid.NewGuid(), "  Películas ");

        Assert.Equal("Películas", categoria.Nome);
    }

    [Fact]
    public void Criar_SemNome_LancaRegraDeNegocio()
    {
        Assert.Throws<RegraDeNegocioException>(() => new Categoria(Guid.NewGuid(), ""));
    }

    [Fact]
    public void Criar_ComNomeLongoDemais_LancaRegraDeNegocio()
    {
        var nome = new string('a', Categoria.NomeTamanhoMaximo + 1);

        Assert.Throws<RegraDeNegocioException>(() => new Categoria(Guid.NewGuid(), nome));
    }

    [Fact]
    public void Criar_SemOficina_LancaRegraDeNegocio()
    {
        Assert.Throws<RegraDeNegocioException>(() => new Categoria(Guid.Empty, "Películas"));
    }
}
