using Orcamentos.Domain.Comum;
using Orcamentos.Domain.Entidades;

namespace Orcamentos.Domain.Tests.Entidades;

public class ServicoTests
{
    private static readonly Guid OficinaId = Guid.NewGuid();
    private static readonly Guid CategoriaId = Guid.NewGuid();

    private static Servico Criar(string nome = "Lavagem Detalhada", string? descricao = null,
        decimal minimo = 120m, decimal maximo = 180m) =>
        new(OficinaId, CategoriaId, nome, descricao, minimo, maximo);

    [Fact]
    public void Criar_ComDadosValidos_FicaAtivo()
    {
        var servico = Criar(nome: "  Lavagem Detalhada  ", descricao: "   ");

        Assert.Equal("Lavagem Detalhada", servico.Nome);
        Assert.Null(servico.Descricao);
        Assert.True(servico.Ativo);
    }

    [Fact]
    public void Criar_ComPrecoFixo_AceitaMinimoIgualAoMaximo()
    {
        var servico = Criar(minimo: 150m, maximo: 150m);

        Assert.Equal(servico.PrecoMinimo, servico.PrecoMaximo);
    }

    [Fact]
    public void Criar_ArredondaPrecosParaCentavos()
    {
        var servico = Criar(minimo: 10.005m, maximo: 20.994m);

        Assert.Equal(10.01m, servico.PrecoMinimo); // ,005 sobe (arredondamento "da escola")
        Assert.Equal(20.99m, servico.PrecoMaximo);
    }

    [Theory]
    [InlineData(200, 150)] // máximo menor que o mínimo
    [InlineData(-1, 100)]  // preço negativo
    public void Criar_ComFaixaInvalida_LancaRegraDeNegocio(decimal minimo, decimal maximo)
    {
        Assert.Throws<RegraDeNegocioException>(() => Criar(minimo: minimo, maximo: maximo));
    }

    [Fact]
    public void Criar_SemNome_LancaRegraDeNegocio()
    {
        Assert.Throws<RegraDeNegocioException>(() => Criar(nome: " "));
    }

    [Fact]
    public void Criar_SemCategoria_LancaRegraDeNegocio()
    {
        Assert.Throws<RegraDeNegocioException>(() =>
            new Servico(OficinaId, Guid.Empty, "Lavagem", null, 100m, 100m));
    }

    [Fact]
    public void Atualizar_ComDadoInvalido_NaoAlteraNada()
    {
        var servico = Criar();

        Assert.Throws<RegraDeNegocioException>(() =>
            servico.Atualizar(CategoriaId, "Outro nome", null, 300m, 100m));

        // Valida tudo antes de alterar: um erro não deixa o objeto "pela metade".
        Assert.Equal("Lavagem Detalhada", servico.Nome);
        Assert.Equal(120m, servico.PrecoMinimo);
    }

    [Fact]
    public void Desativar_EAtivar_AlternaOEstado()
    {
        var servico = Criar();

        servico.Desativar();
        Assert.False(servico.Ativo);

        servico.Ativar();
        Assert.True(servico.Ativo);
    }
}
