using System.Net;
using System.Text;
using CaixaProjeto.Core.Contratos;
using CaixaProjeto.Core.Dominio;
using CaixaProjeto.Web.Services;

namespace CaixaProjeto.WebTests;

/// <summary>
/// Faz de API: em vez de enviar o pedido pela rede, devolve a resposta que o teste escolher
/// (ou lança uma exceção, como quando a API está em baixo).
/// </summary>
public class ApiFalsa(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage pedido, CancellationToken cancellationToken)
    {
        return Task.FromResult(responder(pedido));
    }
}

/// <summary>
/// Testes do CreditoApiClient e das mensagens de erro das páginas: distinguir "a API não responde"
/// de "a API respondeu com um erro", e dar ao utilizador o código para o suporte.
/// </summary>
public class CreditoApiClientTests
{
    private static readonly PedidoCredito Pedido = new()
    {
        Nif = "123456789",
        Idade = 35,
        RendimentoMensalLiquido = 2500,
        PrestacoesAtuais = 200,
        ValorPretendido = 10000,
        PrazoMeses = 60,
        SituacaoProfissional = SituacaoProfissional.Efetivo
    };

    private static readonly DecisaoAnalista Decisao = new(true, "Ana Silva", "Ok.");

    private static CreditoApiClient Cliente(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var httpClient = new HttpClient(new ApiFalsa(responder)) { BaseAddress = new Uri("http://api-de-teste") };
        return new CreditoApiClient(httpClient);
    }

    /// <summary>Uma resposta de erro como a API a envia: formato Problem Details, com o traceId.</summary>
    private static HttpResponseMessage Problema(HttpStatusCode estado, string? detalhe, string traceId = "00-abc123-01")
    {
        var campoDetalhe = "";
        if (detalhe is not null)
        {
            campoDetalhe = $"\"detail\":\"{detalhe}\",";
        }
        var json = $"{{\"status\":{(int)estado},{campoDetalhe}\"traceId\":\"{traceId}\"}}";
        return new HttpResponseMessage(estado)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/problem+json")
        };
    }

    // ---------- A API respondeu com um erro ----------

    [Fact]
    public async Task Erro500_LancaErroDaApi_ComOCodigoParaOSuporte()
    {
        var api = Cliente(_ => Problema(HttpStatusCode.InternalServerError, null, "00-4bf92f-01"));

        var erro = await Assert.ThrowsAsync<ErroDaApiException>(() => api.PreAnalisarAsync(Pedido));

        Assert.Equal(HttpStatusCode.InternalServerError, erro.StatusCode);
        Assert.Equal("00-4bf92f-01", erro.Codigo);
    }

    [Fact]
    public async Task Erro500_ComCorpoQueNaoEJson_LancaErroDaApi_SemCodigo()
    {
        // Por exemplo, uma página HTML devolvida por um proxy
        var api = Cliente(_ => new HttpResponseMessage(HttpStatusCode.BadGateway) { Content = new StringContent("<html>Bad Gateway</html>") });

        var erro = await Assert.ThrowsAsync<ErroDaApiException>(() => api.ListarAsync(null, 1, 20));

        Assert.Equal(HttpStatusCode.BadGateway, erro.StatusCode);
        Assert.Null(erro.Codigo);
    }

    [Fact]
    public async Task TodosOsMetodos_LancamErroDaApi_QuandoAApiRespondeComErro()
    {
        var api = Cliente(_ => Problema(HttpStatusCode.InternalServerError, null));

        await Assert.ThrowsAsync<ErroDaApiException>(() => api.PreAnalisarAsync(Pedido));
        await Assert.ThrowsAsync<ErroDaApiException>(() => api.SubmeterAsync(Pedido));
        await Assert.ThrowsAsync<ErroDaApiException>(() => api.ListarAsync(null, 1, 20));
        await Assert.ThrowsAsync<ErroDaApiException>(() => api.ObterAsync("20260001"));
        await Assert.ThrowsAsync<ErroDaApiException>(() => api.DecidirAsync("20260001", Decisao));
    }

    // ---------- Respostas que não são erros técnicos ----------

    [Fact]
    public async Task Obter_404_DevolveNull()
    {
        var api = Cliente(_ => Problema(HttpStatusCode.NotFound, null));

        Assert.Null(await api.ObterAsync("99999999"));
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "Indique o nome do analista e a observação.")]
    [InlineData(HttpStatusCode.NotFound, "O pedido 99999999 não existe.")]
    [InlineData(HttpStatusCode.Conflict, "Este pedido não está em análise manual, por isso já não pode ser decidido.")]
    public async Task Decidir_DecisaoNaoAceite_DevolveAExplicacaoDaApi(HttpStatusCode estado, string explicacao)
    {
        var api = Cliente(_ => Problema(estado, explicacao));

        Assert.Equal(explicacao, await api.DecidirAsync("99999999", Decisao));
    }

    // ---------- As mensagens que as páginas mostram ----------

    [Fact]
    public async Task Mensagem_SemLigacao_DizQueNaoFoiPossivelContactarAApi()
    {
        // Como quando a API está em baixo: o HttpClient nem recebe resposta
        var api = Cliente(_ => throw new HttpRequestException("Ligação recusada"));

        var erro = await Assert.ThrowsAsync<HttpRequestException>(() => api.ListarAsync(null, 1, 20));

        Assert.Null(erro.StatusCode);
        Assert.Equal(MensagensErro.SemLigacao, MensagensErro.Para(erro));
    }

    [Fact]
    public async Task Mensagem_ErroDaApi_DizQueAApiRespondeuComErro_EDaOCodigo()
    {
        var api = Cliente(_ => Problema(HttpStatusCode.InternalServerError, null, "00-4bf92f-01"));

        var erro = await Assert.ThrowsAsync<ErroDaApiException>(() => api.SubmeterAsync(Pedido));
        var mensagem = MensagensErro.Para(erro);

        Assert.StartsWith("A API respondeu com um erro (500).", mensagem);
        Assert.Contains("00-4bf92f-01", mensagem);
        Assert.NotEqual(MensagensErro.SemLigacao, mensagem);
    }

    [Fact]
    public void Mensagem_ErroDaApiSemCodigo_NaoFalaEmCodigo()
    {
        var erro = new ErroDaApiException(HttpStatusCode.BadGateway, null, null);

        Assert.DoesNotContain("código", MensagensErro.Para(erro));
    }
}
