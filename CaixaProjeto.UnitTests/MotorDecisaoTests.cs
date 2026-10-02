using CaixaProjeto.Core;
using CaixaProjeto.Core.Dominio;

namespace CaixaProjeto.UnitTests;

/// <summary>
/// Cada teste parte do cenário A (aprovado) e muda só o que interessa, com "with", para ficar
/// claro que é essa mudança que provoca o resultado.
/// </summary>
public class MotorDecisaoTests
{
    private readonly MotorDecisao motor = new();

    private static readonly PedidoCredito PedidoAprovado = new()
    {
        Nif = "123456789",
        Idade = 35,
        RendimentoMensalLiquido = 2500,
        PrestacoesAtuais = 200,
        ValorPretendido = 10000,
        PrazoMeses = 60,
        SituacaoProfissional = SituacaoProfissional.Efetivo,
        IncidentesCredito = false
    };

    private static int[] Regras(ResultadoAnalise r) => r.Motivos.Select(m => m.Regra).ToArray();

    // ---------- Cenários do enunciado (Tarefa 2) ----------

    [Fact]
    public void CenarioA_Aprovado()
    {
        var r = motor.Analisar(PedidoAprovado);

        Assert.Equal(Decisao.Aprovado, r.Decisao);
        Assert.Empty(r.Motivos);
        Assert.Equal(166.67m, r.Indicadores!.PrestacaoEstimada);
        Assert.Equal(14.67m, r.Indicadores.TaxaEsforco);
    }

    [Fact]
    public void CenarioB_AnaliseManual_PorContratoPrazoETaxaEsforco()
    {
        var r = motor.Analisar(new PedidoCredito
        {
            Nif = "123456789", Idade = 42, RendimentoMensalLiquido = 2000, PrestacoesAtuais = 500,
            ValorPretendido = 20000, PrazoMeses = 48, SituacaoProfissional = SituacaoProfissional.ContratoPrazo
        });

        Assert.Equal(Decisao.AnaliseManual, r.Decisao);
        Assert.Equal([4, 6], Regras(r));
        Assert.Equal(416.67m, r.Indicadores!.PrestacaoEstimada);
        Assert.Equal(45.83m, r.Indicadores.TaxaEsforco);
    }

    [Fact]
    public void CenarioC_Recusado_PorIncidentes()
    {
        var r = motor.Analisar(new PedidoCredito
        {
            Nif = "123456789", Idade = 40, RendimentoMensalLiquido = 3000, PrestacoesAtuais = 300,
            ValorPretendido = 15000, PrazoMeses = 72, SituacaoProfissional = SituacaoProfissional.Efetivo,
            IncidentesCredito = true
        });

        Assert.Equal(Decisao.Recusado, r.Decisao);
        Assert.Equal([3], Regras(r));
        Assert.Equal(16.94m, r.Indicadores!.TaxaEsforco);
    }

    [Fact]
    public void CenarioD_Recusado_PorTaxaEsforco_ComMontanteAcimaDoLimite()
    {
        var r = motor.Analisar(new PedidoCredito
        {
            Nif = "123456789", Idade = 30, RendimentoMensalLiquido = 1200, PrestacoesAtuais = 300,
            ValorPretendido = 25000, PrazoMeses = 36, SituacaoProfissional = SituacaoProfissional.Efetivo
        });

        Assert.Equal(Decisao.Recusado, r.Decisao);
        Assert.Equal([5, 6], Regras(r));
        Assert.Equal(694.44m, r.Indicadores!.PrestacaoEstimada);
        Assert.Equal(82.87m, r.Indicadores.TaxaEsforco);
    }

    // ---------- Regra 1: validação inicial ----------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12345678")]    // 8 dígitos
    [InlineData("1234567890")]  // 10 dígitos
    [InlineData("12345678A")]   // letra
    [InlineData("123 45678")]   // sem o espaço fica com 8 dígitos
    [InlineData("   ")]         // só espaços
    [InlineData("PT123456789")] // letras não são separadores
    public void Regra1_NifSemNoveDigitos_Invalido(string? nif)
    {
        var r = motor.Analisar(PedidoAprovado with { Nif = nif });

        Assert.Equal(Decisao.PedidoInvalido, r.Decisao);
        Assert.Null(r.Indicadores);
    }

