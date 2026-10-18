using Microsoft.EntityFrameworkCore;
using FlowerDelivery.Domain.Entities;

namespace FlowerDelivery.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<Courier> Couriers { get; set; }
    public DbSet<Bouquet> Bouquets { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Delivery> Deliveries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasDiscriminator<string>("Role")
            .HasValue<User>("User")
            .HasValue<Client>("Client")
            .HasValue<Courier>("Courier");

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Client)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Delivery)
            .WithOne(d => d.Order)
            .HasForeignKey<Delivery>(d => d.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Order)
            .WithMany(o => o.Items)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Bouquet)
            .WithMany(b => b.OrderItems)
            .HasForeignKey(oi => oi.BouquetId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Delivery>()
            .HasOne(d => d.Courier)
            .WithMany(c => c.Deliveries)
            .HasForeignKey(d => d.CourierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}