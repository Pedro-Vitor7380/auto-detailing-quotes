using Microsoft.EntityFrameworkCore;

namespace Orcamentos.Infrastructure.Dados;

/// <summary>
/// Último número de orçamento usado, por oficina e por ano. É um detalhe de banco (não uma regra de
/// negócio), por isso mora na Infraestrutura e não no Domínio.
/// </summary>
public class ContadorDeOrcamentos
{
    public Guid OficinaId { get; private set; }
    public int Ano { get; private set; }
    public int Ultimo { get; private set; }

    /// <summary>
    /// Soma 1 e devolve o novo número, numa única instrução atômica do PostgreSQL:
    /// - primeiro orçamento do ano: INSERT com 1;
    /// - os seguintes: ON CONFLICT ... DO UPDATE soma 1 ao que já existe.
    /// A linha fica travada até o fim da transação: um segundo pedido simultâneo ESPERA o primeiro
    /// terminar e recebe o número seguinte, em vez de pegar o mesmo. Precisa rodar dentro de uma transação.
    /// </summary>
    public static async Task<int> ProximoAsync(AppDbContext db, Guid oficinaId, int ano, CancellationToken ct)
    {
        // FormattableString ($"..."): o EF transforma {oficinaId} e {ano} em parâmetros (sem risco de SQL injection).
        var resultado = await db.Database.SqlQuery<int>($"""
            INSERT INTO contadores_de_orcamentos (oficina_id, ano, ultimo) VALUES ({oficinaId}, {ano}, 1)
            ON CONFLICT (oficina_id, ano) DO UPDATE SET ultimo = contadores_de_orcamentos.ultimo + 1
            RETURNING ultimo AS "Value"
            """).ToListAsync(ct);

        return resultado[0];
    }
}
