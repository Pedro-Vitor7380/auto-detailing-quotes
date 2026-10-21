using System.Buffers.Binary;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Orcamentos.Domain.Entidades;

namespace Orcamentos.Infrastructure.Notificacoes;

/// <summary>Par de chaves VAPID: identifica a NOSSA API para os serviços de notificação. Base64url.</summary>
/// <param name="Contato">Um "mailto:" ou https para o serviço de notificação falar com o dono, se precisar.</param>
public record ChavesVapid(string Publica, string Privada, string Contato);

public enum ResultadoDoEnvio { Enviado, InscricaoExpirada, Falhou }

/// <summary>
/// Envia notificações "Web Push" sem biblioteca de terceiros, só com a criptografia do .NET.
/// (As bibliotecas .NET disponíveis dependem do Portable.BouncyCastle, abandonado e com falhas conhecidas.)
///
/// Duas especificações públicas:
/// - RFC 8292 (VAPID): provar ao serviço de notificação QUEM está enviando (um "crachá" assinado com ECDSA).
/// - RFC 8291 (aes128gcm): CRIPTOGRAFAR a mensagem para só aquele aparelho conseguir ler. Nem o Google,
///   a Apple ou a Microsoft, que entregam a notificação, enxergam o conteúdo.
/// </summary>
public class EnviadorDeNotificacoes(HttpClient http, ChavesVapid chaves)
{
    /// <summary>Gera um par novo de chaves VAPID (curva P-256). Rodado uma vez, pelo comando gerar-chaves-notificacao.</summary>
    public static (string Publica, string Privada) GerarChaves()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var p = ecdsa.ExportParameters(includePrivateParameters: true);
        return (Base64Url(PontoNaoComprimido(p.Q)), Base64Url(p.D!));
    }

    public async Task<ResultadoDoEnvio> EnviarAsync(InscricaoDeNotificacao inscricao, object conteudo, CancellationToken ct)
    {
        var endpoint = new Uri(inscricao.Endpoint);
        var corpo = Criptografar(JsonSerializer.SerializeToUtf8Bytes(conteudo, JsonSerializerOptions.Web),
            DeBase64Url(inscricao.P256dh), DeBase64Url(inscricao.Auth));

        using var pedido = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = new ByteArrayContent(corpo) };
        pedido.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        pedido.Content.Headers.ContentEncoding.Add("aes128gcm");
        pedido.Headers.Add("TTL", "86400"); // se o celular estiver desligado, o serviço guarda por até 1 dia
        pedido.Headers.Add("Urgency", "normal");
        pedido.Headers.TryAddWithoutValidation("Authorization",
            $"vapid t={TokenVapid($"{endpoint.Scheme}://{endpoint.Host}")}, k={chaves.Publica}");

        using var resposta = await http.SendAsync(pedido, ct);
        // 404/410: o aparelho cancelou a inscrição (desinstalou o app, limpou os dados...). Ela pode ser apagada.
        if (resposta.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
            return ResultadoDoEnvio.InscricaoExpirada;
        return resposta.IsSuccessStatusCode ? ResultadoDoEnvio.Enviado : ResultadoDoEnvio.Falhou;
    }

    // ===== RFC 8292: o "crachá" (um JWT assinado com a chave privada VAPID) =====

    private string TokenVapid(string audiencia)
    {
        var cabecalho = Base64Url(Encoding.UTF8.GetBytes("""{"typ":"JWT","alg":"ES256"}"""));
        var expira = DateTimeOffset.UtcNow.AddHours(12).ToUnixTimeSeconds(); // o máximo permitido é 24 h
        var dados = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new { aud = audiencia, exp = expira, sub = chaves.Contato }));
        var assinar = Encoding.ASCII.GetBytes($"{cabecalho}.{dados}");

        using var ecdsa = ECDsa.Create(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            D = DeBase64Url(chaves.Privada),
            Q = PontoDe(DeBase64Url(chaves.Publica)),
        });
        // ES256 pede a assinatura no formato "r || s" (64 bytes), que é o padrão do ECDsa no .NET.
        var assinatura = ecdsa.SignData(assinar, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        return $"{cabecalho}.{dados}.{Base64Url(assinatura)}";
    }

    // ===== RFC 8291: criptografia da mensagem para um aparelho =====

    private static byte[] Criptografar(byte[] mensagem, byte[] chavePublicaDoAparelho, byte[] segredoDoAparelho)
    {
        // 1. Um par de chaves NOVO a cada mensagem (descartável) e o segredo combinado com o aparelho (ECDH).
        using var nossa = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        using var doAparelho = ECDiffieHellman.Create(new ECParameters { Curve = ECCurve.NamedCurves.nistP256, Q = PontoDe(chavePublicaDoAparelho) });
        var segredoEcdh = nossa.DeriveRawSecretAgreement(doAparelho.PublicKey);
        var nossaPublica = PontoNaoComprimido(nossa.ExportParameters(false).Q);

        // 2. Mistura o segredo ECDH com o "auth" do aparelho (HKDF), como manda a RFC.
        var infoChave = Juntar(Encoding.ASCII.GetBytes("WebPush: info\0"), chavePublicaDoAparelho, nossaPublica);
        var ikm = HKDF.DeriveKey(HashAlgorithmName.SHA256, segredoEcdh, 32, salt: segredoDoAparelho, info: infoChave);

        // 3. Chave e "nonce" do AES-GCM, a partir de um sal aleatório.
        var sal = RandomNumberGenerator.GetBytes(16);
        var chave = HKDF.DeriveKey(HashAlgorithmName.SHA256, ikm, 16, sal, Encoding.ASCII.GetBytes("Content-Encoding: aes128gcm\0"));
        var nonce = HKDF.DeriveKey(HashAlgorithmName.SHA256, ikm, 12, sal, Encoding.ASCII.GetBytes("Content-Encoding: nonce\0"));

        // 4. Criptografa (mensagem + byte 0x02, que marca "último bloco") e anexa a etiqueta de 16 bytes.
        var aberto = Juntar(mensagem, [0x02]);
        var cifrado = new byte[aberto.Length];
        var etiqueta = new byte[16];
        using (var aes = new AesGcm(chave, tagSizeInBytes: 16))
            aes.Encrypt(nonce, aberto, cifrado, etiqueta);

        // 5. Cabeçalho: sal (16) + tamanho do bloco (4, big-endian) + tamanho da chave (1) + nossa chave pública (65).
        var tamanhoDoBloco = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(tamanhoDoBloco, 4096);
        return Juntar(sal, tamanhoDoBloco, [(byte)nossaPublica.Length], nossaPublica, cifrado, etiqueta);
    }

    // ===== Utilitários =====

    /// <summary>Chave pública no formato "não comprimido": 0x04 + X (32 bytes) + Y (32 bytes).</summary>
    private static byte[] PontoNaoComprimido(ECPoint q) => Juntar([0x04], q.X!, q.Y!);

    private static ECPoint PontoDe(byte[] naoComprimido) => new() { X = naoComprimido[1..33], Y = naoComprimido[33..65] };

    private static byte[] Juntar(params byte[][] partes) => partes.SelectMany(p => p).ToArray();

    /// <summary>Base64 "de URL": sem "=", com "-" e "_" no lugar de "+" e "/". É o formato dos navegadores.</summary>
    public static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] DeBase64Url(string texto)
    {
        var b64 = texto.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(b64.PadRight(b64.Length + (4 - b64.Length % 4) % 4, '='));
    }
}
