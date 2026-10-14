namespace Orcamentos.Domain.Comum;

/// <summary>
/// Base das entidades que pertencem a uma oficina (usuários, clientes, serviços, orçamentos...).
/// O OficinaId é o que separa os dados de cada oficina quando o sistema atender várias.
/// </summary>
public abstract class EntidadeDaOficina : Entidade
{
    public Guid OficinaId { get; private set; }

    protected EntidadeDaOficina(Guid oficinaId)
    {
        if (oficinaId == Guid.Empty)
            throw new RegraDeNegocioException("A entidade precisa pertencer a uma oficina.");

        OficinaId = oficinaId;
    }

    // Construtor vazio exigido pelo Entity Framework para recriar o objeto a partir do banco.
    protected EntidadeDaOficina() { }
}
