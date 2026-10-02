using Microsoft.EntityFrameworkCore;

namespace CaixaProjeto.ApiTests;

public sealed class MigracoesTests : IDisposable
{
    private readonly BaseDadosDeTeste bd = new();

    public void Dispose()
    {
        bd.Dispose();
    }

    [Fact]
    public void NaoHaMudancasNoModeloSemMigracao()
    {
        // Apanha uma entidade alterada sem a migração correspondente
        using var db = bd.NovoContexto();

        Assert.False(db.Database.HasPendingModelChanges(),
            "O modelo mudou e falta uma migração. Crie-a com: dotnet ef migrations add NomeDaMudanca --project CaixaProjeto.ApiService --output-dir Data/Migrations");
    }

    [Fact]
    public void TodasAsMigracoesForamAplicadas_ENaoFaltaNenhuma()
    {
        using var db = bd.NovoContexto();

        Assert.Empty(db.Database.GetPendingMigrations());
        Assert.Contains(db.Database.GetAppliedMigrations(), nome => nome.EndsWith("_Inicial"));
    }

    [Fact]
    public void AsMigracoesCriamTodasAsTabelas()
    {
        using var comando = bd.Conexao.CreateCommand();
        comando.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name";
        using var leitor = comando.ExecuteReader();

        var tabelas = new List<string>();
        while (leitor.Read())
        {
            tabelas.Add(leitor.GetString(0));
        }

        Assert.Contains("Clientes", tabelas);
        Assert.Contains("Pedidos", tabelas);
        Assert.Contains("MotivosPedido", tabelas);
        Assert.Contains("HistoricoEstados", tabelas);
        Assert.Contains("Simulacoes", tabelas);
        Assert.Contains("__EFMigrationsHistory", tabelas);   // o registo das migrações aplicadas
    }
}
