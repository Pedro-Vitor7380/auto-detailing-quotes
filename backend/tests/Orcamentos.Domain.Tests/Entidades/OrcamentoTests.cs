using Orcamentos.Domain.Comum;
using Orcamentos.Domain.Entidades;

namespace Orcamentos.Domain.Tests.Entidades;

public class OrcamentoTests
{
    private static readonly Guid OficinaId = Guid.NewGuid();
    private static readonly DateOnly Hoje = new(2026, 10, 19);
    private static readonly DateTimeOffset Agora = new(2026, 10, 19, 10, 0, 0, TimeSpan.FromHours(-3));

    private static Orcamento Criar() => new(OficinaId, 2026, 1, Guid.NewGuid(), Guid.NewGuid(), Hoje);

    private static Servico Lavagem(decimal minimo = 120m) =>
        new(OficinaId, Guid.NewGuid(), "Lavagem Detalhada", null, minimo, 180m);

    // ===== Criação =====

    [Fact]
    public void Criar_ComecaComoRascunhoValendoSeteDias()
    {
        var orcamento = Criar();

        Assert.Equal(StatusDoOrcamento.Rascunho, orcamento.Status);
        Assert.Equal(new DateOnly(2026, 10, 26), orcamento.Validade);
        Assert.Equal(0m, orcamento.Total);
    }

    [Theory]
    [InlineData(1, "ORC-2026-0001")]
    [InlineData(42, "ORC-2026-0042")]
    [InlineData(12345, "ORC-2026-12345")] // passou de 9999: não corta, só cresce
    public void Numero_TemAnoESequencialComQuatroDigitos(int sequencial, string esperado)
    {
        var orcamento = new Orcamento(OficinaId, 2026, sequencial, Guid.NewGuid(), Guid.NewGuid(), Hoje);

        Assert.Equal(esperado, orcamento.Numero);
    }

    // ===== Itens =====

    [Fact]
    public void AdicionarServico_EntraPeloPrecoMinimoECopiaOsDados()
    {
        var orcamento = Criar();
        var servico = Lavagem();

        var item = orcamento.AdicionarServico(servico);

        Assert.Equal(servico.Id, item.ServicoId);
        Assert.Equal("Lavagem Detalhada", item.Descricao);
        Assert.Equal(120m, item.PrecoCatalogo);
        Assert.Equal(120m, item.ValorUnitario);
        Assert.Equal(0m, item.Ajuste);
    }

    [Fact]
    public void AdicionarServico_ComValorAjustado_GuardaODiferencaEmRelacaoAoCatalogo()
    {
        var item = Criar().AdicionarServico(Lavagem(), valorUnitario: 160m, observacaoEstado: "Muitos pelos de animal");

        Assert.Equal(160m, item.ValorUnitario);
        Assert.Equal(40m, item.Ajuste);
        Assert.Equal("Muitos pelos de animal", item.ObservacaoEstado);
    }

    [Fact]
    public void MudarOPrecoDoCatalogo_NaoAlteraItemJaAdicionado()
    {
        var orcamento = Criar();
        var servico = Lavagem();
        var item = orcamento.AdicionarServico(servico);

        servico.Atualizar(servico.CategoriaId, servico.Nome, null, 999m, 999m);

        Assert.Equal(120m, item.PrecoCatalogo);
        Assert.Equal(120m, item.ValorUnitario);
    }

    [Fact]
    public void AdicionarServico_Desativado_LancaRegraDeNegocio()
    {
        var servico = Lavagem();
        servico.Desativar();

        Assert.Throws<RegraDeNegocioException>(() => Criar().AdicionarServico(servico));
    }

    [Fact]
    public void AdicionarServico_DeOutraOficina_LancaRegraDeNegocio()
    {
        var deOutraOficina = new Servico(Guid.NewGuid(), Guid.NewGuid(), "Lavagem", null, 100m, 100m);

        Assert.Throws<RegraDeNegocioException>(() => Criar().AdicionarServico(deOutraOficina));
    }

    [Fact]
    public void AdicionarAvulso_NaoTemPrecoDeCatalogoNemAjuste()
    {
        var item = Criar().AdicionarAvulso("Remoção de adesivos", 1, 80m);

        Assert.Null(item.ServicoId);
        Assert.Null(item.PrecoCatalogo);
        Assert.Null(item.Ajuste);
    }

