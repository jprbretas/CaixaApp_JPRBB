using CaixaProjeto.ApiService.Services;
using CaixaProjeto.Core.Contratos;
using CaixaProjeto.Core.Dominio;
using Microsoft.EntityFrameworkCore;

namespace CaixaProjeto.ApiTests;

/// <summary>
/// Testes do PedidoService com uma base de dados SQLite verdadeira (em memória):
/// o que fica gravado ao submeter e ao simular, a lista, o detalhe e a decisão do analista.
/// </summary>
public sealed class PedidoServiceTests : IDisposable
{
    private readonly BaseDadosDeTeste bd = new();

    public void Dispose()
    {
        bd.Dispose();
    }

    // ---------- Submeter ----------

    [Fact]
    public async Task Submeter_GravaPedidoMotivosHistoricoECliente()
    {
        var submetido = await bd.Servico().SubmeterAsync(Exemplos.AnaliseManual);

        Assert.Equal("20260001", submetido.Numero);
        Assert.Equal(Decisao.AnaliseManual, submetido.Resultado.Decisao);

        using var db = bd.NovoContexto();
        var pedido = await db.Pedidos
            .Include(p => p.Motivos)
            .Include(p => p.Historico)
            .Include(p => p.Cliente)
            .SingleAsync();

        Assert.Equal(Decisao.AnaliseManual, pedido.DecisaoAutomatica);
        Assert.Equal(Decisao.AnaliseManual, pedido.EstadoAtual);
        Assert.Equal("123456789", pedido.Cliente!.Nif);

        // Indicadores gravados, incluindo o limite da Regra 5 (20 x 2000)
        Assert.Equal(416.67m, pedido.PrestacaoEstimada);
        Assert.Equal(45.83m, pedido.TaxaEsforco);
        Assert.Equal(40000m, pedido.LimiteMontante);

        // Um motivo por regra que disparou: contrato a prazo (4) e taxa de esforço (6)
        Assert.Equal([4, 6], pedido.Motivos.OrderBy(m => m.Id).Select(m => m.Regra));

        // A decisão automática é a primeira linha do histórico
        var linha = Assert.Single(pedido.Historico);
        Assert.Null(linha.EstadoAnterior);
        Assert.Equal(Decisao.AnaliseManual, linha.EstadoNovo);
        Assert.Equal(PedidoService.UtilizadorSistema, linha.Utilizador);
    }

    [Fact]
    public async Task Submeter_NumerosSeguidos_ERecomecamNoAnoSeguinte()
    {
        var primeiro = await bd.Servico().SubmeterAsync(Exemplos.Aprovado);
        var segundo = await bd.Servico().SubmeterAsync(Exemplos.Aprovado);

        bd.Relogio.Agora = new DateTimeOffset(2027, 1, 1, 9, 0, 0, TimeSpan.Zero);
        var anoNovo = await bd.Servico().SubmeterAsync(Exemplos.Aprovado);

        Assert.Equal("20260001", primeiro.Numero);
        Assert.Equal("20260002", segundo.Numero);
        Assert.Equal("20270001", anoNovo.Numero);
    }

    [Fact]
    public async Task Submeter_MesmoNif_ReaproveitaOCliente()
    {
        await bd.Servico().SubmeterAsync(Exemplos.Aprovado);
        await bd.Servico().SubmeterAsync(Exemplos.Recusado);   // mesmo NIF

        using var db = bd.NovoContexto();
        Assert.Equal(1, await db.Clientes.CountAsync());
        Assert.Equal(2, await db.Pedidos.CountAsync());
    }

    [Fact]
    public async Task Submeter_NifComEspacos_GravaONifLimpo_EOMesmoCliente()
    {
        await bd.Servico().SubmeterAsync(Exemplos.Aprovado with { Nif = "123 456 789" });
        await bd.Servico().SimularAsync(Exemplos.Aprovado with { Nif = "123-456-789" });
        await bd.Servico().SubmeterAsync(Exemplos.Aprovado with { Nif = "123456789" });

        using var db = bd.NovoContexto();
        var cliente = await db.Clientes.SingleAsync();                // um só cliente para as três formas
        Assert.Equal("123456789", cliente.Nif);
        Assert.All(await db.Pedidos.ToListAsync(), p => Assert.Equal("123456789", p.Nif));
        Assert.Equal("123456789", (await db.Simulacoes.SingleAsync()).Nif);
    }

    [Fact]
    public async Task Submeter_PedidoInvalido_GravaSemClienteESemIndicadores()
    {
        var submetido = await bd.Servico().SubmeterAsync(Exemplos.Invalido);

        Assert.Equal(Decisao.PedidoInvalido, submetido.Resultado.Decisao);

        using var db = bd.NovoContexto();
        var pedido = await db.Pedidos.SingleAsync();
        Assert.Null(pedido.ClienteId);
        Assert.Null(pedido.TaxaEsforco);
        Assert.Null(pedido.LimiteMontante);
        Assert.Equal(0, await db.Clientes.CountAsync());
    }

