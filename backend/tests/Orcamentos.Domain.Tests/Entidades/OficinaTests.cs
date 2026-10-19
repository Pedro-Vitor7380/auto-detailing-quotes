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

    [Fact]
    public void DefinirLogo_AceitaPngEJpeg()
    {
        var oficina = new Oficina("Estética Exemplo");
        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00];
        byte[] jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00];

        oficina.DefinirLogo(png);
        Assert.Equal(png, oficina.Logo);

        oficina.DefinirLogo(jpeg);
        Assert.Equal(jpeg, oficina.Logo);

        oficina.DefinirLogo(null);
        Assert.Null(oficina.Logo);
    }

    [Fact]
    public void DefinirLogo_ComOutroFormato_LancaRegraDeNegocio()
    {
        // "GIF89a": um GIF renomeado para .png continua sendo GIF.
        byte[] gif = [0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x00, 0x00, 0x00];

        Assert.Throws<RegraDeNegocioException>(() => new Oficina("Estética Exemplo").DefinirLogo(gif));
    }

    [Fact]
    public void DefinirLogo_GrandeDemais_LancaRegraDeNegocio()
    {
        var grande = new byte[Oficina.LogoTamanhoMaximo + 1];
        grande[0] = 0xFF; grande[1] = 0xD8; grande[2] = 0xFF;

        Assert.Throws<RegraDeNegocioException>(() => new Oficina("Estética Exemplo").DefinirLogo(grande));
    }
}
