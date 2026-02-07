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
        public DbSet<LicenceSpecialty> LicenceSpecialties { get; set; } = null!;
        public DbSet<LicenceDoctorSpecialty> LicenceDoctorSpecialties { get; set; } = null!;

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
            modelBuilder.Entity<LicenceSpecialty>(entity =>
            {
                entity.ToTable("LicenceSpecialties");
            });
            modelBuilder.Entity<LicenceDoctorSpecialty>(entity =>
            {
                entity.ToTable("LicenceDoctorSpecialties");
                entity.HasKey(ld => new { ld.DoctorLicenseId, ld.SpecialtyId });
                entity.HasOne(ld => ld.DoctorLicense)
                    .WithMany(d => d.DoctorLicenseSpecialties)
                    .HasForeignKey(ld => ld.DoctorLicenseId);
                entity.HasOne(ld => ld.Specialty)
                    .WithMany(s => s.DoctorLicenceSpecialties)
                    .HasForeignKey(ld => ld.SpecialtyId);
            });
        }
    }
}
