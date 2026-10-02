namespace CaixaProjeto.Core.Dominio;

public enum SituacaoProfissional
{
    Efetivo,
    ContratoPrazo,
    Desempregado
}

/// <summary>O valor é a severidade (Regra 8): a decisão final é o máximo das decisões das regras.</summary>
public enum Decisao
{
    Aprovado = 0,
    AnaliseManual = 1,
    Recusado = 2,
    PedidoInvalido = 3
}

public static class TextosDominio
{
    public static string Texto(this Decisao decisao) => decisao switch
    {
        Decisao.Aprovado => "APROVADO",
        Decisao.AnaliseManual => "ANÁLISE MANUAL",
        Decisao.Recusado => "RECUSADO",
        Decisao.PedidoInvalido => "PEDIDO INVÁLIDO",
        _ => decisao.ToString()
    };

    public static string Texto(this SituacaoProfissional situacao) => situacao switch
    {
        SituacaoProfissional.Efetivo => "Efetivo",
        SituacaoProfissional.ContratoPrazo => "Contrato a prazo",
        SituacaoProfissional.Desempregado => "Desempregado",
        _ => situacao.ToString()
    };
}
