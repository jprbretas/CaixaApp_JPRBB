using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CaixaProjeto.Core.Contratos;
using CaixaProjeto.Core.Dominio;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace CaixaProjeto.ApiTests;

/// <summary>A API verdadeira, dentro do teste, com uma base de dados em memória: nunca toca na caixa.db.</summary>
public sealed class ApiDeTeste : WebApplicationFactory<Program>
{
    // Em memória e partilhada por todas as ligações com este nome (único por ApiDeTeste)
    private readonly string ligacao = $"Data Source=api-testes-{Guid.NewGuid()};Mode=Memory;Cache=Shared";

    // A base de dados em memória desaparece quando fecha a última ligação
    private readonly SqliteConnection manterAberta;

    public ApiDeTeste()
    {
        manterAberta = new SqliteConnection(ligacao);
        manterAberta.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Em vez da caixa.db do appsettings.json
        builder.UseSetting("ConnectionStrings:caixa", ligacao);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            manterAberta.Dispose();
        }
    }
}

/// <summary>A API arranca uma vez para a classe toda, por isso cada teste cria os seus pedidos.</summary>
public sealed class EndpointsTests(ApiDeTeste api) : IClassFixture<ApiDeTeste>
{
    private readonly HttpClient cliente = api.CreateClient();

    // Tem de bater certo com a API: nomes em camelCase e enums como texto
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private async Task<string> SubmeterAsync(PedidoCredito pedido)
    {
        var resposta = await cliente.PostAsJsonAsync("/api/pedidos", pedido, Json);
        resposta.EnsureSuccessStatusCode();
        var submetido = await resposta.Content.ReadFromJsonAsync<PedidoSubmetido>(Json);
        return submetido!.Numero;
    }

    private Task<HttpResponseMessage> DecidirAsync(string numero, DecisaoAnalista decisao)
    {
        return cliente.PostAsJsonAsync($"/api/pedidos/{numero}/decisao", decisao, Json);
    }

    [Fact]
    public async Task Preanalise_DevolveOResultado_ComAsDecisoesEmTexto()
    {
        var resposta = await cliente.PostAsJsonAsync("/api/pedidos/preanalise", Exemplos.AnaliseManual, Json);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var texto = await resposta.Content.ReadAsStringAsync();
        Assert.Contains("\"decisao\":\"AnaliseManual\"", texto);   // texto, e não o número 1
    }

    [Fact]
    public async Task Submeter_EDepoisObter_DevolveODetalhe()
    {
        var numero = await SubmeterAsync(Exemplos.AnaliseManual);

        var detalhe = await cliente.GetFromJsonAsync<PedidoDetalhe>($"/api/pedidos/{numero}", Json);

        Assert.Equal(numero, detalhe!.Numero);
        Assert.Equal(Decisao.AnaliseManual, detalhe.EstadoAtual);
        Assert.Equal([4, 6], detalhe.Resultado.Motivos.Select(m => m.Regra));
    }

    [Fact]
    public async Task Obter_PedidoQueNaoExiste_Responde404()
    {
        var resposta = await cliente.GetAsync("/api/pedidos/99999999");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Listar_ComFiltro_SoDevolvePedidosNesseEstado()
    {
        await SubmeterAsync(Exemplos.Aprovado);
        await SubmeterAsync(Exemplos.Recusado);

        var pagina = await cliente.GetFromJsonAsync<Pagina<PedidoResumo>>("/api/pedidos?estado=Recusado", Json);

        Assert.NotEmpty(pagina!.Itens);
        Assert.All(pagina.Itens, p => Assert.Equal(Decisao.Recusado, p.EstadoAtual));
    }

    [Fact]
    public async Task Decidir_PedidoEmAnaliseManual_Responde200_ComODetalheAtualizado()
    {
        var numero = await SubmeterAsync(Exemplos.AnaliseManual);

        var resposta = await DecidirAsync(numero, new DecisaoAnalista(true, "Ana Silva", "Contrato renovado há 3 anos."));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var detalhe = await resposta.Content.ReadFromJsonAsync<PedidoDetalhe>(Json);
        Assert.Equal(Decisao.Aprovado, detalhe!.EstadoAtual);
        Assert.Equal("Contrato renovado há 3 anos.", detalhe.Historico[^1].Observacao);   // acentos intactos
    }

    [Fact]
    public async Task Decidir_SemObservacao_Responde400_ComAMensagem()
    {
        var numero = await SubmeterAsync(Exemplos.AnaliseManual);

        var resposta = await DecidirAsync(numero, new DecisaoAnalista(true, "Ana Silva", ""));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var problema = await resposta.Content.ReadFromJsonAsync<ProblemDetails>(Json);
        Assert.Equal("Indique o nome do analista e a observação.", problema!.Detail);
    }

    [Fact]
    public async Task Decidir_PedidoQueNaoExiste_Responde404()
    {
        var resposta = await DecidirAsync("99999999", new DecisaoAnalista(true, "Ana Silva", "Ok."));

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Decidir_PedidoJaDecidido_Responde409()
    {
        var numero = await SubmeterAsync(Exemplos.Aprovado);   // aprovado pelo motor, não espera analista

        var resposta = await DecidirAsync(numero, new DecisaoAnalista(false, "Ana Silva", "Ok."));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        var problema = await resposta.Content.ReadFromJsonAsync<ProblemDetails>(Json);
        Assert.Contains("não está em análise manual", problema!.Detail);
    }
}
