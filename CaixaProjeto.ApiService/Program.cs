using System.Text.Json.Serialization;
using CaixaProjeto.ApiService.Data;
using CaixaProjeto.ApiService.Services;
using CaixaProjeto.Core;
using CaixaProjeto.Core.Contratos;
using CaixaProjeto.Core.Dominio;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Enums em JSON como texto ("Efetivo", "AnaliseManual") em vez de números
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Limites das regras lidos da secção "Regras" do appsettings.json (se faltar, ficam os do enunciado)
var parametros = builder.Configuration.GetSection("Regras").Get<ParametrosRegras>() ?? new ParametrosRegras();
builder.Services.AddSingleton(parametros);
builder.Services.AddSingleton<MotorDecisao>();

// Base de dados SQLite (ficheiro caixa.db, definido em ConnectionStrings no appsettings.json)
builder.Services.AddDbContext<CaixaDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("caixa")));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<PedidoService>();

var app = builder.Build();

// Aplica as migrações que faltam (pasta Data/Migrations): no primeiro arranque cria a BD e as tabelas;
// depois de uma mudança no modelo, acrescenta só o que mudou, sem apagar os dados.
// A tabela __EFMigrationsHistory, dentro da BD, regista as migrações já aplicadas.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<CaixaDbContext>().Database.Migrate();
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => "API de pré-análise de crédito em execução.");

// Pré-análise (simulação): aplica as regras, regista a simulação e devolve o resultado.
// Não cria pedido; a simulação só conta para estatística.
app.MapPost("/api/pedidos/preanalise", async (PedidoCredito pedido, PedidoService servico, CancellationToken ct) =>
        await servico.SimularAsync(pedido, ct))
    .WithName("PreAnalisarPedido");

// Submissão: analisa e grava o pedido, devolvendo o número atribuído
app.MapPost("/api/pedidos", async (PedidoCredito pedido, PedidoService servico, CancellationToken ct) =>
        await servico.SubmeterAsync(pedido, ct))
    .WithName("SubmeterPedido");

// Lista paginada dos pedidos gravados, com filtro opcional por estado atual.
// Exemplo: GET /api/pedidos?estado=AnaliseManual&pagina=2&tamanho=10
app.MapGet("/api/pedidos", async (Decisao? estado, int? pagina, int? tamanho, PedidoService servico, CancellationToken ct) =>
        await servico.ListarAsync(estado, pagina ?? 1, tamanho ?? 20, ct))
    .WithName("ListarPedidos");

// Detalhe de um pedido: dados, resultado da análise, estado atual e histórico.
// Se o número não existir, responde 404 (Not Found).
app.MapGet("/api/pedidos/{numero}", async (string numero, PedidoService servico, CancellationToken ct) =>
    {
        var detalhe = await servico.ObterAsync(numero, ct);
        if (detalhe is null)
        {
            return Results.NotFound();
        }
        return Results.Ok(detalhe);
    })
    .WithName("ObterPedido");

// Decisão do analista sobre um pedido em análise manual (aprovar ou recusar).
// Responde com o detalhe atualizado, ou com um erro que explica porque não foi possível.
app.MapPost("/api/pedidos/{numero}/decisao", async (string numero, DecisaoAnalista decisao, PedidoService servico, CancellationToken ct) =>
    {
        var resultado = await servico.DecidirAsync(numero, decisao, ct);

        if (resultado == ResultadoDecisao.DadosEmFalta)
        {
            return Results.Problem(statusCode: 400, detail: "Indique o nome do analista e a observação.");
        }
        if (resultado == ResultadoDecisao.NaoEncontrado)
        {
            return Results.Problem(statusCode: 404, detail: $"O pedido {numero} não existe.");
        }
        if (resultado == ResultadoDecisao.NaoAguardaAnalista)
        {
            return Results.Problem(statusCode: 409, detail: "Este pedido não está em análise manual, por isso já não pode ser decidido.");
        }

        return Results.Ok(await servico.ObterAsync(numero, ct));
    })
    .WithName("DecidirPedido");

app.MapDefaultEndpoints();

app.Run();
