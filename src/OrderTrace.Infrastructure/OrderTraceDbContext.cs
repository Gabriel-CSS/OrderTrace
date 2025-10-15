using Microsoft.EntityFrameworkCore;
using OrderTrace.Core.Entities;
using OrderTrace.Infrastructure.Configurations;

namespace OrderTrace.Infrastructure;

public class OrderTraceDbContext : DbContext
{
    public OrderTraceDbContext(DbContextOptions<OrderTraceDbContext> options)
        : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Aplica configurações separadas
        modelBuilder.ApplyConfiguration(new OrderConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentConfiguration());
        modelBuilder.ApplyConfiguration(new TransactionConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}
