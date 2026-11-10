using Farmacia_site.Models;
using Microsoft.EntityFrameworkCore;

namespace Farmacia_site.Data;

public sealed class Contexto(DbContextOptions<Contexto> options) : DbContext(options)
{
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<ItemPedido> ItensPedido => Set<ItemPedido>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Produto>(entity =>
        {
            entity.ToTable("Produtos");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Nome).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Descricao).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Categoria).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Ilustracao).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Selo).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Preco).HasPrecision(12, 2);
            entity.Property(x => x.PrecoAnterior).HasPrecision(12, 2);
            entity.Property(x => x.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("Usuarios");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Nome).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(180).IsRequired();
            entity.Property(x => x.SenhaHash).IsRequired();
            entity.Property(x => x.Perfil).HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<Pedido>(entity =>
        {
            entity.ToTable("Pedidos");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Total).HasPrecision(12, 2);
            entity.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId);
        });

        modelBuilder.Entity<ItemPedido>(entity =>
        {
            entity.ToTable("ItensPedido");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.NomeProduto).IsRequired();
            entity.Property(x => x.PrecoUnitario).HasPrecision(12, 2);
            entity.HasOne(x => x.Pedido).WithMany(x => x.Itens)
                .HasForeignKey(x => x.PedidoId);
            entity.HasOne(x => x.Produto).WithMany().HasForeignKey(x => x.ProdutoId);
        });
    }
}
