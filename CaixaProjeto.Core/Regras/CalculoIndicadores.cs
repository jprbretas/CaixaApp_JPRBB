using CaixaProjeto.Core.Dominio;

namespace CaixaProjeto.Core.Regras;

/// <summary>
/// Calcula os indicadores de um pedido já validado (a Regra 1 garante que não há divisões por zero).
/// A prestação e a taxa são arredondadas a 2 casas antes de serem comparadas com os limites,
/// para que o valor mostrado ao utilizador seja o mesmo que decide.
/// </summary>
public static class CalculoIndicadores
{
    public static Indicadores Calcular(PedidoCredito pedido, ParametrosRegras parametros)
    {
        // Regra 6 (simplificação do enunciado): prestação nova = montante / prazo, sem juros
        var prestacao = Math.Round(pedido.ValorPretendido / pedido.PrazoMeses, 2);

        // Regra 6: taxa de esforço = (prestações atuais + prestação nova) / rendimento x 100
        var taxaEsforco = Math.Round(
            (pedido.PrestacoesAtuais + prestacao) / pedido.RendimentoMensalLiquido * 100, 2);

        // Regra 2: idade no fim do contrato = idade atual + prazo em anos
        var idadeFinal = Math.Round(pedido.Idade + pedido.PrazoMeses / 12m, 2);

        // Regra 5: montante máximo recomendado = 20 x rendimento
        var limite = pedido.RendimentoMensalLiquido * parametros.MultiploRendimentoMaximo;

        return new Indicadores(prestacao, taxaEsforco, idadeFinal, limite);
    }
}
