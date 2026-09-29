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
}
