using System.Text.Json.Serialization;
using CaixaProjeto.ApiService.Data;
using CaixaProjeto.ApiService.Services;
using CaixaProjeto.Core;
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

// Cria a BD e as tabelas no primeiro arranque, se ainda não existirem.
// Para recomeçar do zero, basta apagar o ficheiro caixa.db.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<CaixaDbContext>().Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => "API de pré-análise de crédito em execução.");

// Pré-análise: aplica as regras e devolve o resultado, sem gravar nada
app.MapPost("/api/pedidos/preanalise", (PedidoCredito pedido, MotorDecisao motor) => motor.Analisar(pedido))
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

app.MapDefaultEndpoints();

app.Run();
