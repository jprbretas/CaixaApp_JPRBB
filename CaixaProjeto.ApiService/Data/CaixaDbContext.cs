using Microsoft.EntityFrameworkCore;

namespace CaixaProjeto.ApiService.Data;

public class CaixaDbContext(DbContextOptions<CaixaDbContext> options) : DbContext(options)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<MotivoPedido> MotivosPedido => Set<MotivoPedido>();
    public DbSet<HistoricoEstado> HistoricoEstados => Set<HistoricoEstado>();
    public DbSet<Simulacao> Simulacoes => Set<Simulacao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cliente>(e =>
        {
            e.HasIndex(c => c.Nif).IsUnique();
            e.Property(c => c.Nif).HasMaxLength(9);
        });

        modelBuilder.Entity<Pedido>(e =>
        {
            e.HasIndex(p => p.Numero).IsUnique();
            e.HasIndex(p => p.DataSubmissao);
            e.Property(p => p.Numero).HasMaxLength(20);

            // Enums gravados como texto ("AnaliseManual"), para as queries SQL serem legíveis
            e.Property(p => p.SituacaoProfissional).HasConversion<string>();
            e.Property(p => p.DecisaoAutomatica).HasConversion<string>();
            e.Property(p => p.EstadoAtual).HasConversion<string>();

            e.HasMany(p => p.Motivos).WithOne().HasForeignKey(m => m.PedidoId);
            e.HasMany(p => p.Historico).WithOne().HasForeignKey(h => h.PedidoId);
        });

        modelBuilder.Entity<MotivoPedido>().Property(m => m.Decisao).HasConversion<string>();

        modelBuilder.Entity<Simulacao>(e =>
        {
            e.HasIndex(s => s.DataSimulacao);
            e.Property(s => s.SituacaoProfissional).HasConversion<string>();
            e.Property(s => s.Decisao).HasConversion<string>();
        });

        modelBuilder.Entity<HistoricoEstado>(e =>
        {
            e.Property(h => h.EstadoAnterior).HasConversion<string>();
            e.Property(h => h.EstadoNovo).HasConversion<string>();
        });
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // O SQLite não tem tipo decimal: guardamos como REAL (número) em vez de texto,
        // para as queries da Tarefa 4 poderem somar e comparar valores diretamente.
        configurationBuilder.Properties<decimal>().HaveConversion<double>();
    }
}
