using CaixaProjeto.Core.Dominio;

namespace CaixaProjeto.Core.Contratos;

// Objetos que viajam em JSON entre a API e a Web; ficam no Core para os dois lados usarem as mesmas classes.

public sealed record PedidoSubmetido(string Numero, ResultadoAnalise Resultado);

public sealed record PedidoResumo(
    string Numero,
    string? Nif,
    decimal ValorPretendido,
    int PrazoMeses,
    decimal? TaxaEsforco,
    Decisao DecisaoAutomatica,
    Decisao EstadoAtual,
    DateTime DataSubmissao);

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

public sealed record EstadoHistorico(
    Decisao? EstadoAnterior,
    Decisao EstadoNovo,
    DateTime Data,
    string Utilizador,
    string? Observacao);

public sealed record PedidoDetalhe(
    string Numero,
    DateTime DataSubmissao,
    PedidoCredito Dados,
    ResultadoAnalise Resultado,
    Decisao EstadoAtual,
    IReadOnlyList<EstadoHistorico> Historico)
{
    public bool AguardaAnalista
    {
        get { return EstadoAtual == Decisao.AnaliseManual; }
    }
}

public sealed record DecisaoAnalista(bool Aprovar, string Utilizador, string Observacao);
