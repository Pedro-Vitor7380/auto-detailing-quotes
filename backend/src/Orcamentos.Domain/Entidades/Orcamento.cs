using Orcamentos.Domain.Comum;

namespace Orcamentos.Domain.Entidades;

/// <summary>
/// O orçamento: itens, desconto, validade e status. É a raiz do agregado Orcamento + Itens.
/// O Domínio nunca lê o relógio sozinho: quem chama passa "hoje"/"agora". Isso deixa as regras
/// de validade testáveis (dá para simular "8 dias depois" sem esperar 8 dias).
/// </summary>
public class Orcamento : EntidadeDaOficina
{
    public const int DiasDeValidadePadrao = 7;
    public const int ObservacoesTamanhoMaximo = 1000;

    private readonly List<ItemOrcamento> _itens = [];

    /// <summary>Ano + Sequencial formam o número exibido: ORC-2026-0001.</summary>
    public int Ano { get; private set; }

    public int Sequencial { get; private set; }

    public Guid ClienteId { get; private set; }
    public Guid VeiculoId { get; private set; }
    public StatusDoOrcamento Status { get; private set; }
    public TipoDeDesconto DescontoTipo { get; private set; }
    public decimal DescontoValor { get; private set; }

    /// <summary>Último dia em que o orçamento vale (inclusive). No dia seguinte, um Enviado vira Expirado.</summary>
    public DateOnly Validade { get; private set; }

    public string? Observacoes { get; private set; }
    public DateTimeOffset? EnviadoEm { get; private set; }
    public DateTimeOffset? AprovadoEm { get; private set; }
    public DateTimeOffset? RecusadoEm { get; private set; }

    /// <summary>Dia em que o carro chega para o serviço (combinado com o cliente depois da aprovação).</summary>
    public DateOnly? AgendadoDe { get; private set; }

    /// <summary>Dia da entrega. Igual ao AgendadoDe quando o serviço cabe num dia só.</summary>
    public DateOnly? AgendadoAte { get; private set; }

    public IReadOnlyCollection<ItemOrcamento> Itens => _itens.AsReadOnly();

    // ===== Valores calculados (não são gravados no banco: sempre batem com os itens) =====

    public string Numero => FormatarNumero(Ano, Sequencial);
    public decimal Subtotal => _itens.Sum(i => i.Total);

    /// <summary>Nunca maior que o subtotal: se itens forem removidos depois do desconto, o total não fica negativo.</summary>
    public decimal ValorDoDesconto => DescontoTipo switch
    {
        TipoDeDesconto.Valor => Math.Min(DescontoValor, Subtotal),
        TipoDeDesconto.Percentual => Dinheiro.Arredondar(Subtotal * DescontoValor / 100m),
        _ => 0m,
    };

    public decimal Total => Subtotal - ValorDoDesconto;
    public bool PodeSerEditado => Status is StatusDoOrcamento.Rascunho or StatusDoOrcamento.Enviado;

    public Orcamento(Guid oficinaId, int ano, int sequencial, Guid clienteId, Guid veiculoId, DateOnly hoje)
        : base(oficinaId)
    {
        if (sequencial < 1)
            throw new RegraDeNegocioException("O número do orçamento precisa ser maior que zero.");

        Ano = ano;
        Sequencial = sequencial;
        Status = StatusDoOrcamento.Rascunho;
        Validade = hoje.AddDays(DiasDeValidadePadrao);
        DefinirClienteEVeiculo(clienteId, veiculoId);
    }

    // Construtor vazio exigido pelo Entity Framework para recriar o objeto a partir do banco.
    private Orcamento() { }

    public static string FormatarNumero(int ano, int sequencial) => $"ORC-{ano}-{sequencial:D4}";

    // ===== Edição (só em Rascunho ou Enviado) =====

    /// <summary>O endpoint confere antes se o veículo é deste cliente (isso precisa do banco).</summary>
    public void DefinirClienteEVeiculo(Guid clienteId, Guid veiculoId)
    {
        GarantirEditavel();
        if (clienteId == Guid.Empty || veiculoId == Guid.Empty)
            throw new RegraDeNegocioException("Escolha o cliente e o veículo do orçamento.");

        ClienteId = clienteId;
        VeiculoId = veiculoId;
    }

