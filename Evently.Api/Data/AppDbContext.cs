using Microsoft.EntityFrameworkCore;
using Evently.Api.Models;

namespace Evently.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<TicketBatch> TicketBatches { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<Review> Reviews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Favorite>()
                .HasIndex(f => new { f.UserId, f.EventId })
                .IsUnique();

            modelBuilder.Entity<Coupon>()
                .HasIndex(c => new { c.EventId, c.Code })
                .IsUnique();

            modelBuilder.Entity<Transaction>()
                .HasIndex(t => new { t.Provider, t.ProviderTransactionId })
                .IsUnique();
        }
    }
}
