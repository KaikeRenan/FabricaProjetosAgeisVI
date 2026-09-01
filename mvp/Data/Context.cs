using Microsoft.EntityFrameworkCore;
using mvp.Entities;

namespace mvp.Data
{
    public class Context : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;

        public Context(DbContextOptions<Context> options) : base(options) { }

    }
}
