namespace CaixaProjeto.Core.Dominio;

public sealed record Indicadores(
    decimal PrestacaoEstimada,
    decimal TaxaEsforco,
    decimal IdadeFinalContrato,
    decimal LimiteMontante);

/// <summary>A regra que disparou e a decisão que ela pede (não a decisão final).</summary>
public sealed record Motivo(int Regra, string Descricao, Decisao Decisao);

/// <summary>Os indicadores ficam a null quando o pedido é inválido: não são calculáveis.</summary>
public sealed record ResultadoAnalise(
    Decisao Decisao,
    IReadOnlyList<Motivo> Motivos,
    Indicadores? Indicadores);