    /// <summary>Item do catálogo: entra com o preço mínimo (ou o valor já ajustado) e guarda uma cópia do serviço.</summary>
    public ItemOrcamento AdicionarServico(Servico servico, int quantidade = 1, decimal? valorUnitario = null, string? observacaoEstado = null)
    {
        GarantirEditavel();
        if (servico.OficinaId != OficinaId)
            throw new RegraDeNegocioException("O serviço não é desta oficina.");
        if (!servico.Ativo)
            throw new RegraDeNegocioException($"O serviço \"{servico.Nome}\" está desativado.");

        var item = new ItemOrcamento(servico.Id, servico.PrecoMinimo, servico.Nome, quantidade,
            valorUnitario ?? servico.PrecoMinimo, observacaoEstado);
        _itens.Add(item);
        return item;
    }

    public ItemOrcamento AdicionarAvulso(string descricao, int quantidade, decimal valorUnitario, string? observacaoEstado = null)
    {
        GarantirEditavel();
        var item = new ItemOrcamento(null, null, descricao, quantidade, valorUnitario, observacaoEstado);
        _itens.Add(item);
        return item;
    }

    /// <summary>
    /// Ajusta um item já existente. O preço de catálogo guardado não muda, e a descrição de itens
    /// do catálogo também não (só a do avulso, que não tem de onde vir).
    /// </summary>
    public void AtualizarItem(Guid itemId, int quantidade, decimal valorUnitario, string? observacaoEstado,
        string? descricaoAvulso = null)
    {
        GarantirEditavel();
        var item = ObterItem(itemId);
        var ehAvulso = item.ServicoId is null;
        item.Atualizar(
            ehAvulso ? descricaoAvulso ?? item.Descricao : item.Descricao,
            quantidade,
            valorUnitario,
            observacaoEstado);
    }

    public void RemoverItem(Guid itemId)
    {
        GarantirEditavel();
        _itens.Remove(ObterItem(itemId));
    }

    public void DefinirDesconto(TipoDeDesconto tipo, decimal valor)
    {
        GarantirEditavel();
        if (tipo == TipoDeDesconto.Nenhum)
            valor = 0;
        if (valor < 0)
            throw new RegraDeNegocioException("O desconto não pode ser negativo.");
        if (tipo == TipoDeDesconto.Percentual && valor > 100)
            throw new RegraDeNegocioException("O desconto não pode passar de 100%.");
        if (tipo == TipoDeDesconto.Valor && valor > Subtotal)
            throw new RegraDeNegocioException("O desconto não pode ser maior que o subtotal.");

        DescontoTipo = tipo;
        DescontoValor = Dinheiro.Arredondar(valor);
    }

    public void DefinirValidade(DateOnly validade, DateOnly hoje)
    {
        GarantirEditavel();
        if (validade < hoje)
            throw new RegraDeNegocioException("A validade não pode ser uma data que já passou.");

        Validade = validade;
    }

    public void DefinirObservacoes(string? observacoes)
    {
        GarantirEditavel();
        observacoes = string.IsNullOrWhiteSpace(observacoes) ? null : observacoes.Trim();
        if (observacoes?.Length > ObservacoesTamanhoMaximo)
            throw new RegraDeNegocioException($"As observações podem ter no máximo {ObservacoesTamanhoMaximo} caracteres.");

        Observacoes = observacoes;
    }

    // ===== Mudanças de status =====

    public void MarcarComoEnviado(DateTimeOffset agora)
    {
        GarantirEditavel();
        GarantirQueTemItens();
        Status = StatusDoOrcamento.Enviado;
        EnviadoEm = agora; // reenviar (depois de uma alteração) atualiza a data
    }

    public void Aprovar(DateTimeOffset agora)
    {
        GarantirEmAberto("aprovado");
        GarantirQueTemItens();
        Status = StatusDoOrcamento.Aprovado;
        AprovadoEm = agora;
    }

    public void Recusar(DateTimeOffset agora)
    {
        GarantirEmAberto("recusado");
        Status = StatusDoOrcamento.Recusado;
        RecusadoEm = agora;
    }

