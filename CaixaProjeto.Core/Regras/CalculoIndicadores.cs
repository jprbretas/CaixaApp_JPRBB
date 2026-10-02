using CaixaProjeto.Core.Dominio;

namespace CaixaProjeto.Core.Regras;

/// <summary>
/// Só para pedidos válidos: a Regra 1 garante que não há divisões por zero. Os valores são
/// arredondados a 2 casas antes de comparar, para o valor mostrado ser o que decide.
/// </summary>
public static class CalculoIndicadores
{
    public static Indicadores Calcular(PedidoCredito pedido, ParametrosRegras parametros)
    {
        // Sem juros: simplificação pedida no enunciado
        var prestacao = Math.Round(pedido.ValorPretendido / pedido.PrazoMeses, 2);

        var taxaEsforco = Math.Round(
            (pedido.PrestacoesAtuais + prestacao) / pedido.RendimentoMensalLiquido * 100, 2);

        var idadeFinal = Math.Round(pedido.Idade + pedido.PrazoMeses / 12m, 2);

        var limite = pedido.RendimentoMensalLiquido * parametros.MultiploRendimentoMaximo;

        return new Indicadores(prestacao, taxaEsforco, idadeFinal, limite);
    }
}