    [Theory]
    [InlineData("123 456 789")]     // como se escreve à mão
    [InlineData("123-456-789")]
    [InlineData("123.456.789")]
    [InlineData(" 123456789 ")]
    public void Regra1_NifComEspacosPontosOuHifenes_Valido(string nif)
    {
        Assert.Equal(Decisao.Aprovado, motor.Analisar(PedidoAprovado with { Nif = nif }).Decisao);
    }

    [Fact]
    public void Normalizado_TiraSoOsSeparadores_ENaoMudaOResto()
    {
        var pedido = PedidoAprovado with { Nif = " 123 456-789. " };

        var normalizado = pedido.Normalizado();

        Assert.Equal("123456789", normalizado.Nif);
        Assert.Equal(pedido with { Nif = "123456789" }, normalizado);   // os outros campos ficam iguais
        Assert.Null((PedidoAprovado with { Nif = null }).Normalizado().Nif);
    }

    [Fact]
    public void Regra1_NifComZeroAEsquerda_Valido()
    {
        Assert.Equal(Decisao.Aprovado, motor.Analisar(PedidoAprovado with { Nif = "012345678" }).Decisao);
    }

    [Fact]
    public void Regra1_Idade18_Valido_Idade17_Invalido()
    {
        Assert.Equal(Decisao.Aprovado, motor.Analisar(PedidoAprovado with { Idade = 18 }).Decisao);
        Assert.Equal(Decisao.PedidoInvalido, motor.Analisar(PedidoAprovado with { Idade = 17 }).Decisao);
    }

    [Fact]
    public void Regra1_ValoresAZeroOuNegativos_Invalido()
    {
        Assert.Equal(Decisao.PedidoInvalido, motor.Analisar(PedidoAprovado with { RendimentoMensalLiquido = 0 }).Decisao);
        Assert.Equal(Decisao.PedidoInvalido, motor.Analisar(PedidoAprovado with { ValorPretendido = 0 }).Decisao);
        Assert.Equal(Decisao.PedidoInvalido, motor.Analisar(PedidoAprovado with { PrazoMeses = 0 }).Decisao);
        Assert.Equal(Decisao.PedidoInvalido, motor.Analisar(PedidoAprovado with { RendimentoMensalLiquido = -1 }).Decisao);
    }

    [Fact]
    public void Regra1_CasosNaoPrevistos_PrestacoesNegativasESemSituacao_Invalido()
    {
        Assert.Equal(Decisao.PedidoInvalido, motor.Analisar(PedidoAprovado with { PrestacoesAtuais = -1 }).Decisao);
        Assert.Equal(Decisao.PedidoInvalido, motor.Analisar(PedidoAprovado with { SituacaoProfissional = null }).Decisao);
    }

    [Fact]
    public void Regra1_JuntaTodosOsErros_ENaoAvaliaAsOutrasRegras()
    {
        var r = motor.Analisar(new PedidoCredito { Nif = "1", IncidentesCredito = true });

        Assert.Equal(Decisao.PedidoInvalido, r.Decisao);
        Assert.All(r.Motivos, m => Assert.Equal(1, m.Regra));   // a Regra 3 (incidentes) não aparece
        Assert.Equal(6, r.Motivos.Count);                        // NIF, idade, rendimento, valor, prazo, situação
    }

    // ---------- Regra 2: idade no fim do contrato ----------

    [Fact]
    public void Regra2_IdadeFinalExatamente75_NaoDispara()
    {
        var r = motor.Analisar(PedidoAprovado with { Idade = 70, PrazoMeses = 60 });   // 70 + 5 = 75

        Assert.Equal(Decisao.Aprovado, r.Decisao);
        Assert.Equal(75m, r.Indicadores!.IdadeFinalContrato);
    }

    [Fact]
    public void Regra2_IdadeFinalAcimaDe75_AnaliseManual()
    {
        var r = motor.Analisar(PedidoAprovado with { Idade = 70, PrazoMeses = 61 });   // 75,08

        Assert.Equal(Decisao.AnaliseManual, r.Decisao);
        Assert.Equal([2], Regras(r));
    }

    // ---------- Regras 3 e 4 ----------

    [Theory]
    [InlineData(SituacaoProfissional.Efetivo, Decisao.Aprovado)]
    [InlineData(SituacaoProfissional.ContratoPrazo, Decisao.AnaliseManual)]
    [InlineData(SituacaoProfissional.Desempregado, Decisao.Recusado)]
    public void Regra4_SituacaoProfissional(SituacaoProfissional situacao, Decisao esperada)
    {
        Assert.Equal(esperada, motor.Analisar(PedidoAprovado with { SituacaoProfissional = situacao }).Decisao);
    }

