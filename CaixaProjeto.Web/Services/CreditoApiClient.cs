using System.Text.Json;
using System.Text.Json.Serialization;
using CaixaProjeto.Core.Contratos;
using CaixaProjeto.Core.Dominio;

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
}
