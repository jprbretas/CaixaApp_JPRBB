namespace CaixaProjeto.Core.Dominio;

/// <summary>
/// Limites das regras de negócio, fora da lógica, para que a área de risco
/// os possa ajustar por configuração (appsettings) sem alterar código.
/// Os valores por omissão são os do enunciado.
/// </summary>
public sealed record ParametrosRegras
{
    public int IdadeMinima { get; init; } = 18;                        // Regra 1
    public int IdadeMaximaFinalContrato { get; init; } = 75;           // Regra 2
    public decimal MultiploRendimentoMaximo { get; init; } = 20m;      // Regra 5
    public decimal TaxaEsforcoAnaliseManual { get; init; } = 35m;      // Regra 6
    public decimal TaxaEsforcoRecusa { get; init; } = 50m;             // Regra 6
    public decimal MontanteElevado { get; init; } = 50_000m;           // Regra 7
}