    [Fact]
    public void Regra3_Incidentes_Recusado()
    {
        Assert.Equal(Decisao.Recusado, motor.Analisar(PedidoAprovado with { IncidentesCredito = true }).Decisao);
    }

    // ---------- Regra 5: montante até 20x o rendimento ----------

    [Fact]
    public void Regra5_MontanteIgualA20xRendimento_NaoDispara()
    {
        // 20 x 2000 = 40.000; prazo longo para a taxa de esforço ficar baixa
        var pedido = PedidoAprovado with { RendimentoMensalLiquido = 2000, PrestacoesAtuais = 0, ValorPretendido = 40000, PrazoMeses = 400 };

        Assert.Equal(Decisao.Aprovado, motor.Analisar(pedido).Decisao);
        Assert.Equal(Decisao.AnaliseManual, motor.Analisar(pedido with { ValorPretendido = 40000.01m }).Decisao);
    }

    // ---------- Regra 6: taxa de esforço ----------
    // Rendimento 1000 e prestação nova 100 (10.000 / 100 meses): as prestações atuais afinam a taxa.

    [Theory]
    [InlineData(250.00, Decisao.Aprovado)]        // 35,00% (limite, não dispara)
    [InlineData(250.10, Decisao.AnaliseManual)]   // 35,01%
    [InlineData(400.00, Decisao.AnaliseManual)]   // 50,00% (limite, continua manual)
    [InlineData(400.10, Decisao.Recusado)]        // 50,01%
    public void Regra6_LimitesDaTaxaDeEsforco(double prestacoesAtuais, Decisao esperada)
    {
        var pedido = PedidoAprovado with
        {
            RendimentoMensalLiquido = 1000,
            ValorPretendido = 10000,
            PrazoMeses = 100,
            PrestacoesAtuais = (decimal)prestacoesAtuais
        };

        Assert.Equal(esperada, motor.Analisar(pedido).Decisao);
    }

    // ---------- Regra 7: montantes elevados ----------

    [Fact]
    public void Regra7_Exatamente50000_NaoDispara_Acima_AnaliseManual()
    {
        var pedido = PedidoAprovado with { RendimentoMensalLiquido = 10000, PrestacoesAtuais = 0, ValorPretendido = 50000, PrazoMeses = 100 };

        Assert.Equal(Decisao.Aprovado, motor.Analisar(pedido).Decisao);

        var acima = motor.Analisar(pedido with { ValorPretendido = 50000.01m });
        Assert.Equal(Decisao.AnaliseManual, acima.Decisao);
        Assert.Equal([7], Regras(acima));
    }

    // ---------- Regra 8: prioridade ----------

    [Fact]
    public void Regra8_MontanteElevadoNaoAnulaRecusa()
    {
        var pedido = PedidoAprovado with { RendimentoMensalLiquido = 10000, ValorPretendido = 60000, PrazoMeses = 100, IncidentesCredito = true };

        var r = motor.Analisar(pedido);

        Assert.Equal(Decisao.Recusado, r.Decisao);
        Assert.Equal([3, 7], Regras(r));
    }

    [Fact]
    public void Regra8_AcumulaTodosOsMotivos_EFicaOMaisRestritivo()
    {
        var pedido = PedidoAprovado with
        {
            Idade = 70, PrazoMeses = 120,                                   // Regra 2
            SituacaoProfissional = SituacaoProfissional.ContratoPrazo,       // Regra 4
            RendimentoMensalLiquido = 1000, ValorPretendido = 60000         // Regras 5, 6 e 7
        };

        var r = motor.Analisar(pedido);

        Assert.Equal(Decisao.Recusado, r.Decisao);      // a taxa de esforço (> 50%) manda
        Assert.Equal([2, 4, 5, 6, 7], Regras(r));
    }

    // ---------- Parâmetros ----------

    [Fact]
    public void Parametros_LimitesConfiguraveis()
    {
        var motorExigente = new MotorDecisao(new ParametrosRegras { MontanteElevado = 5000 });

        Assert.Equal(Decisao.AnaliseManual, motorExigente.Analisar(PedidoAprovado).Decisao);   // 10.000 > 5.000
    }
}
