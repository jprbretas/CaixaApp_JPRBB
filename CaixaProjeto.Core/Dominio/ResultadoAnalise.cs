namespace CaixaProjeto.Core.Dominio;

/// <summary>Valores calculados a partir de um pedido válido (Regras 2, 5 e 6).</summary>
public sealed record Indicadores(
    decimal PrestacaoEstimada,
    decimal TaxaEsforco,
    decimal IdadeFinalContrato,
    decimal LimiteMontante);

/// <summary>Um motivo da decisão: que regra o originou e que decisão essa regra pede.</summary>
public sealed record Motivo(int Regra, string Descricao, Decisao Decisao);

/// <summary>
/// O que a aplicação devolve (secção "Resultado Esperado"): decisão final, motivos e indicadores.
/// Os indicadores ficam a null quando o pedido é inválido, porque não são calculáveis.
/// </summary>
public sealed record ResultadoAnalise(
    Decisao Decisao,
    IReadOnlyList<Motivo> Motivos,
    Indicadores? Indicadores);
