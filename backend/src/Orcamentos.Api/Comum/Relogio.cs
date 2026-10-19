namespace Orcamentos.Api.Comum;

/// <summary>
/// "Que horas são?" e "que dia é hoje?" no fuso da oficina.
/// O servidor (Render) roda em UTC: às 22h de São Paulo já é o dia seguinte em UTC.
/// Sem converter, um orçamento expiraria 3 horas antes da hora.
/// TimeProvider é a abstração de relógio do .NET: nos testes, dá para trocar por um relógio "parado".
/// </summary>
public class Relogio(TimeProvider tempo)
{
    // IANA ("America/Sao_Paulo") funciona no Linux do container e no Windows (o .NET converte).
    private static readonly TimeZoneInfo FusoDaOficina = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public DateTimeOffset Agora => tempo.GetUtcNow();

    public DateOnly Hoje => DataNoFuso(Agora);

    /// <summary>O dia de um instante, no fuso da oficina (ex.: a data de emissão impressa no PDF).</summary>
    public static DateOnly DataNoFuso(DateTimeOffset instante) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instante, FusoDaOficina).DateTime);
}
