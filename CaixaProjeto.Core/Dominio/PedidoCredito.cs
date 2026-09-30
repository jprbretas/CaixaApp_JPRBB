namespace CaixaProjeto.Core.Dominio;

/// <summary>
/// Dados de entrada de um pedido de crédito (secção "Dados de Entrada" do enunciado).
/// O NIF é texto para não perder zeros à esquerda e para podermos validar os 9 dígitos.
/// A situação profissional é opcional aqui para que um pedido incompleto
/// chegue ao motor e seja classificado como inválido, em vez de rebentar antes.
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
    /// Cópia do pedido com o NIF limpo: sem espaços, pontos nem hífenes, que é como as pessoas
    /// o costumam escrever ("123 456 789", "123-456-789"). Letras e outros caracteres ficam,
    /// para a Regra 1 os recusar.
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
                continue;   // separador: não entra no NIF
            }
            limpo.Append(caracter);
        }

        return this with { Nif = limpo.ToString() };
    }
}
