using CaixaProjeto.Core.Contratos;
using CaixaProjeto.Core.Dominio;
using Microsoft.Data.Sqlite;

namespace CaixaProjeto.ApiTests;

/// <summary>Corre o sql/Tarefa4_Consultas.sql tal como está, sobre dados preparados.</summary>
public sealed class ConsultasTarefa4Tests : IDisposable
{
    private readonly BaseDadosDeTeste bd = new();

    public ConsultasTarefa4Tests()
    {
        // A consulta 4 compara com a hora atual ("último mês"), por isso aqui o relógio é o verdadeiro
        bd.Relogio.Agora = DateTimeOffset.UtcNow;
    }

    public void Dispose()
    {
        bd.Dispose();
    }

    // ---------- Ler e correr o ficheiro ----------

    /// <summary>Tira as linhas de comentário (que também podem ter ";") e separa pelos ";".</summary>
    private static List<string> ConsultasDoFicheiro()
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "sql", "Tarefa4_Consultas.sql");
        var linhas = File.ReadAllLines(caminho)
            .Where(linha => !linha.TrimStart().StartsWith("--"));

        return string.Join("\n", linhas)
            .Split(';')
            .Select(consulta => consulta.Trim())
            .Where(consulta => consulta.Length > 0)
            .ToList();
    }

    private List<object[]> Correr(int numero)
    {
        var consultas = ConsultasDoFicheiro();
        Assert.Equal(5, consultas.Count);   // se isto falhar, o ficheiro mudou de forma

        using var comando = bd.Conexao.CreateCommand();
        comando.CommandText = consultas[numero - 1];
        using var leitor = comando.ExecuteReader();

        var linhas = new List<object[]>();
        while (leitor.Read())
        {
            var colunas = new object[leitor.FieldCount];
            leitor.GetValues(colunas);
            linhas.Add(colunas);
        }
        return linhas;
    }

    // ---------- Dados preparados ----------

    private async Task<string> Submeter(PedidoCredito pedido, string nif)
    {
        var submetido = await bd.Servico().SubmeterAsync(pedido with { Nif = nif });
        return submetido.Numero;
    }

    private async Task Decidir(string numero, bool aprovar)
    {
        await bd.Servico().DecidirAsync(numero, new DecisaoAnalista(aprovar, "Ana Silva", "Decisão de teste."));
    }

    /// <summary>
    ///   111111111  1 aprovado + 1 análise manual aprovada pelo analista
    ///   222222222  2 recusados pela Regra 3 (incidentes)
    ///   333333333  1 recusado pela Regra 4 (desempregado) + 1 pela Regra 6 (taxa de esforço)
    ///   444444444  1 análise manual recusada pelo analista + 1 análise manual com 60 dias, por decidir
    ///   555555555  2 aprovados com 60 dias
    ///   666666666  2 simulações
    ///   777777777  1 simulação recente + 1 com 60 dias
    ///   NIF "12"   1 pedido inválido
    /// </summary>
    private async Task PrepararDados()
    {
        var agora = bd.Relogio.Agora;
        var desempregado = Exemplos.Aprovado with { SituacaoProfissional = SituacaoProfissional.Desempregado };
        var taxaAcimaDe50 = Exemplos.Aprovado with { RendimentoMensalLiquido = 1000, PrestacoesAtuais = 500 };

        await Submeter(Exemplos.Aprovado, "111111111");
        await Decidir(await Submeter(Exemplos.AnaliseManual, "111111111"), aprovar: true);
        await Submeter(Exemplos.Recusado, "222222222");
        await Submeter(Exemplos.Recusado, "222222222");
        await Submeter(desempregado, "333333333");
        await Submeter(taxaAcimaDe50, "333333333");
        await Decidir(await Submeter(Exemplos.AnaliseManual, "444444444"), aprovar: false);
        await Submeter(Exemplos.Invalido, "12");
        await bd.Servico().SimularAsync(Exemplos.Aprovado with { Nif = "666666666" });
        await bd.Servico().SimularAsync(Exemplos.Aprovado with { Nif = "666666666" });
        await bd.Servico().SimularAsync(Exemplos.Aprovado with { Nif = "777777777" });

        // Registos com 60 dias: fora do "último mês"
        bd.Relogio.Agora = agora.AddDays(-60);
        await Submeter(Exemplos.AnaliseManual, "444444444");
        await Submeter(Exemplos.Aprovado, "555555555");
        await Submeter(Exemplos.Aprovado, "555555555");
        await bd.Servico().SimularAsync(Exemplos.Aprovado with { Nif = "777777777" });
        bd.Relogio.Agora = agora;
    }

    // ---------- As cinco consultas ----------

    [Fact]
    public async Task Consulta1_ContaOsAprovados_PeloMotorEPeloAnalista()
    {
        await PrepararDados();

        var linha = Assert.Single(Correr(1));

        // 111 (motor), 111 (analista) e os dois do 555
        Assert.Equal(4L, linha[0]);
    }

    [Fact]
    public async Task Consulta2_ContaCadaUmDosOutrosEstados()
    {
        await PrepararDados();

        var porEstado = Correr(2).ToDictionary(l => (string)l[0], l => (long)l[1]);

        Assert.Equal(5L, porEstado["Recusado"]);        // 2 do 222, 2 do 333, 1 do 444 (analista)
        Assert.Equal(1L, porEstado["AnaliseManual"]);   // o do 444 com 60 dias
        Assert.Equal(1L, porEstado["PedidoInvalido"]);
    }

    [Fact]
    public void Consulta2_SemPedidos_MostraOsTresEstadosComZero()
    {
        var porEstado = Correr(2).ToDictionary(l => (string)l[0], l => (long)l[1]);

        Assert.Equal(3, porEstado.Count);
        Assert.All(porEstado.Values, total => Assert.Equal(0L, total));
    }

    [Fact]
    public async Task Consulta3_MotivoDeRecusaMaisFrequente()
    {
        await PrepararDados();

        var linha = Assert.Single(Correr(3));

        Assert.Equal(3L, linha[0]);                                          // Regra 3
        Assert.Equal("Cliente com incidentes de crédito registados", linha[1]);
        Assert.Equal(2L, linha[2]);
    }

    [Fact]
    public async Task Consulta3_EmCasoDeEmpate_MostraTodosOsMotivosEmpatados()
    {
        await Submeter(Exemplos.Recusado, "222222222");
        await Submeter(Exemplos.Aprovado with { SituacaoProfissional = SituacaoProfissional.Desempregado }, "333333333");

        var regras = Correr(3).Select(l => (long)l[0]).Order();

        Assert.Equal([3L, 4L], regras);
    }

    [Fact]
    public async Task Consulta4_ClientesComMaisDeUmPedidoOuSimulacaoNoUltimoMes()
    {
        await PrepararDados();

        var linhas = Correr(4);
        var nifs = linhas.Select(l => (string)l[0]).Order();

        // Ficam de fora: 444 (um dos pedidos tem 60 dias), 555 (os dois têm 60 dias),
        // 777 (uma das simulações tem 60 dias) e o NIF "12" (inválido, sem cliente)
        Assert.Equal(["111111111", "222222222", "333333333", "666666666"], nifs);

        // O 666 só tem simulações: 2 no total, 0 pedidos, 2 simulações
        var cliente666 = linhas.Single(l => (string)l[0] == "666666666");
        Assert.Equal(2L, cliente666[1]);
        Assert.Equal(0L, cliente666[2]);
        Assert.Equal(2L, cliente666[3]);
    }

    [Fact]
    public async Task Consulta5_PedidosQuePassaramDeAnaliseManualAAprovado()
    {
        await PrepararDados();

        var linha = Assert.Single(Correr(5));

        // Só o do 111. O do 444 foi recusado, e o de 60 dias ainda está por decidir.
        Assert.Equal(1L, linha[0]);
    }
}