    [Theory]
    [InlineData(0, 100)]   // quantidade zero
    [InlineData(1, -1)]    // valor negativo
    public void AdicionarAvulso_ComValoresInvalidos_LancaRegraDeNegocio(int quantidade, decimal valor)
    {
        Assert.Throws<RegraDeNegocioException>(() => Criar().AdicionarAvulso("Item", quantidade, valor));
    }

    [Fact]
    public void AtualizarItem_DoCatalogo_MantemDescricaoEPrecoDeCatalogo()
    {
        var orcamento = Criar();
        var item = orcamento.AdicionarServico(Lavagem());

        orcamento.AtualizarItem(item.Id, 2, 150m, "Barro no assoalho", descricaoAvulso: "Outro nome");

        Assert.Equal("Lavagem Detalhada", item.Descricao);
        Assert.Equal(120m, item.PrecoCatalogo);
        Assert.Equal(2, item.Quantidade);
        Assert.Equal(150m, item.ValorUnitario);
    }

    [Fact]
    public void AtualizarItem_Avulso_PodeMudarDescricao()
    {
        var orcamento = Criar();
        var item = orcamento.AdicionarAvulso("Adesivo", 1, 80m);

        orcamento.AtualizarItem(item.Id, 1, 90m, null, descricaoAvulso: "Remoção de adesivos");

        Assert.Equal("Remoção de adesivos", item.Descricao);
    }

    [Fact]
    public void RemoverItem_DeOutroOrcamento_LancaRegraDeNegocio()
    {
        Assert.Throws<RegraDeNegocioException>(() => Criar().RemoverItem(Guid.NewGuid()));
    }

    // ===== Totais e desconto =====

    [Fact]
    public void Totais_SomamQuantidadeVezesValor()
    {
        var orcamento = Criar();
        orcamento.AdicionarServico(Lavagem(), quantidade: 2);   // 2 × 120 = 240
        orcamento.AdicionarAvulso("Adesivo", 1, 60.50m);         // 60,50

        Assert.Equal(300.50m, orcamento.Subtotal);
        Assert.Equal(300.50m, orcamento.Total);
    }

    [Fact]
    public void DescontoEmValor_SubtraiDoTotal()
    {
        var orcamento = Criar();
        orcamento.AdicionarAvulso("Serviço", 1, 500m);

        orcamento.DefinirDesconto(TipoDeDesconto.Valor, 50m);

        Assert.Equal(50m, orcamento.ValorDoDesconto);
        Assert.Equal(450m, orcamento.Total);
    }

    [Fact]
    public void DescontoPercentual_ArredondaParaCentavos()
    {
        var orcamento = Criar();
        orcamento.AdicionarAvulso("Serviço", 1, 333.33m);

        orcamento.DefinirDesconto(TipoDeDesconto.Percentual, 10m); // 33,333 → 33,33

        Assert.Equal(33.33m, orcamento.ValorDoDesconto);
        Assert.Equal(300.00m, orcamento.Total);
    }

    [Theory]
    [InlineData(TipoDeDesconto.Percentual, 101)]
    [InlineData(TipoDeDesconto.Valor, 600)]   // maior que o subtotal de 500
    [InlineData(TipoDeDesconto.Valor, -1)]
    public void Desconto_Invalido_LancaRegraDeNegocio(TipoDeDesconto tipo, decimal valor)
    {
        var orcamento = Criar();
        orcamento.AdicionarAvulso("Serviço", 1, 500m);

        Assert.Throws<RegraDeNegocioException>(() => orcamento.DefinirDesconto(tipo, valor));
    }

    [Fact]
    public void DescontoEmValor_NuncaDeixaOTotalNegativo_MesmoRemovendoItensDepois()
    {
        var orcamento = Criar();
        var caro = orcamento.AdicionarAvulso("Caro", 1, 500m);
        orcamento.AdicionarAvulso("Barato", 1, 100m);
        orcamento.DefinirDesconto(TipoDeDesconto.Valor, 400m);

        orcamento.RemoverItem(caro.Id); // sobra 100 de subtotal e 400 de desconto

        Assert.Equal(100m, orcamento.ValorDoDesconto);
        Assert.Equal(0m, orcamento.Total);
    }