    // ---------- Simular ----------

    [Fact]
    public async Task Simular_GravaSimulacao_ENaoCriaPedido()
    {
        var resultado = await bd.Servico().SimularAsync(Exemplos.AnaliseManual);

        Assert.Equal(Decisao.AnaliseManual, resultado.Decisao);

        using var db = bd.NovoContexto();
        var simulacao = await db.Simulacoes.SingleAsync();
        Assert.Equal(Decisao.AnaliseManual, simulacao.Decisao);
        Assert.Equal(20000m, simulacao.ValorPretendido);
        Assert.Equal(0, await db.Pedidos.CountAsync());
    }

    [Fact]
    public async Task Simular_EDepoisSubmeter_UsamOMesmoCliente()
    {
        await bd.Servico().SimularAsync(Exemplos.Aprovado);
        await bd.Servico().SubmeterAsync(Exemplos.Aprovado);

        using var db = bd.NovoContexto();
        var cliente = await db.Clientes.SingleAsync();
        Assert.Equal(cliente.Id, (await db.Simulacoes.SingleAsync()).ClienteId);
        Assert.Equal(cliente.Id, (await db.Pedidos.SingleAsync()).ClienteId);
    }

    [Fact]
    public async Task Simular_NifInvalido_GravaSemCliente()
    {
        await bd.Servico().SimularAsync(Exemplos.Invalido);

        using var db = bd.NovoContexto();
        Assert.Null((await db.Simulacoes.SingleAsync()).ClienteId);
        Assert.Equal(0, await db.Clientes.CountAsync());
    }

    // ---------- Listar ----------

    [Fact]
    public async Task Listar_DoMaisRecenteParaOMaisAntigo_ComFiltroEPaginas()
    {
        await bd.Servico().SubmeterAsync(Exemplos.Aprovado);        // 20260001
        bd.Relogio.Agora = bd.Relogio.Agora.AddMinutes(1);
        await bd.Servico().SubmeterAsync(Exemplos.AnaliseManual);   // 20260002
        bd.Relogio.Agora = bd.Relogio.Agora.AddMinutes(1);
        await bd.Servico().SubmeterAsync(Exemplos.Recusado);        // 20260003

        var todos = await bd.Servico().ListarAsync(null, 1, 20);
        Assert.Equal(["20260003", "20260002", "20260001"], todos.Itens.Select(p => p.Numero));

        var manuais = await bd.Servico().ListarAsync(Decisao.AnaliseManual, 1, 20);
        var manual = Assert.Single(manuais.Itens);
        Assert.Equal("20260002", manual.Numero);

        var pagina1 = await bd.Servico().ListarAsync(null, 1, 2);
        var pagina2 = await bd.Servico().ListarAsync(null, 2, 2);
        Assert.Equal(2, pagina1.Itens.Count);
        Assert.Equal("20260001", Assert.Single(pagina2.Itens).Numero);
        Assert.Equal(3, pagina2.Total);
        Assert.Equal(2, pagina2.TotalPaginas);
    }

    [Theory]
    [InlineData(0, 20, 1, 20)]      // página 0 passa a 1
    [InlineData(-5, 20, 1, 20)]
    [InlineData(1, 0, 1, 20)]       // tamanho 0 passa ao tamanho por omissão
    [InlineData(1, 500, 1, 20)]     // acima de 100 também
    [InlineData(2, 100, 2, 100)]    // valores com sentido ficam como estão
    public async Task Listar_ValoresSemSentido_UsamOsValoresPorOmissao(int pagina, int tamanho, int paginaEsperada, int tamanhoEsperado)
    {
        var resultado = await bd.Servico().ListarAsync(null, pagina, tamanho);

        Assert.Equal(paginaEsperada, resultado.NumeroPagina);
        Assert.Equal(tamanhoEsperado, resultado.TamanhoPagina);
    }

    // ---------- Obter (detalhe) ----------

    [Fact]
    public async Task Obter_PedidoQueNaoExiste_DevolveNull()
    {
        Assert.Null(await bd.Servico().ObterAsync("99999999"));
    }

    [Fact]
    public async Task Obter_DevolveDadosAnaliseEHistorico_TalComoForamGravados()
    {
        var submetido = await bd.Servico().SubmeterAsync(Exemplos.AnaliseManual);

        var detalhe = await bd.Servico().ObterAsync(submetido.Numero);

        Assert.NotNull(detalhe);
        Assert.Equal(Exemplos.AnaliseManual, detalhe.Dados);   // records comparam campo a campo
        Assert.Equal(Decisao.AnaliseManual, detalhe.Resultado.Decisao);
        Assert.Equal([4, 6], detalhe.Resultado.Motivos.Select(m => m.Regra));
        Assert.Equal(40000m, detalhe.Resultado.Indicadores!.LimiteMontante);
        Assert.Single(detalhe.Historico);
        Assert.True(detalhe.AguardaAnalista);
    }

