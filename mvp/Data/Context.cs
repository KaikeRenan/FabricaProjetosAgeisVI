using Microsoft.EntityFrameworkCore;
using mvp.Entities;

namespace mvp.Data
{
    public class Context : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<HealthPost> HealthPosts { get; set; } = null!;

        public Context(DbContextOptions<Context> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<HealthPost>(entity =>
            {
                entity.OwnsOne(h => h.Address);
            });
        }
    }
}
