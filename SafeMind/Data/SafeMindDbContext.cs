using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Data.Models;
using Microsoft.AspNetCore.Identity;
namespace SafeMind.Data;

public class SafeMindDbContext : IdentityDbContext
{
    public SafeMindDbContext(DbContextOptions<SafeMindDbContext> options)
        : base(options)
    {
    }
    public DbSet<Specialty> Specialties { get; set; }
    public DbSet<Doctor> Doctors { get; set; }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Specialty>(entity =>
    {
        entity.ToTable("Specialties");

        entity.HasKey(e => e.Id);

        entity.Property(e => e.Name)
              .IsRequired()
              .HasMaxLength(200);
    });
        builder.Entity<Doctor>(entity =>
            {
                entity.ToTable("Doctors");

                entity.HasKey(e => e.Id);

                entity.HasOne<IdentityUser>()
                      .WithOne()
                      .HasForeignKey<Doctor>(d => d.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.Rating)
                      .HasColumnType("decimal(3,2)");
            });
        
    }
}
