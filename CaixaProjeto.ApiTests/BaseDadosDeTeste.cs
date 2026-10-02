using CaixaProjeto.ApiService.Data;
using CaixaProjeto.ApiService.Services;
using CaixaProjeto.Core;
using CaixaProjeto.Core.Dominio;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CaixaProjeto.ApiTests;

/// <summary>Marca a hora que o teste quiser (por exemplo, para criar um pedido com 60 dias).</summary>
public class RelogioDeTeste(DateTimeOffset agora) : TimeProvider
{
    public DateTimeOffset Agora { get; set; } = agora;

    public override DateTimeOffset GetUtcNow() => Agora;
}

/// <summary>
/// SQLite em memória, criada pelas mesmas migrações da caixa.db. Existe enquanto a ligação estiver
/// aberta; o xUnit cria uma por teste, por isso cada teste começa com a base de dados vazia.
/// </summary>
public sealed class BaseDadosDeTeste : IDisposable
{
    public SqliteConnection Conexao { get; }

    // Por omissão, uma data fixa: os números dos pedidos ficam previsíveis (20260001, 20260002...)
    public RelogioDeTeste Relogio { get; } = new(new DateTimeOffset(2026, 9, 30, 10, 0, 0, TimeSpan.Zero));

    public BaseDadosDeTeste()
    {
        Conexao = new SqliteConnection("Data Source=:memory:");
        Conexao.Open();

        using var db = NovoContexto();
        db.Database.Migrate();
    }

    public CaixaDbContext NovoContexto()
    {
        var opcoes = new DbContextOptionsBuilder<CaixaDbContext>().UseSqlite(Conexao).Options;
        return new CaixaDbContext(opcoes);
    }

    /// <summary>
    /// Com um DbContext novo, como a API em cada pedido HTTP. Os testes confirmam o que ficou gravado
    /// com outro (NovoContexto), para não verem o que o EF ainda tem em memória.
    /// </summary>
    public PedidoService Servico()
    {
        return new PedidoService(NovoContexto(), new MotorDecisao(), Relogio);
    }

    public void Dispose()
    {
        Conexao.Dispose();
    }
}

// A partir do cenário A do enunciado (aprovado)
public static class Exemplos
{
    public static readonly PedidoCredito Aprovado = new()
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

    // Cenário B: regras 4 e 6
    public static readonly PedidoCredito AnaliseManual = new()
    {
        Nif = "123456789",
        Idade = 42,
        RendimentoMensalLiquido = 2000,
        PrestacoesAtuais = 500,
        ValorPretendido = 20000,
        PrazoMeses = 48,
        SituacaoProfissional = SituacaoProfissional.ContratoPrazo,
        IncidentesCredito = false
    };

    // Regra 3
    public static readonly PedidoCredito Recusado = Aprovado with { IncidentesCredito = true };

    // Regra 1
    public static readonly PedidoCredito Invalido = Aprovado with { Nif = "12", Idade = 15 };
}
