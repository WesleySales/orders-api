using Microsoft.EntityFrameworkCore;
using OrdersApi.Models;

namespace OrdersApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<ItemPedido> Itens => Set<ItemPedido>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Pedido>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.NomeCliente).IsRequired().HasMaxLength(150);
            entity.Property(p => p.ValorTotal).HasPrecision(18, 2);

            entity.HasMany(p => p.Itens)
                  .WithOne()
                  .HasForeignKey("PedidoId")
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ItemPedido>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.NomeProduto).IsRequired().HasMaxLength(150);
            entity.Property(i => i.PrecoUnitario).HasPrecision(18, 2);
        });
    }
}