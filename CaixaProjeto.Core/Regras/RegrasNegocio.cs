using CaixaProjeto.Core.Dominio;

namespace CaixaProjeto.Core.Regras;

// Nota: as regras estão escritas de propósito na forma "longa" (if / else com return),
// para serem fáceis de ler e comparar com o enunciado.

/// <summary>
/// Uma regra de negócio avaliada sobre um pedido válido.
/// Devolve um motivo quando a regra "dispara", ou null quando não tem nada a dizer.
/// </summary>
public interface IRegra
{
    Motivo? Avaliar(PedidoCredito pedido, Indicadores indicadores, ParametrosRegras parametros);
}

/// <summary>Regra 2: a idade no fim do contrato não pode ultrapassar 75 anos.</summary>
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

/// <summary>Regra 3: incidentes de crédito registados levam a recusa.</summary>
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

/// <summary>Regra 4: efetivo prossegue, contrato a prazo vai a análise manual, desempregado é recusado.</summary>
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
            // Efetivo: prossegue, esta regra não tem objeções
            return null;
        }
    }
}

/// <summary>Regra 5: o montante não pode exceder 20 vezes o rendimento mensal.</summary>
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

/// <summary>Regra 6: até 35% mantém, entre 35% e 50% análise manual, acima de 50% recusa.</summary>
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
            // Até 35%: mantém a decisão
            return null;
        }
    }
}

/// <summary>
/// Regra 7: montantes acima de 50.000 € vão sempre, no mínimo, a análise manual.
/// "Independentemente das restantes regras" é lido como um mínimo: se outra regra recusar,
/// a Regra 8 mantém a recusa, que é mais restritiva.
/// </summary>
public sealed class RegraMontanteElevado : IRegra
{
    // Formata o limite como no enunciado ("50.000"), seja qual for a língua do servidor
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