    [Fact]
    public void DescontoNenhum_ZeraOValor()
    {
        var orcamento = Criar();
        orcamento.AdicionarAvulso("Serviço", 1, 500m);

        orcamento.DefinirDesconto(TipoDeDesconto.Nenhum, 50m);

        Assert.Equal(0m, orcamento.DescontoValor);
        Assert.Equal(500m, orcamento.Total);
    }

    // ===== Validade e expiração =====

    [Fact]
    public void DefinirValidade_NoPassado_LancaRegraDeNegocio()
    {
        Assert.Throws<RegraDeNegocioException>(() => Criar().DefinirValidade(Hoje.AddDays(-1), Hoje));
    }

    [Fact]
    public void Enviado_ExpiraSoNoDiaSeguinteAValidade()
    {
        var orcamento = Criar();
        orcamento.AdicionarAvulso("Serviço", 1, 100m);
        orcamento.MarcarComoEnviado(Agora);

        Assert.False(orcamento.ExpirarSeVencido(orcamento.Validade));          // último dia: ainda vale
        Assert.Equal(StatusDoOrcamento.Enviado, orcamento.Status);

        Assert.True(orcamento.ExpirarSeVencido(orcamento.Validade.AddDays(1)));
        Assert.Equal(StatusDoOrcamento.Expirado, orcamento.Status);
    }

    [Fact]
    public void Rascunho_NaoExpira()
    {
        var orcamento = Criar();

        Assert.False(orcamento.ExpirarSeVencido(Hoje.AddDays(30)));
        Assert.Equal(StatusDoOrcamento.Rascunho, orcamento.Status);
    }

    // ===== Status =====

    [Fact]
    public void Enviar_SemItens_LancaRegraDeNegocio()
    {
        Assert.Throws<RegraDeNegocioException>(() => Criar().MarcarComoEnviado(Agora));
    }

    [Fact]
    public void Aprovar_GuardaDataEImpedeEdicao()
    {
        var orcamento = Criar();
        orcamento.AdicionarAvulso("Serviço", 1, 100m);

        orcamento.Aprovar(Agora);

        Assert.Equal(StatusDoOrcamento.Aprovado, orcamento.Status);
        Assert.Equal(Agora, orcamento.AprovadoEm);
        Assert.False(orcamento.PodeSerEditado);
        var erro = Assert.Throws<RegraDeNegocioException>(() => orcamento.AdicionarAvulso("Outro", 1, 10m));
        Assert.Contains("Duplicar", erro.Message);
    }

    [Fact]
    public void Expirado_NaoPodeSerAprovado()
    {
        var orcamento = Criar();
        orcamento.AdicionarAvulso("Serviço", 1, 100m);
        orcamento.MarcarComoEnviado(Agora);
        orcamento.ExpirarSeVencido(orcamento.Validade.AddDays(1));

        Assert.Throws<RegraDeNegocioException>(() => orcamento.Aprovar(Agora));
    }

    [Fact]
    public void Recusado_NaoPodeSerAprovado()
    {
        var orcamento = Criar();
        orcamento.AdicionarAvulso("Serviço", 1, 100m);
        orcamento.Recusar(Agora);

        Assert.Throws<RegraDeNegocioException>(() => orcamento.Aprovar(Agora));
    }

    [Fact]
    public void Enviado_AindaPodeSerEditadoEReenviado()
    {
        var orcamento = Criar();
        orcamento.AdicionarAvulso("Serviço", 1, 100m);
        orcamento.MarcarComoEnviado(Agora);

        orcamento.AdicionarAvulso("Outro", 1, 50m);
        orcamento.MarcarComoEnviado(Agora.AddHours(1));

        Assert.Equal(150m, orcamento.Total);
        Assert.Equal(Agora.AddHours(1), orcamento.EnviadoEm);
    }

    [Fact]
    public void Excluir_SoRascunho()
    {
        var orcamento = Criar();
        orcamento.GarantirQuePodeSerExcluido(); // rascunho: não lança

        orcamento.AdicionarAvulso("Serviço", 1, 100m);
        orcamento.MarcarComoEnviado(Agora);

        Assert.Throws<RegraDeNegocioException>(orcamento.GarantirQuePodeSerExcluido);
    }