    /// <summary>
    /// Um Enviado vira Expirado no dia seguinte ao fim da validade. Devolve true se mudou.
    /// Um Rascunho não expira: ninguém o recebeu ainda.
    /// </summary>
    public bool ExpirarSeVencido(DateOnly hoje)
    {
        if (Status != StatusDoOrcamento.Enviado || hoje <= Validade)
            return false;

        Status = StatusDoOrcamento.Expirado;
        return true;
    }

    /// <summary>
    /// Cria um rascunho novo com os mesmos cliente, veículo, itens, desconto e observações.
    /// É o caminho para "alterar" um orçamento que não pode mais ser editado (ex.: aprovado).
    /// Os itens mantêm os valores DESTE orçamento, não os preços atuais do catálogo.
    /// </summary>
    public Orcamento Duplicar(int novoAno, int novoSequencial, DateOnly hoje)
    {
        var copia = new Orcamento(OficinaId, novoAno, novoSequencial, ClienteId, VeiculoId, hoje);
        foreach (var item in _itens)
            copia._itens.Add(new ItemOrcamento(item.ServicoId, item.PrecoCatalogo, item.Descricao, item.Quantidade,
                item.ValorUnitario, item.ObservacaoEstado));

        copia.DescontoTipo = DescontoTipo;
        copia.DescontoValor = DescontoValor;
        copia.Observacoes = Observacoes;
        return copia;
    }

    /// <summary>Só rascunho pode ser excluído: os outros já foram vistos pelo cliente e ficam como histórico.</summary>
    public void GarantirQuePodeSerExcluido()
    {
        if (Status != StatusDoOrcamento.Rascunho)
            throw new RegraDeNegocioException(
                $"Só rascunhos podem ser excluídos. Este orçamento está {Status.ToString().ToLowerInvariant()} e fica no histórico.");
    }

    // ===== Agendamento (só depois de aprovado) =====

    public const int MaximoDeDiasAgendados = 60;

    /// <summary>
    /// Marca o período do serviço: o carro chega em "de" e é entregue em "ate" (pedido da equipe: o serviço
    /// quase nunca cabe num dia só). Reagendar é chamar de novo. Sem limite de carros por dia (decisão do dono).
    /// O carro chega e sai em dia aberto, mas pode FICAR num domingo no meio do período.
    /// </summary>
    public void Agendar(DateOnly de, DateOnly ate, DateOnly hoje)
    {
        if (Status != StatusDoOrcamento.Aprovado)
            throw new RegraDeNegocioException("Só um orçamento aprovado pode ser agendado.");
        if (de < hoje)
            throw new RegraDeNegocioException("Escolha um início de hoje em diante.");
        if (ate < de)
            throw new RegraDeNegocioException("O último dia não pode ser antes do primeiro.");
        if (ate.DayNumber - de.DayNumber + 1 > MaximoDeDiasAgendados)
            throw new RegraDeNegocioException($"O período pode ter no máximo {MaximoDeDiasAgendados} dias.");
        if (!Oficina.AbreEm(de) || !Oficina.AbreEm(ate))
            throw new RegraDeNegocioException("O carro precisa chegar e sair num dia em que a oficina abre.");

        AgendadoDe = de;
        AgendadoAte = ate;
    }

    public void CancelarAgendamento()
    {
        if (AgendadoDe is null)
            throw new RegraDeNegocioException("Este orçamento não tem agendamento.");

        AgendadoDe = null;
        AgendadoAte = null;
    }

    // ===== Regras auxiliares =====

    private void GarantirEditavel()
    {
        if (!PodeSerEditado)
            throw new RegraDeNegocioException(
                $"Um orçamento {Status.ToString().ToLowerInvariant()} não pode ser alterado. Use \"Duplicar\" para criar um novo.");
    }

    private void GarantirEmAberto(string acao)
    {
        if (!PodeSerEditado)
            throw new RegraDeNegocioException(
                $"Só um orçamento em rascunho ou enviado pode ser {acao} (este está {Status.ToString().ToLowerInvariant()}).");
    }

    private void GarantirQueTemItens()
    {
        if (_itens.Count == 0)
            throw new RegraDeNegocioException("Adicione pelo menos um item ao orçamento.");
    }

    private ItemOrcamento ObterItem(Guid itemId) =>
        _itens.FirstOrDefault(i => i.Id == itemId)
        ?? throw new RegraDeNegocioException("Item não encontrado neste orçamento.");
}
