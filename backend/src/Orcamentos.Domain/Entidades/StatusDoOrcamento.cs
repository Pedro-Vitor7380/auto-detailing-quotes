namespace Orcamentos.Domain.Entidades;

/// <summary>
/// Ciclo de vida: Rascunho → Enviado → Aprovado / Recusado. Enviado vira Expirado quando passa da validade.
/// Guardado no banco como texto ("Rascunho"), para quem ler a tabela entender sem consultar o código.
/// </summary>
public enum StatusDoOrcamento
{
    Rascunho,
    Enviado,
    Aprovado,
    Recusado,
    Expirado,
}

public enum TipoDeDesconto
{
    Nenhum,
    Valor,
    Percentual,
}
