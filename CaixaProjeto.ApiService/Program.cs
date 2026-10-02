using System.Text.Json.Serialization;
using CaixaProjeto.ApiService.Data;
using CaixaProjeto.ApiService.Services;
using CaixaProjeto.Core;
using CaixaProjeto.Core.Contratos;
using CaixaProjeto.Core.Dominio;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();

// Enums em JSON como texto ("Efetivo", "AnaliseManual") em vez de números
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Limites das regras lidos da secção "Regras" do appsettings.json (se faltar, ficam os do enunciado)
var parametros = builder.Configuration.GetSection("Regras").Get<ParametrosRegras>() ?? new ParametrosRegras();
builder.Services.AddSingleton(parametros);
builder.Services.AddSingleton<MotorDecisao>();

builder.Services.AddDbContext<CaixaDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("caixa")));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<PedidoService>();

var app = builder.Build();

// Aplica as migrações em falta: cria a BD no primeiro arranque e depois atualiza-a sem perder dados
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<CaixaDbContext>().Database.Migrate();
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => "API de pré-análise de crédito em execução.");

// Simulação: fica registada para estatística, mas não cria pedido
app.MapPost("/api/pedidos/preanalise", async (PedidoCredito pedido, PedidoService servico, CancellationToken ct) =>
        await servico.SimularAsync(pedido, ct))
    .WithName("PreAnalisarPedido");

app.MapPost("/api/pedidos", async (PedidoCredito pedido, PedidoService servico, CancellationToken ct) =>
        await servico.SubmeterAsync(pedido, ct))
    .WithName("SubmeterPedido");

app.MapGet("/api/pedidos", async (Decisao? estado, int? pagina, int? tamanho, PedidoService servico, CancellationToken ct) =>
        await servico.ListarAsync(estado, pagina ?? 1, tamanho ?? 20, ct))
    .WithName("ListarPedidos");

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
