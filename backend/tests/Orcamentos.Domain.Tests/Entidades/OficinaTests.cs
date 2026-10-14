using Orcamentos.Domain.Comum;
using Orcamentos.Domain.Entidades;

namespace Orcamentos.Domain.Tests.Entidades;

public class OficinaTests
{
    [Fact]
    public void Criar_SemNome_LancaRegraDeNegocio()
    {
        Assert.Throws<RegraDeNegocioException>(() => new Oficina(""));
    }

    [Fact]
    public void AtualizarContato_ComTextoVazio_GuardaNull()
    {
        var oficina = new Oficina("Estética Exemplo");

        oficina.AtualizarContato(cnpj: "   ", telefone: " (11) 99999-0000 ", endereco: null);

        Assert.Null(oficina.Cnpj);
        Assert.Equal("(11) 99999-0000", oficina.Telefone);
        Assert.Null(oficina.Endereco);
    }
}