    // ===== Duplicar =====

    [Fact]
    public void Duplicar_CriaRascunhoNovoComOsMesmosValores()
    {
        var original = Criar();
        var servico = Lavagem();
        original.AdicionarServico(servico, valorUnitario: 150m, observacaoEstado: "Sujo");
        original.DefinirDesconto(TipoDeDesconto.Percentual, 10m);
        original.DefinirObservacoes("Cliente antigo");
        original.Aprovar(Agora);
        servico.Atualizar(servico.CategoriaId, servico.Nome, null, 999m, 999m); // catálogo mudou depois

        var copia = original.Duplicar(2026, 2, Hoje.AddDays(3));

        Assert.NotEqual(original.Id, copia.Id);
        Assert.Equal("ORC-2026-0002", copia.Numero);
        Assert.Equal(StatusDoOrcamento.Rascunho, copia.Status);
        Assert.Equal(Hoje.AddDays(3 + Orcamento.DiasDeValidadePadrao), copia.Validade);
        Assert.Equal(original.ClienteId, copia.ClienteId);
        Assert.Equal(original.Total, copia.Total);           // 150 - 10% = 135, não usa o preço novo
        Assert.Equal(120m, copia.Itens.Single().PrecoCatalogo);
        Assert.Equal("Sujo", copia.Itens.Single().ObservacaoEstado);
        Assert.Equal("Cliente antigo", copia.Observacoes);
        Assert.Null(copia.AprovadoEm);
        Assert.NotEqual(original.Itens.Single().Id, copia.Itens.Single().Id);
    }

    // ===== Agendamento =====

    private static Orcamento Aprovado()
    {
        var orcamento = Criar();
        orcamento.AdicionarAvulso("Serviço", 1, 100m);
        orcamento.Aprovar(Agora);
        return orcamento;
    }

    [Fact]
    public void Agendar_Aprovado_GuardaODia()
    {
        var orcamento = Aprovado();
        var terca = new DateOnly(2026, 10, 20);

        orcamento.Agendar(terca, Hoje);

        Assert.Equal(terca, orcamento.DataAgendada);
    }

    [Fact]
    public void Agendar_DeNovo_Reagenda()
    {
        var orcamento = Aprovado();
        orcamento.Agendar(new DateOnly(2026, 10, 20), Hoje);

        orcamento.Agendar(new DateOnly(2026, 10, 22), Hoje);

        Assert.Equal(new DateOnly(2026, 10, 22), orcamento.DataAgendada);
    }

    [Fact]
    public void Agendar_SemEstarAprovado_LancaRegraDeNegocio()
    {
        var rascunho = Criar();
        rascunho.AdicionarAvulso("Serviço", 1, 100m);

        Assert.Throws<RegraDeNegocioException>(() => rascunho.Agendar(new DateOnly(2026, 10, 20), Hoje));
    }

    [Fact]
    public void Agendar_NoPassado_LancaRegraDeNegocio()
    {
        Assert.Throws<RegraDeNegocioException>(() => Aprovado().Agendar(Hoje.AddDays(-1), Hoje));
    }

    [Fact]
    public void Agendar_NoDomingo_LancaRegraDeNegocio()
    {
        var domingo = new DateOnly(2026, 10, 25);
        Assert.Equal(DayOfWeek.Sunday, domingo.DayOfWeek);

        Assert.Throws<RegraDeNegocioException>(() => Aprovado().Agendar(domingo, Hoje));
    }

    [Fact]
    public void Agendar_Hoje_EPermitido()
    {
        var orcamento = Aprovado();

        orcamento.Agendar(Hoje, Hoje); // 19/10/2026 é segunda-feira

        Assert.Equal(Hoje, orcamento.DataAgendada);
    }

    [Fact]
    public void CancelarAgendamento_LimpaODia_ESemAgendamentoLanca()
    {
        var orcamento = Aprovado();
        Assert.Throws<RegraDeNegocioException>(orcamento.CancelarAgendamento);

        orcamento.Agendar(new DateOnly(2026, 10, 20), Hoje);
        orcamento.CancelarAgendamento();

        Assert.Null(orcamento.DataAgendada);
    }
}
