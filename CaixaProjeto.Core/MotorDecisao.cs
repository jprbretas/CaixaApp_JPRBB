using CaixaProjeto.Core.Dominio;
using CaixaProjeto.Core.Regras;

namespace CaixaProjeto.Core;

/// <summary>
/// Ponto de entrada da pré-análise: recebe um pedido e devolve decisão, motivos e indicadores.
/// </summary>
public sealed class MotorDecisao(ParametrosRegras parametros)
{
    // Regras 2 a 7, pela ordem do enunciado. Todas são avaliadas, para registar todos os motivos.
    private static readonly IReadOnlyList<IRegra> Regras =
    [
        new RegraIdadeFinal(),
        new RegraIncidentes(),
        new RegraSituacaoProfissional(),
        new RegraLimiteMontante(),
        new RegraTaxaEsforco(),
        new RegraMontanteElevado()
    ];

    public MotorDecisao() : this(new ParametrosRegras()) { }

    public ResultadoAnalise Analisar(PedidoCredito pedido)
    {
        // 1. Regra 1: se o pedido é inválido, pára aqui (os indicadores nem são calculáveis)
        var erros = ValidacaoInicial.Validar(pedido, parametros);
        if (erros.Count > 0)
            return new ResultadoAnalise(Decisao.PedidoInvalido, erros, null);

        // 2. Indicadores
        var indicadores = CalculoIndicadores.Calcular(pedido, parametros);

        // 3. Regras 2 a 7
        var motivos = Regras
            .Select(regra => regra.Avaliar(pedido, indicadores, parametros))
            .OfType<Motivo>()
            .ToList();

        // 4. Regra 8: fica a decisão mais restritiva; sem motivos, o pedido é aprovado
        var decisao = motivos.Count == 0 ? Decisao.Aprovado : motivos.Max(m => m.Decisao);

        return new ResultadoAnalise(decisao, motivos, indicadores);
    }
}
