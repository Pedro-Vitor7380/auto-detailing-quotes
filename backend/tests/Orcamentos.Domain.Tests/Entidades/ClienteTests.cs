using Orcamentos.Domain.Comum;
using Orcamentos.Domain.Entidades;

namespace Orcamentos.Domain.Tests.Entidades;

public class ClienteTests
{
    private static Cliente Criar() => new(Guid.NewGuid(), "  Maria Souza ", "(11) 99999-8888");

    [Theory]
    [InlineData("(11) 99999-8888", "5511999998888")]   // celular com 9 dígitos
    [InlineData("11 3333-4444", "551133334444")]       // fixo com 8 dígitos
    [InlineData("+55 (21) 98888-7777", "5521988887777")] // já com o código do país
    public void Criar_NormalizaWhatsApp(string digitado, string guardado)
    {
        var cliente = new Cliente(Guid.NewGuid(), "Maria", digitado);

        Assert.Equal(guardado, cliente.WhatsApp);
    }

    [Theory]
    [InlineData("99999-8888")]       // sem DDD
    [InlineData("")]
    [InlineData("+1 415 555 2671")]  // número de outro país
    [InlineData("14155552671")]      // 11 dígitos, mas celular sem o 9 na frente
    public void Criar_ComWhatsAppInvalido_LancaRegraDeNegocio(string digitado)
    {
        Assert.Throws<RegraDeNegocioException>(() => new Cliente(Guid.NewGuid(), "Maria", digitado));
    }

    [Fact]
    public void Criar_RemoveEspacosDoNome()
    {
        Assert.Equal("Maria Souza", Criar().Nome);
    }

    [Theory]
    [InlineData("abc-1234", "ABC1234")]  // antiga
    [InlineData("abc 1d23", "ABC1D23")]  // Mercosul
    public void AdicionarVeiculo_NormalizaPlaca(string digitada, string guardada)
    {
        var cliente = Criar();

        var veiculo = cliente.AdicionarVeiculo(digitada, " Civic 2019 ");

        Assert.Equal(guardada, veiculo.Placa);
        Assert.Equal("Civic 2019", veiculo.Modelo);
        Assert.Equal(cliente.Id, veiculo.ClienteId);
        Assert.Equal(cliente.OficinaId, veiculo.OficinaId);
    }

    [Theory]
    [InlineData("AB1234")]    // faltando letra
    [InlineData("ABCD123")]   // 4 letras
    [InlineData("ABC12345")]  // longa demais
    public void AdicionarVeiculo_ComPlacaInvalida_LancaRegraDeNegocio(string placa)
    {
        Assert.Throws<RegraDeNegocioException>(() => Criar().AdicionarVeiculo(placa, "Civic"));
    }

    [Fact]
    public void AdicionarVeiculo_ComPlacaRepetida_LancaRegraDeNegocio()
    {
        var cliente = Criar();
        cliente.AdicionarVeiculo("ABC1D23", "Civic");

        Assert.Throws<RegraDeNegocioException>(() => cliente.AdicionarVeiculo("abc-1d23", "Corolla"));
    }

    [Fact]
    public void AtualizarVeiculo_AlteraPlacaEModelo()
    {
        var cliente = Criar();
        var veiculo = cliente.AdicionarVeiculo("ABC1D23", "Civic");

        cliente.AtualizarVeiculo(veiculo.Id, "xyz9a87", "Civic Touring");

        Assert.Equal("XYZ9A87", veiculo.Placa);
        Assert.Equal("Civic Touring", veiculo.Modelo);
    }

    [Fact]
    public void AtualizarVeiculo_ParaPlacaDeOutroVeiculoDoCliente_LancaRegraDeNegocio()
    {
        var cliente = Criar();
        cliente.AdicionarVeiculo("ABC1D23", "Civic");
        var segundo = cliente.AdicionarVeiculo("XYZ9A87", "Corolla");

        Assert.Throws<RegraDeNegocioException>(() => cliente.AtualizarVeiculo(segundo.Id, "ABC1D23", "Corolla"));
    }

    [Fact]
    public void RemoverVeiculo_TiraDaLista()
    {
        var cliente = Criar();
        var veiculo = cliente.AdicionarVeiculo("ABC1D23", "Civic");

        cliente.RemoverVeiculo(veiculo.Id);

        Assert.Empty(cliente.Veiculos);
    }

    [Fact]
    public void RemoverVeiculo_QueNaoEDoCliente_LancaRegraDeNegocio()
    {
        Assert.Throws<RegraDeNegocioException>(() => Criar().RemoverVeiculo(Guid.NewGuid()));
    }
}
