using Microsoft.EntityFrameworkCore;
using Data.Models;

namespace SafeMind.Data
{
    public class DoctorLicensingDbContext : DbContext
    {
        public DoctorLicensingDbContext(DbContextOptions<DoctorLicensingDbContext> options)
            : base(options)
        {
        }

        public DbSet<DoctorLicense> DoctorLicenses { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DoctorLicense>(entity =>
            {
                entity.ToTable("DoctorLicenses");
                entity.HasIndex(d => d.LicenseNumber).IsUnique();
                entity.HasIndex(d => d.NationalId);
                entity.Property(d => d.Status).HasDefaultValue("Active");
            });
        }
    }
}
