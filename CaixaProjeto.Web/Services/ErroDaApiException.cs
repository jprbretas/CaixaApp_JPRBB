using System.Net;

namespace CaixaProjeto.Web.Services;

/// <summary>
/// A API respondeu, mas com um erro (por exemplo, 500 quando algo correu mal do lado dela).
/// É diferente de não conseguir contactar a API, e por isso tem uma exceção própria.
/// Deriva de HttpRequestException para as páginas continuarem a apanhar os dois casos no mesmo catch.
/// </summary>
public class ErroDaApiException(HttpStatusCode estado, string? detalhe, string? codigo)
    : HttpRequestException(detalhe ?? $"A API respondeu com o erro {(int)estado}.", null, estado)
{
    /// <summary>A explicação que a API deu (campo "detail" da resposta), se deu alguma.</summary>
    public string? Detalhe { get; } = detalhe;

    /// <summary>
    /// O código do pedido nos logs da API (campo "traceId" da resposta).
    /// Com ele, o suporte encontra logo o erro nos logs.
    /// </summary>
    public string? Codigo { get; } = codigo;
}

/// <summary>A frase que as páginas mostram quando uma chamada à API falha.</summary>
public static class MensagensErro
{
    public const string SemLigacao = "Não foi possível contactar a API. Confirme que a app foi arrancada pelo AppHost.";

    public static string Para(HttpRequestException erro)
    {
        // A API respondeu com um erro: dizer isso, e dar o código para o suporte
        if (erro is ErroDaApiException erroDaApi)
        {
            var mensagem = $"A API respondeu com um erro ({(int)erroDaApi.StatusCode!}). Tente de novo dentro de momentos.";
            if (erroDaApi.Codigo is not null)
            {
                mensagem += $" Se o problema continuar, contacte o suporte e indique o código {erroDaApi.Codigo}.";
            }
            return mensagem;
        }

        // Nem chegou a haver resposta: a API está em baixo ou inacessível
        return SemLigacao;
    }
}
