using System.Net;

namespace CaixaProjeto.Web.Services;

/// <summary>
/// A API respondeu, mas com um erro (diferente de não haver ligação). Deriva de HttpRequestException
/// para as páginas apanharem os dois casos no mesmo catch.
/// </summary>
public class ErroDaApiException(HttpStatusCode estado, string? detalhe, string? codigo)
    : HttpRequestException(detalhe ?? $"A API respondeu com o erro {(int)estado}.", null, estado)
{
    public string? Detalhe { get; } = detalhe;

    /// <summary>O traceId da resposta: o código que o suporte procura nos logs.</summary>
    public string? Codigo { get; } = codigo;
}

public static class MensagensErro
{
    public const string SemLigacao = "Não foi possível contactar a API. Confirme que a app foi arrancada pelo AppHost.";

    public static string Para(HttpRequestException erro)
    {
        if (erro is ErroDaApiException erroDaApi)
        {
            var mensagem = $"A API respondeu com um erro ({(int)erroDaApi.StatusCode!}). Tente de novo dentro de momentos.";
            if (erroDaApi.Codigo is not null)
            {
                mensagem += $" Se o problema continuar, contacte o suporte e indique o código {erroDaApi.Codigo}.";
            }
            return mensagem;
        }

        // Nem houve resposta: a API está em baixo ou inacessível
        return SemLigacao;
    }
}
