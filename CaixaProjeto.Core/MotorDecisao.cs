using CaixaProjeto.Core.Dominio;
using CaixaProjeto.Core.Regras;

namespace CaixaProjeto.Core;

public sealed class MotorDecisao(ParametrosRegras parametros)
{
    // Todas são avaliadas, mesmo depois de uma recusa, para o resultado ter todos os motivos
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
        pedido = pedido.Normalizado();

        // Pedido inválido: pára aqui, os indicadores nem são calculáveis
        var erros = ValidacaoInicial.Validar(pedido, parametros);
        if (erros.Count > 0)
            return new ResultadoAnalise(Decisao.PedidoInvalido, erros, null);

        var indicadores = CalculoIndicadores.Calcular(pedido, parametros);

        var motivos = Regras
            .Select(regra => regra.Avaliar(pedido, indicadores, parametros))
            .OfType<Motivo>()
            .ToList();

        // Regra 8: fica a mais restritiva; sem motivos, aprovado
        var decisao = motivos.Count == 0 ? Decisao.Aprovado : motivos.Max(m => m.Decisao);

        return new ResultadoAnalise(decisao, motivos, indicadores);
    }
}
