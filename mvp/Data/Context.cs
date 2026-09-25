using Microsoft.EntityFrameworkCore;
using mvp.Entities;

namespace mvp.Data
{
    public class Context : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<HealthUnit> HealthUnits { get; set; } = null!;
        public DbSet<Stock> Stocks { get; set; } = null!;
        public DbSet<Medicine> Medicine { get; set; } = null;

        public Context(DbContextOptions<Context> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<HealthUnit>(entity =>
            {
                entity.OwnsOne(h => h.Address);
            });
        }
    }
}
