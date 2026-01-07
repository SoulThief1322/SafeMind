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
        public DbSet<Session> Sessions { get; set; }
        public DbSet<Article> Articles { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // -------- Doctor --------
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

            // -------- Specialty --------
            builder.Entity<Specialty>(entity =>
            {
                entity.ToTable("Specialties");
                entity.HasKey(s => s.Id);

                entity.Property(s => s.Name)
                      .IsRequired()
                      .HasMaxLength(200);
            });

            // -------- DoctorSpecialty --------
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

            // -------- Language --------
            builder.Entity<Language>(entity =>
            {
                entity.ToTable("Languages");
                entity.HasKey(l => l.Id);

                entity.Property(l => l.Name)
                      .IsRequired()
                      .HasMaxLength(50);
            });

            // -------- DoctorLanguages --------
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

            // -------- Session --------
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

            // -------- Article --------
            builder.Entity<Article>(entity =>
            {
                entity.ToTable("Articles");
                entity.HasKey(a => a.Id);

                entity.Property(a => a.Headline)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(a => a.Content)
                      .IsRequired();

                entity.Property(a => a.ImagePath)
                      .HasMaxLength(500);

                entity.Property(a => a.PublishedOn)
                      .IsRequired();

                entity.Property(a => a.ViewCount)
                      .HasDefaultValue(0);

                entity.Property(a => a.ViewsInLastWeek)
                      .HasDefaultValue(0);

                entity.Property(a => a.Likes)
                      .HasDefaultValue(0);

                entity.HasOne(a => a.Author)
                      .WithMany()
                      .HasForeignKey(a => a.AuthorId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
