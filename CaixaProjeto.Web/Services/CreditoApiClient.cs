using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using CaixaProjeto.Core.Contratos;
using CaixaProjeto.Core.Dominio;
using Microsoft.AspNetCore.Mvc;

namespace CaixaProjeto.Web.Services;

/// <summary>
/// Sem ligação à API, o HttpClient lança HttpRequestException; se a API responder com um erro,
/// este cliente lança ErroDaApiException, com o código para o suporte.
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

    /// <summary>Devolve null se o pedido não existir (404).</summary>
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
    /// Devolve null se correu bem, ou a explicação da API se a decisão não for aceite (400, 404, 409).
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
            var problema = await LerProblemaAsync(resposta, cancellationToken);
            if (problema?.Detail is not null)
            {
                return problema.Detail;
            }
            return $"A API respondeu com o erro {(int)resposta.StatusCode}.";
        }

        throw await CriarErroAsync(resposta, cancellationToken);
    }

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
    /// Lê um erro no formato Problem Details; null se o corpo não for JSON (por exemplo, o HTML de um proxy).
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
