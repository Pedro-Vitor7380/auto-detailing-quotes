using Orcamentos.Domain.Comum;

namespace Orcamentos.Domain.Entidades;

/// <summary>
/// Um celular (ou computador) que aceitou receber notificações. O navegador entrega três dados:
/// o endereço para onde mandar (Endpoint, no serviço de notificações do Google, da Apple ou da Microsoft)
/// e duas chaves (P256dh e Auth) para CRIPTOGRAFAR a mensagem: só aquele aparelho consegue ler.
/// </summary>
public class InscricaoDeNotificacao : EntidadeDaOficina
{
    public const int EndpointTamanhoMaximo = 1000;

    public string Endpoint { get; private set; } = null!;
    public string P256dh { get; private set; } = null!;
    public string Auth { get; private set; } = null!;

    public InscricaoDeNotificacao(Guid oficinaId, string endpoint, string p256dh, string auth) : base(oficinaId)
    {
        AtualizarChaves(endpoint, p256dh, auth);
    }

    // Construtor vazio exigido pelo Entity Framework para recriar o objeto a partir do banco.
    private InscricaoDeNotificacao() { }

    /// <summary>O navegador pode trocar as chaves do mesmo aparelho; o endereço identifica a inscrição.</summary>
    public void AtualizarChaves(string endpoint, string p256dh, string auth)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new RegraDeNegocioException("Endereço de notificação inválido.");
        if (endpoint.Length > EndpointTamanhoMaximo)
            throw new RegraDeNegocioException("Endereço de notificação longo demais.");
        if (string.IsNullOrWhiteSpace(p256dh) || string.IsNullOrWhiteSpace(auth))
            throw new RegraDeNegocioException("Chaves de notificação ausentes.");

        Endpoint = endpoint;
        P256dh = p256dh;
        Auth = auth;
    }
}
