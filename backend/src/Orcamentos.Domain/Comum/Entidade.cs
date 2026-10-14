namespace Orcamentos.Domain.Comum;

/// <summary>
/// Base de todas as entidades: tudo que tem identidade própria e é salvo no banco.
/// </summary>
public abstract class Entidade
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public DateTimeOffset CriadoEm { get; private set; } = DateTimeOffset.UtcNow;
}
