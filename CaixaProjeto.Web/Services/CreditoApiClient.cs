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
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<ResultadoAnalise>(Json, cancellationToken))!;
    }

    public async Task<PedidoSubmetido> SubmeterAsync(PedidoCredito pedido, CancellationToken cancellationToken = default)
    {
        var resposta = await httpClient.PostAsJsonAsync("/api/pedidos", pedido, Json, cancellationToken);
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<PedidoSubmetido>(Json, cancellationToken))!;
    }

    public async Task<Pagina<PedidoResumo>> ListarAsync(Decisao? estado, int pagina, int tamanho, CancellationToken cancellationToken = default)
    {
        var url = $"/api/pedidos?pagina={pagina}&tamanho={tamanho}";
        if (estado is not null)
        {
            url += $"&estado={estado}";
        }
        return (await httpClient.GetFromJsonAsync<Pagina<PedidoResumo>>(url, Json, cancellationToken))!;
    }

    /// <summary>Devolve o detalhe do pedido, ou null se a API responder 404 (o pedido não existe).</summary>
    public async Task<PedidoDetalhe?> ObterAsync(string numero, CancellationToken cancellationToken = default)
    {
        var resposta = await httpClient.GetAsync($"/api/pedidos/{Uri.EscapeDataString(numero)}", cancellationToken);
        if (resposta.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        resposta.EnsureSuccessStatusCode();
        return await resposta.Content.ReadFromJsonAsync<PedidoDetalhe>(Json, cancellationToken);
    }

    /// <summary>
    /// Envia a decisão do analista. Devolve null se correu bem,
    /// ou a mensagem de erro que a API explicou (campo "detail" da resposta).
    /// </summary>
    public async Task<string?> DecidirAsync(string numero, DecisaoAnalista decisao, CancellationToken cancellationToken = default)
    {
        var resposta = await httpClient.PostAsJsonAsync($"/api/pedidos/{Uri.EscapeDataString(numero)}/decisao", decisao, Json, cancellationToken);
        if (resposta.IsSuccessStatusCode)
        {
            return null;
        }

        // Os erros da API vêm no formato "Problem Details": { "status": 409, "detail": "..." }
        var problema = await resposta.Content.ReadFromJsonAsync<ProblemDetails>(Json, cancellationToken);
        if (problema?.Detail is not null)
        {
            return problema.Detail;
        }
        return $"A API respondeu com o erro {(int)resposta.StatusCode}.";
    }
}