    [Fact]
    public async Task Obter_PedidoInvalido_NaoTemIndicadores()
    {
        var submetido = await bd.Servico().SubmeterAsync(Exemplos.Invalido);

        var detalhe = await bd.Servico().ObterAsync(submetido.Numero);

        Assert.Null(detalhe!.Resultado.Indicadores);
        Assert.All(detalhe.Resultado.Motivos, m => Assert.Equal(1, m.Regra));
        Assert.False(detalhe.AguardaAnalista);
    }

    // ---------- Decisão do analista ----------

    [Theory]
    [InlineData(true, Decisao.Aprovado)]
    [InlineData(false, Decisao.Recusado)]
    public async Task Decidir_MudaOEstado_AcrescentaHistorico_ENaoMudaADecisaoAutomatica(bool aprovar, Decisao esperado)
    {
        var submetido = await bd.Servico().SubmeterAsync(Exemplos.AnaliseManual);
        bd.Relogio.Agora = bd.Relogio.Agora.AddHours(2);

        var resultado = await bd.Servico().DecidirAsync(submetido.Numero, new DecisaoAnalista(aprovar, "  Ana Silva ", " Rendimento estável. "));

        Assert.Equal(ResultadoDecisao.Decidido, resultado);

        var detalhe = await bd.Servico().ObterAsync(submetido.Numero);
        Assert.Equal(esperado, detalhe!.EstadoAtual);
        Assert.Equal(Decisao.AnaliseManual, detalhe.Resultado.Decisao);   // a do motor não muda
        Assert.False(detalhe.AguardaAnalista);

        Assert.Equal(2, detalhe.Historico.Count);
        var decisao = detalhe.Historico[1];
        Assert.Equal(Decisao.AnaliseManual, decisao.EstadoAnterior);
        Assert.Equal(esperado, decisao.EstadoNovo);
        Assert.Equal("Ana Silva", decisao.Utilizador);                    // sem os espaços à volta
        Assert.Equal("Rendimento estável.", decisao.Observacao);
        Assert.Equal(bd.Relogio.Agora.UtcDateTime, decisao.Data);
    }

    [Theory]
    [InlineData("", "Rendimento estável.")]
    [InlineData("   ", "Rendimento estável.")]
    [InlineData("Ana Silva", "")]
    [InlineData("Ana Silva", "   ")]
    public async Task Decidir_SemNomeOuSemObservacao_DadosEmFalta_ENaoMudaNada(string utilizador, string observacao)
    {
        var submetido = await bd.Servico().SubmeterAsync(Exemplos.AnaliseManual);

        var resultado = await bd.Servico().DecidirAsync(submetido.Numero, new DecisaoAnalista(true, utilizador, observacao));

        Assert.Equal(ResultadoDecisao.DadosEmFalta, resultado);
        var detalhe = await bd.Servico().ObterAsync(submetido.Numero);
        Assert.Equal(Decisao.AnaliseManual, detalhe!.EstadoAtual);
        Assert.Single(detalhe.Historico);
    }

    [Fact]
    public async Task Decidir_PedidoQueNaoExiste_NaoEncontrado()
    {
        var resultado = await bd.Servico().DecidirAsync("99999999", new DecisaoAnalista(true, "Ana Silva", "Ok."));

        Assert.Equal(ResultadoDecisao.NaoEncontrado, resultado);
    }

    [Fact]
    public async Task Decidir_PedidoQueNaoEstaEmAnaliseManual_NaoAguardaAnalista()
    {
        var submetido = await bd.Servico().SubmeterAsync(Exemplos.Aprovado);

        var resultado = await bd.Servico().DecidirAsync(submetido.Numero, new DecisaoAnalista(false, "Ana Silva", "Ok."));

        Assert.Equal(ResultadoDecisao.NaoAguardaAnalista, resultado);
    }

    [Fact]
    public async Task Decidir_DuasVezes_ASegundaJaNaoEAceite()
    {
        var submetido = await bd.Servico().SubmeterAsync(Exemplos.AnaliseManual);

        var primeira = await bd.Servico().DecidirAsync(submetido.Numero, new DecisaoAnalista(true, "Ana Silva", "Ok."));
        var segunda = await bd.Servico().DecidirAsync(submetido.Numero, new DecisaoAnalista(false, "Rui Costa", "Não."));

        Assert.Equal(ResultadoDecisao.Decidido, primeira);
        Assert.Equal(ResultadoDecisao.NaoAguardaAnalista, segunda);
        var detalhe = await bd.Servico().ObterAsync(submetido.Numero);
        Assert.Equal(Decisao.Aprovado, detalhe!.EstadoAtual);
    }
}
