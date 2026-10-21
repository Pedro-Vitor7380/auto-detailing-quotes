using Orcamentos.Domain.Comum;
using Orcamentos.Domain.Entidades;

namespace Orcamentos.Domain.Tests.Entidades;

public class InscricaoDeNotificacaoTests
{
    [Fact]
    public void Criar_ComEnderecoHttps_Aceita()
    {
        var inscricao = new InscricaoDeNotificacao(Guid.NewGuid(), "https://fcm.googleapis.com/fcm/send/abc", "chave", "segredo");

        Assert.Equal("https://fcm.googleapis.com/fcm/send/abc", inscricao.Endpoint);
    }

    [Theory]
    [InlineData("http://inseguro.com/x")]   // sem https
    [InlineData("nao-e-endereco")]
    public void Criar_ComEnderecoInvalido_LancaRegraDeNegocio(string endpoint)
    {
        Assert.Throws<RegraDeNegocioException>(() => new InscricaoDeNotificacao(Guid.NewGuid(), endpoint, "chave", "segredo"));
    }

    [Fact]
    public void Criar_SemChaves_LancaRegraDeNegocio()
    {
        Assert.Throws<RegraDeNegocioException>(() => new InscricaoDeNotificacao(Guid.NewGuid(), "https://x.com/y", "", "segredo"));
    }
}
