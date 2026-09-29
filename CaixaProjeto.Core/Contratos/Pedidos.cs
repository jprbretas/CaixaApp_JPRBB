using CaixaProjeto.Core.Dominio;

namespace CaixaProjeto.Core.Contratos;

// "Contratos" são os objetos que viajam em JSON entre a API e a Web.
// Ficam no Core para os dois lados usarem exatamente as mesmas classes.

/// <summary>Resposta da API quando um pedido é submetido e gravado: o número atribuído e o resultado.</summary>
public sealed record PedidoSubmetido(string Numero, ResultadoAnalise Resultado);

/// <summary>Uma linha da lista de pedidos.</summary>
public sealed record PedidoResumo(
    string Numero,
    string? Nif,
    decimal ValorPretendido,
    int PrazoMeses,
    decimal? TaxaEsforco,
    Decisao DecisaoAutomatica,
    Decisao EstadoAtual,
    DateTime DataSubmissao);

/// <summary>Uma página de resultados: os itens desta página e o total que existe.</summary>
public sealed record Pagina<T>(IReadOnlyList<T> Itens, int Total, int NumeroPagina, int TamanhoPagina)
{
    public int TotalPaginas
    {
        get
        {
            if (Total == 0)
            {
                return 1;
            }
            return (Total + TamanhoPagina - 1) / TamanhoPagina;
        }
    }
}

/// <summary>Uma mudança de estado do pedido (decisão automática ou do analista).</summary>
public sealed record EstadoHistorico(
    Decisao? EstadoAnterior,
    Decisao EstadoNovo,
    DateTime Data,
    string Utilizador,
    string? Observacao);

/// <summary>Tudo o que se sabe de um pedido: dados, resultado da análise, estado atual e histórico.</summary>
public sealed record PedidoDetalhe(
    string Numero,
    DateTime DataSubmissao,
    PedidoCredito Dados,
    ResultadoAnalise Resultado,
    Decisao EstadoAtual,
    IReadOnlyList<EstadoHistorico> Historico)
{
    /// <summary>Só os pedidos em análise manual esperam pela decisão de um analista.</summary>
    public bool AguardaAnalista
    {
        get { return EstadoAtual == Decisao.AnaliseManual; }
    }
}

/// <summary>O que o analista envia ao decidir um pedido em análise manual.</summary>
public sealed record DecisaoAnalista(bool Aprovar, string Utilizador, string Observacao);
