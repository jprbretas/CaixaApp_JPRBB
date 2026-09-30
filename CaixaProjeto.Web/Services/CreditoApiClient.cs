using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using CaixaProjeto.Core.Contratos;
using CaixaProjeto.Core.Dominio;
using Microsoft.AspNetCore.Mvc;

namespace CaixaProjeto.Web.Services;

/// <summary>
/// Cliente HTTP da ApiService. O endereço base ("https+http://apiservice") é resolvido
/// pelo service discovery do Aspire, por isso a Web não precisa de saber a porta da API.
///
/// Erros: se não houver ligação à API, o HttpClient lança HttpRequestException;
/// se a API responder com um erro, este cliente lança ErroDaApiException (com o código para o suporte).
/// </summary>
public class CreditoApiClient(HttpClient httpClient)
{
    // Tem de bater certo com a API: enums como texto, nomes em camelCase
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<ResultadoAnalise> PreAnalisarAsync(PedidoCredito pedido, CancellationToken cancellationToken = default)
    {
        var resposta = await httpClient.PostAsJsonAsync("/api/pedidos/preanalise", pedido, Json, cancellationToken);
        if (!resposta.IsSuccessStatusCode)
        {
            throw await CriarErroAsync(resposta, cancellationToken);
        }
        return (await resposta.Content.ReadFromJsonAsync<ResultadoAnalise>(Json, cancellationToken))!;
    }

    public async Task<PedidoSubmetido> SubmeterAsync(PedidoCredito pedido, CancellationToken cancellationToken = default)
    {
        var resposta = await httpClient.PostAsJsonAsync("/api/pedidos", pedido, Json, cancellationToken);
        if (!resposta.IsSuccessStatusCode)
        {
            throw await CriarErroAsync(resposta, cancellationToken);
        }
        return (await resposta.Content.ReadFromJsonAsync<PedidoSubmetido>(Json, cancellationToken))!;
    }

    public async Task<Pagina<PedidoResumo>> ListarAsync(Decisao? estado, int pagina, int tamanho, CancellationToken cancellationToken = default)
    {
        var url = $"/api/pedidos?pagina={pagina}&tamanho={tamanho}";
        if (estado is not null)
        {
            url += $"&estado={estado}";
        }

        var resposta = await httpClient.GetAsync(url, cancellationToken);
        if (!resposta.IsSuccessStatusCode)
        {
            throw await CriarErroAsync(resposta, cancellationToken);
        }
        return (await resposta.Content.ReadFromJsonAsync<Pagina<PedidoResumo>>(Json, cancellationToken))!;
    }

    /// <summary>Devolve o detalhe do pedido, ou null se a API responder 404 (o pedido não existe).</summary>
    public async Task<PedidoDetalhe?> ObterAsync(string numero, CancellationToken cancellationToken = default)
    {
        var resposta = await httpClient.GetAsync($"/api/pedidos/{Uri.EscapeDataString(numero)}", cancellationToken);
        if (resposta.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        if (!resposta.IsSuccessStatusCode)
        {
            throw await CriarErroAsync(resposta, cancellationToken);
        }
        return await resposta.Content.ReadFromJsonAsync<PedidoDetalhe>(Json, cancellationToken);
    }

    /// <summary>
    /// Envia a decisão do analista. Devolve null se correu bem, ou a mensagem que a API explicou
    /// quando a decisão não é aceite (400 dados em falta, 404 não existe, 409 já não está em análise manual).
    /// Qualquer outro erro da API lança ErroDaApiException, como nos outros métodos.
    /// </summary>
    public async Task<string?> DecidirAsync(string numero, DecisaoAnalista decisao, CancellationToken cancellationToken = default)
    {
        var resposta = await httpClient.PostAsJsonAsync($"/api/pedidos/{Uri.EscapeDataString(numero)}/decisao", decisao, Json, cancellationToken);
        if (resposta.IsSuccessStatusCode)
        {
            return null;
        }

        if (resposta.StatusCode == HttpStatusCode.BadRequest
            || resposta.StatusCode == HttpStatusCode.NotFound
            || resposta.StatusCode == HttpStatusCode.Conflict)
        {
            // Decisão recusada por uma regra: a API explica porquê no campo "detail"
            var problema = await LerProblemaAsync(resposta, cancellationToken);
            if (problema?.Detail is not null)
            {
                return problema.Detail;
            }
            return $"A API respondeu com o erro {(int)resposta.StatusCode}.";
        }

        throw await CriarErroAsync(resposta, cancellationToken);
    }

    /// <summary>Transforma uma resposta de erro numa ErroDaApiException, com a explicação e o código da API.</summary>
    private static async Task<ErroDaApiException> CriarErroAsync(HttpResponseMessage resposta, CancellationToken cancellationToken)
    {
        var problema = await LerProblemaAsync(resposta, cancellationToken);

        string? codigo = null;
        if (problema is not null && problema.Extensions.TryGetValue("traceId", out var traceId))
        {
            codigo = traceId?.ToString();
        }

        return new ErroDaApiException(resposta.StatusCode, problema?.Detail, codigo);
    }

    /// <summary>
    /// Lê o corpo de uma resposta de erro no formato "Problem Details": { "status": 500, "detail": "...", "traceId": "..." }.
    /// Devolve null se o corpo vier vazio ou noutro formato (por exemplo, uma página HTML de um proxy).
    /// </summary>
    private static async Task<ProblemDetails?> LerProblemaAsync(HttpResponseMessage resposta, CancellationToken cancellationToken)
    {
        var corpo = await resposta.Content.ReadAsStringAsync(cancellationToken);
        try
        {
            return JsonSerializer.Deserialize<ProblemDetails>(corpo, Json);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
