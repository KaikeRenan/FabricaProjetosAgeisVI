using Microsoft.EntityFrameworkCore;
using mvp.Entities;
using mvp.ValueObjects;

namespace mvp.Data
{
    public class Context : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<HealthUnit> HealthUnits { get; set; } = null!;
        public DbSet<Stock> Stocks { get; set; } = null!;
        public DbSet<Batch> Batches { get; set; } = null!;
        public DbSet<ExpirationAlert> ExpirationAlerts { get; set; } = null!;
        public DbSet<Medicine> Medicines { get; set; } = null!;
        public DbSet<Pharmacy> Pharmacies { get; set; } = null!;
        public DbSet<StockMovement> StockMovements { get; set; } = null!;

        public Context(DbContextOptions<Context> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");

                entity.HasKey(u => u.Id);

                entity.Property(u => u.Id)
                    .HasColumnName("id");

                entity.Property(u => u.Email)
                    .HasColumnName("email")
                    .HasConversion(
                        email => email.Value,
                        value => new Email(value))
                    .IsRequired();

                entity.Property(u => u.Password)
                    .HasColumnName("password")
                    .HasConversion(
                        password => password.Value,
                        value => Password.FromHash(value))
                    .IsRequired();

                entity.Property(u => u.CreatedAt)
                    .HasColumnName("created_at")
                    .IsRequired();

                entity.Property(u => u.UpdatedAt)
                    .HasColumnName("updated_at")
                    .IsRequired();

                entity.Property(u => u.RemovedAt)
                    .HasColumnName("removed_at");
            });

            // HealthUnit
            modelBuilder.Entity<HealthUnit>(entity =>
            {
                entity.ToTable("health_units");

                entity.HasKey(h => h.Id);

                entity.Property(h => h.Id)
                    .HasColumnName("id");

                entity.Property(h => h.Name)
                    .HasColumnName("name")
                    .IsRequired();

                entity.Property(h => h.CNES)
                    .HasColumnName("cnes")
                    .HasConversion(
                        cnes => cnes.Value,
                        value => new CNES(value))
                    .IsRequired();

                entity.OwnsOne(h => h.Address, address =>
                {
                    address.Property(a => a.Street)
                        .HasColumnName("street")
                        .IsRequired();

                    address.Property(a => a.Number)
                        .HasColumnName("number");

                    address.Property(a => a.Neighborhood)
                        .HasColumnName("neighborhood");

                    address.Property(a => a.ZipCode)
                        .HasColumnName("zip_code")
                        .IsRequired();

                    address.Property(a => a.Zone)
                        .HasColumnName("zone");
                });

                entity.Property(h => h.CreatedAt)
                    .HasColumnName("created_at")
                    .IsRequired();

                entity.Property(h => h.UpdatedAt)
                    .HasColumnName("updated_at")
                    .IsRequired();

                entity.Property(h => h.RemovedAt)
                    .HasColumnName("removed_at");
            });

            // Pharmacy
            modelBuilder.Entity<Pharmacy>(entity =>
            {
                entity.ToTable("pharmacies");

                entity.HasKey(p => p.Id);

                entity.Property(p => p.Id)
                    .HasColumnName("id");

                entity.Property(p => p.Name)
                    .HasColumnName("name")
                    .IsRequired();

                entity.Property(p => p.CNPJ)
                    .HasColumnName("cnpj")
                    .HasConversion(
                        cnpj => cnpj.Value,
                        value => new CNPJ(value))
                    .IsRequired();

                entity.Property(p => p.PhoneNumber)
                    .HasColumnName("phone_number")
                    .HasConversion(
                        phone => phone.Value,
                        value => new PhoneNumber(value))
                    .IsRequired();

                entity.OwnsOne(p => p.Address, address =>
                {
                    address.Property(a => a.Street)
                        .HasColumnName("street")
                        .IsRequired();

                    address.Property(a => a.Number)
                        .HasColumnName("number");

                    address.Property(a => a.Neighborhood)
                        .HasColumnName("neighborhood");

                    address.Property(a => a.ZipCode)
                        .HasColumnName("zip_code")
                        .IsRequired();

                    address.Property(a => a.Zone)
                        .HasColumnName("zone");
                });

                entity.Property(p => p.CreatedAt)
                    .HasColumnName("created_at")
                    .IsRequired();

                entity.Property(p => p.UpdatedAt)
                    .HasColumnName("updated_at")
                    .IsRequired();

                entity.Property(p => p.RemovedAt)
                    .HasColumnName("removed_at");
            });

            // Medicine
            modelBuilder.Entity<Medicine>(entity =>
            {
                entity.ToTable("medicines");

                entity.HasKey(m => m.Id);

                entity.Property(m => m.Id)
                    .HasColumnName("id");

                entity.Property(m => m.Name)
                    .HasColumnName("name")
                    .IsRequired();

                entity.Property(m => m.ActiveIngredient)
                    .HasColumnName("active_ingredient")
                    .IsRequired();

                entity.Property(m => m.Dosage)
                    .HasColumnName("dosage")
                    .IsRequired();

                entity.Property(m => m.Unit)
                    .HasColumnName("unit")
                    .IsRequired();

                entity.Property(m => m.CreatedAt)
                    .HasColumnName("created_at")
                    .IsRequired();

                entity.Property(m => m.UpdatedAt)
                    .HasColumnName("updated_at")
                    .IsRequired();

                entity.Property(m => m.RemovedAt)
                    .HasColumnName("removed_at");
            });

            // Batch
            modelBuilder.Entity<Batch>(entity =>
            {
                entity.ToTable("batches");

                entity.HasKey(b => b.Id);

                entity.Property(b => b.Id)
                    .HasColumnName("id");

                entity.Property(b => b.MedicineId)
                    .HasColumnName("medicine_id");

                entity.Property(b => b.BatchNumber)
                    .HasColumnName("batch_number")
                    .IsRequired();

                entity.Property(b => b.FabricationDate)
                    .HasColumnName("fabrication_date")
                    .IsRequired();

                entity.Property(b => b.ExpirationDate)
                    .HasColumnName("expiration_date")
                    .IsRequired();

                entity.HasOne<Medicine>()
                    .WithMany()
                    .HasForeignKey(b => b.MedicineId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.Property(b => b.CreatedAt)
                    .HasColumnName("created_at")
                    .IsRequired();

                entity.Property(b => b.UpdatedAt)
                    .HasColumnName("updated_at")
                    .IsRequired();

                entity.Property(b => b.RemovedAt)
                    .HasColumnName("removed_at");
            });

            // ExpirationAlert
            modelBuilder.Entity<ExpirationAlert>(entity =>
            {
                entity.ToTable("expiration_alerts");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("id");

                entity.Property(e => e.BatchId)
                    .HasColumnName("batch_id");

                entity.Property(e => e.AlertDate)
                    .HasColumnName("alert_date")
                    .IsRequired();

                entity.Property(e => e.Resolved)
                    .HasColumnName("resolved")
                    .IsRequired();

                entity.HasOne<Batch>()
                    .WithMany()
                    .HasForeignKey(e => e.BatchId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at")
                    .IsRequired();

                entity.Property(e => e.UpdatedAt)
                    .HasColumnName("updated_at")
                    .IsRequired();

                entity.Property(e => e.RemovedAt)
                    .HasColumnName("removed_at");
            });

            // Stock
            modelBuilder.Entity<Stock>(entity =>
            {
                entity.ToTable("stocks");

                entity.HasKey(s => s.Id);

                entity.Property(s => s.Id)
                    .HasColumnName("id");

                entity.Property(s => s.PharmacyId)
                    .HasColumnName("pharmacy_id");

                entity.Property(s => s.HealthUnitId)
                    .HasColumnName("health_unit_id");

                entity.Property(s => s.BatchId)
                    .HasColumnName("batch_id");

                entity.Property(s => s.Quantity)
                    .HasColumnName("quantity")
                    .IsRequired();

                entity.HasOne<Pharmacy>()
                    .WithMany()
                    .HasForeignKey(s => s.PharmacyId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne<HealthUnit>()
                    .WithMany()
                    .HasForeignKey(s => s.HealthUnitId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne<Batch>()
                    .WithMany()
                    .HasForeignKey(s => s.BatchId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.Property(s => s.CreatedAt)
                    .HasColumnName("created_at")
                    .IsRequired();

                entity.Property(s => s.UpdatedAt)
                    .HasColumnName("updated_at")
                    .IsRequired();

                entity.Property(s => s.RemovedAt)
                    .HasColumnName("removed_at");
            });

            // StockMovement
            modelBuilder.Entity<StockMovement>(entity =>
            {
                entity.ToTable("stock_movements");

                entity.HasKey(s => s.Id);

                entity.Property(s => s.Id)
                    .HasColumnName("id");

                entity.Property(s => s.StockId)
                    .HasColumnName("stock_id");

                entity.Property(s => s.Type)
                    .HasColumnName("type")
                    .HasConversion<string>()
                    .IsRequired();

                entity.Property(s => s.Quantity)
                    .HasColumnName("quantity")
                    .IsRequired();

                entity.HasOne<Stock>()
                    .WithMany()
                    .HasForeignKey(s => s.StockId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.Property(s => s.CreatedAt)
                    .HasColumnName("created_at")
                    .IsRequired();

                entity.Property(s => s.UpdatedAt)
                    .HasColumnName("updated_at")
                    .IsRequired();

                entity.Property(s => s.RemovedAt)
                    .HasColumnName("removed_at");
            });
        }
    }
}
