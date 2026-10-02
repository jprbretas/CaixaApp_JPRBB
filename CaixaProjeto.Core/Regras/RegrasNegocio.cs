using CaixaProjeto.Core.Dominio;

namespace CaixaProjeto.Core.Regras;

/// <summary>Devolve um motivo quando a regra dispara, ou null.</summary>
public interface IRegra
{
    Motivo? Avaliar(PedidoCredito pedido, Indicadores indicadores, ParametrosRegras parametros);
}

public sealed class RegraIdadeFinal : IRegra
{
    public Motivo? Avaliar(PedidoCredito pedido, Indicadores indicadores, ParametrosRegras parametros)
    {
        if (indicadores.IdadeFinalContrato > parametros.IdadeMaximaFinalContrato)
        {
            return new Motivo(2, $"Idade no fim do contrato superior a {parametros.IdadeMaximaFinalContrato} anos", Decisao.AnaliseManual);
        }
        else
        {
            return null;
        }
    }
}

public sealed class RegraIncidentes : IRegra
{
    public Motivo? Avaliar(PedidoCredito pedido, Indicadores indicadores, ParametrosRegras parametros)
    {
        if (pedido.IncidentesCredito)
        {
            return new Motivo(3, "Cliente com incidentes de crédito registados", Decisao.Recusado);
        }
        else
        {
            return null;
        }
    }
}

public sealed class RegraSituacaoProfissional : IRegra
{
    public Motivo? Avaliar(PedidoCredito pedido, Indicadores indicadores, ParametrosRegras parametros)
    {
        if (pedido.SituacaoProfissional == SituacaoProfissional.ContratoPrazo)
        {
            return new Motivo(4, "Cliente com contrato a prazo", Decisao.AnaliseManual);
        }
        else if (pedido.SituacaoProfissional == SituacaoProfissional.Desempregado)
        {
            return new Motivo(4, "Cliente desempregado", Decisao.Recusado);
        }
        else
        {
            return null;
        }
    }
}

public sealed class RegraLimiteMontante : IRegra
{
    public Motivo? Avaliar(PedidoCredito pedido, Indicadores indicadores, ParametrosRegras parametros)
    {
        if (pedido.ValorPretendido > indicadores.LimiteMontante)
        {
            return new Motivo(5, $"Montante superior a {parametros.MultiploRendimentoMaximo:0}x o rendimento mensal", Decisao.AnaliseManual);
        }
        else
        {
            return null;
        }
    }
}

public sealed class RegraTaxaEsforco : IRegra
{
    public Motivo? Avaliar(PedidoCredito pedido, Indicadores indicadores, ParametrosRegras parametros)
    {
        decimal taxa = indicadores.TaxaEsforco;

        if (taxa > parametros.TaxaEsforcoRecusa)
        {
            return new Motivo(6, $"Taxa de esforço superior a {parametros.TaxaEsforcoRecusa:0}%", Decisao.Recusado);
        }
        else if (taxa > parametros.TaxaEsforcoAnaliseManual)
        {
            return new Motivo(6, $"Taxa de esforço entre {parametros.TaxaEsforcoAnaliseManual:0}% e {parametros.TaxaEsforcoRecusa:0}%", Decisao.AnaliseManual);
        }
        else
        {
            return null;
        }
    }
}

/// <summary>
/// "Independentemente das restantes regras" é lido como um mínimo: se outra regra recusar,
/// a recusa mantém-se (Regra 8).
/// </summary>
public sealed class RegraMontanteElevado : IRegra
{
    // "50.000" como no enunciado, seja qual for a língua do servidor
    private static readonly System.Globalization.NumberFormatInfo PtPt = new()
    {
        NumberGroupSeparator = ".",
        NumberDecimalSeparator = ","
    };

    public Motivo? Avaliar(PedidoCredito pedido, Indicadores indicadores, ParametrosRegras parametros)
    {
        if (pedido.ValorPretendido > parametros.MontanteElevado)
        {
            string limite = parametros.MontanteElevado.ToString("N0", PtPt);
            return new Motivo(7, $"Montante superior a {limite} €", Decisao.AnaliseManual);
        }
        else
        {
            return null;
        }
    }
}
