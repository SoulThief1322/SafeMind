using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Data.Models;
using Data.Enums;

namespace SafeMind.Data
{
    public class SafeMindDbContext : IdentityDbContext<IdentityUser>
    {
        public SafeMindDbContext(DbContextOptions<SafeMindDbContext> options)
            : base(options)
        {
        }

        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Specialty> Specialties { get; set; }
        public DbSet<DoctorSpecialty> DoctorSpecialties { get; set; }
        public DbSet<Language> Languages { get; set; }
        public DbSet<DoctorLanguages> DoctorLanguages { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);


            builder.Entity<Doctor>(entity =>
            {
                entity.ToTable("Doctors");

                entity.HasKey(d => d.Id);

                entity.Property(d => d.Name)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(d => d.Rating)
                      .HasColumnType("decimal(3,2)");

                entity.HasOne(d => d.User)
                      .WithOne()
                      .HasForeignKey<Doctor>(d => d.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });


            builder.Entity<Specialty>(entity =>
            {
                entity.ToTable("Specialties");

                entity.HasKey(s => s.Id);

                entity.Property(s => s.Name)
                      .IsRequired()
                      .HasMaxLength(200);
            });


            builder.Entity<DoctorSpecialty>(entity =>
            {
                entity.ToTable("DoctorSpecialties");

                entity.HasKey(ds => new { ds.DoctorId, ds.SpecialtyId });

                entity.HasOne(ds => ds.Doctor)
                      .WithMany(d => d.DoctorSpecialties)
                      .HasForeignKey(ds => ds.DoctorId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ds => ds.Specialty)
                      .WithMany(s => s.DoctorSpecialties)
                      .HasForeignKey(ds => ds.SpecialtyId)
                      .OnDelete(DeleteBehavior.Cascade);
            });


            builder.Entity<Language>(entity =>
            {
                entity.ToTable("Languages");

                entity.HasKey(l => l.Id);

                entity.Property(l => l.Name)
                      .IsRequired()
                      .HasMaxLength(50);
            });

            builder.Entity<DoctorLanguages>(entity =>
{
    entity.ToTable("DoctorLanguages");

    entity.HasKey(dl => new { dl.DoctorId, dl.LanguageId });

    entity.HasOne(dl => dl.Doctor)
          .WithMany(d => d.DoctorLanguages)
          .HasForeignKey(dl => dl.DoctorId)
          .OnDelete(DeleteBehavior.Cascade);

    entity.HasOne(dl => dl.Language)
          .WithMany(l => l.DoctorLanguages)
          .HasForeignKey(dl => dl.LanguageId)
          .OnDelete(DeleteBehavior.Cascade);
});
            builder.Entity<Session>(entity =>
            {
                entity.Property(s => s.SessionStatus)
                      .HasConversion<int>()
                      .HasDefaultValue(SessionStatus.Scheduled);

                entity.Property(s => s.PaymentStatus)
                      .HasConversion<int>()
                      .HasDefaultValue(PaymentStatus.Pending);

                entity.Property(s => s.Price)
                      .HasColumnType("decimal(8,2)");
            });
        }

    }

}
