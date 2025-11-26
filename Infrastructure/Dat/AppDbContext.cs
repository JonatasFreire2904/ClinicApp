using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Dat
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Clinic> Clinics => Set<Clinic>();
        public DbSet<UserClinic> UserClinics => Set<UserClinic>();
        public DbSet<Material> Materials => Set<Material>();
        public DbSet<ClinicStock> ClinicStocks => Set<ClinicStock>();
        public DbSet<StockMovement> StockMovements => Set<StockMovement>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserClinic>()
                .HasKey(x => new { x.UserId, x.ClinicId });

            modelBuilder.Entity<UserClinic>()
                .HasOne(x => x.User)
                .WithMany(x => x.UserClinics)
                .HasForeignKey(x => x.UserId);

            modelBuilder.Entity<UserClinic>()
                .HasOne(x => x.Clinic)
                .WithMany(x => x.UserClinics)
                .HasForeignKey(x => x.ClinicId);

            // Configure StockMovement: ClinicId is nullable (for warehouse movements)
            modelBuilder.Entity<StockMovement>()
                .Property(s => s.ClinicId)
                .IsRequired(false);
        }
    }
}