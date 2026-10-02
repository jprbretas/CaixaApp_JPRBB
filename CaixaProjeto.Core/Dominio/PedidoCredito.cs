namespace CaixaProjeto.Core.Dominio;

/// <summary>
/// O NIF é texto para não perder zeros à esquerda. A situação profissional é opcional para um
/// pedido incompleto chegar ao motor e ser classificado como inválido.
/// </summary>
public sealed record PedidoCredito
{
    public string? Nif { get; init; }
    public int Idade { get; init; }
    public decimal RendimentoMensalLiquido { get; init; }
    public decimal PrestacoesAtuais { get; init; }
    public decimal ValorPretendido { get; init; }
    public int PrazoMeses { get; init; }
    public SituacaoProfissional? SituacaoProfissional { get; init; }
    public bool IncidentesCredito { get; init; }

    /// <summary>
    /// Cópia com o NIF sem espaços, pontos nem hífenes ("123 456 789"). Letras ficam, para a
    /// Regra 1 as recusar.
    /// </summary>
    public PedidoCredito Normalizado()
    {
        if (Nif is null)
        {
            return this;
        }

        var limpo = new System.Text.StringBuilder();
        foreach (var caracter in Nif)
        {
            if (char.IsWhiteSpace(caracter) || caracter == '-' || caracter == '.')
            {
                continue;
            }
            limpo.Append(caracter);
        }

        return this with { Nif = limpo.ToString() };
    }
}
