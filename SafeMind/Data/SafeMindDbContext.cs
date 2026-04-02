using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SafeMind.Data.Models;
using SafeMind.Data.Enums;

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
            public DbSet<DoctorLanguage> DoctorLanguages { get; set; }
            public DbSet<Session> Sessions { get; set; }
            public DbSet<Article> Articles { get; set; }
            public DbSet<Category> Categories { get; set; }
            public DbSet<ArticleCategory> ArticleCategories { get; set; }
            public DbSet<ArticleLike> ArticleLikes { get; set; }
            public DbSet<Journal> Journals { get; set; }
            public DbSet<DailyCheck> DailyChecks { get; set; }
            public DbSet<Goal> Goals { get; set; }
            public DbSet<SessionContact> SessionContacts { get; set; }
            public DbSet<ChatMessage> ChatMessages { get; set; }
            public DbSet<GoalTemplate> GoalTemplates { get; set; }
            public DbSet<WeeklyGoal> WeeklyGoals { get; set; }
            public DbSet<ContactMessage> ContactMessages { get; set; }
            public DbSet<MoodCheck> MoodChecks { get; set; }
            public DbSet<SessionRating> SessionRatings { get; set; }

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

                        entity.Property(d => d.Price)
                        .HasColumnType("decimal(10,2)");

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
                  builder.Entity<DoctorLanguage>(entity =>
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
                  // -------- Category --------
                  builder.Entity<Category>(entity =>
                  {
                        entity.ToTable("Categories");

                        entity.HasKey(c => c.Id);

                        entity.Property(c => c.Name)
                        .IsRequired()
                        .HasMaxLength(100);

                        entity.HasIndex(c => c.Name)
                        .IsUnique();
                  });

                  // -------- ArticleCategories --------
                  builder.Entity<ArticleCategory>(entity =>
                  {
                        entity.ToTable("ArticleCategories");

                        entity.HasKey(ac => new { ac.ArticleId, ac.CategoryId });

                        entity.HasOne(ac => ac.Article)
                        .WithMany(a => a.ArticleCategories)
                        .HasForeignKey(ac => ac.ArticleId)
                        .OnDelete(DeleteBehavior.Cascade);

                        entity.HasOne(ac => ac.Category)
                        .WithMany(c => c.ArticleCategories)
                        .HasForeignKey(ac => ac.CategoryId)
                        .OnDelete(DeleteBehavior.Cascade);
                  });

                  // -------- ArticleLike --------
                  builder.Entity<ArticleLike>(entity =>
                  {
                        entity.ToTable("ArticleLikes");
                        entity.HasKey(al => new { al.ArticleId, al.UserId });

                        entity.HasOne(al => al.Article)
                        .WithMany()
                        .HasForeignKey(al => al.ArticleId)
                        .OnDelete(DeleteBehavior.Cascade);

                        entity.HasOne(al => al.User)
                        .WithMany()
                        .HasForeignKey(al => al.UserId)
                        .OnDelete(DeleteBehavior.Cascade);
                  });

                  // -------- Journal --------
                  builder.Entity<Journal>(entity =>
                  {
                        entity.ToTable("Journals");
                        entity.HasKey(j => j.Id);

                        entity.Property(j => j.Title)
                        .IsRequired()
                        .HasMaxLength(200);

                        entity.Property(j => j.Content)
                        .IsRequired();

                        entity.Property(j => j.CreatedAt)
                        .IsRequired();

                        entity.Property(j => j.Mood)
                        .HasConversion<int>()
                        .IsRequired();

                        entity.Property(j => j.Category)
                        .HasConversion<int>()
                        .IsRequired();

                        entity.HasOne(j => j.User)
                        .WithMany()
                        .HasForeignKey(j => j.UserId)
                        .OnDelete(DeleteBehavior.Restrict);
                  });
                  // -------- DailyCheck --------
                  builder.Entity<DailyCheck>(entity =>
                  {
                        entity.ToTable("DailyChecks");
                        entity.HasKey(dc => dc.Id);

                        entity.Property(dc => dc.CreatedOn)
                        .IsRequired();

                        entity.Property(dc => dc.Mood)
                        .HasConversion<int>()
                        .IsRequired();

                        entity.Property(dc => dc.Energy)
                        .HasConversion<int>()
                        .IsRequired();

                        entity.Property(dc => dc.Stress)
                        .HasConversion<int>()
                        .IsRequired();

                        entity.Property(dc => dc.Sleep)
                        .HasConversion<int>()
                        .IsRequired();

                        entity.Property(dc => dc.Notes)
                        .IsRequired()
                        .HasMaxLength(1000);

                        entity.HasOne(dc => dc.User)
                        .WithMany()
                        .HasForeignKey(dc => dc.UserId)
                        .OnDelete(DeleteBehavior.Restrict);
                  });
                  // -------- Goal --------
                  builder.Entity<Goal>(entity =>
                  {
                        entity.ToTable("Goals");
                        entity.HasKey(g => g.Id);

                        entity.Property(g => g.Description)
                        .IsRequired()
                        .HasMaxLength(100);

                        entity.Property(g => g.TargetDate)
                        .IsRequired();

                        entity.Property(g => g.IsCompleted)
                        .IsRequired()
                        .HasDefaultValue(false);

                        entity.HasOne(g => g.User)
                        .WithMany()
                        .HasForeignKey(g => g.UserId)
                        .OnDelete(DeleteBehavior.Restrict);
                  });

                  // -------- GoalTemplate --------
                  builder.Entity<GoalTemplate>(entity =>
                  {
                        entity.ToTable("GoalTemplates");
                        entity.HasKey(g => g.Id);
                        entity.Property(g => g.Description).IsRequired().HasMaxLength(200);
                  });

                  // -------- WeeklyGoal --------
                  builder.Entity<WeeklyGoal>(entity =>
                  {
                        entity.ToTable("WeeklyGoals");
                        entity.HasKey(w => w.Id);
                        entity.Property(w => w.WeekStart).IsRequired();
                        entity.Property(w => w.IsCompleted).IsRequired().HasDefaultValue(false);
                        entity.HasOne(w => w.GoalTemplate).WithMany().HasForeignKey(w => w.GoalTemplateId).OnDelete(DeleteBehavior.Cascade);
                        entity.HasOne(w => w.User).WithMany().HasForeignKey(w => w.UserId).OnDelete(DeleteBehavior.Restrict);
                  });

                  // -------- ContactMessage --------
                  builder.Entity<ContactMessage>(entity =>
                  {
                        entity.ToTable("ContactMessages");
                        entity.HasKey(c => c.Id);
                        entity.Property(c => c.FullName).IsRequired().HasMaxLength(100);
                        entity.Property(c => c.Email).IsRequired().HasMaxLength(200);
                        entity.Property(c => c.Subject).IsRequired().HasMaxLength(100);
                        entity.Property(c => c.Message).IsRequired().HasMaxLength(2000);
                        entity.Property(c => c.SubmittedOn).IsRequired();
                        entity.Property(c => c.IsRead).IsRequired().HasDefaultValue(false);
                        entity.Property(c => c.IsArchived).IsRequired().HasDefaultValue(false);
                  });

                  // -------- SessionRating --------
                  builder.Entity<SessionRating>(entity =>
                  {
                        entity.ToTable("SessionRatings");
                        entity.HasKey(r => new { r.SessionId, r.PatientId });

                        entity.Property(r => r.Stars)
                              .IsRequired();

                        entity.Property(r => r.CreatedAt)
                              .IsRequired();

                        entity.HasOne(r => r.Session)
                              .WithMany()
                              .HasForeignKey(r => r.SessionId)
                              .OnDelete(DeleteBehavior.Cascade);

                        entity.HasOne(r => r.Patient)
                              .WithMany()
                              .HasForeignKey(r => r.PatientId)
                              .OnDelete(DeleteBehavior.Restrict);
                  });

                  // -------- MoodCheck --------
                  builder.Entity<MoodCheck>(entity =>
                  {
                        entity.ToTable("MoodChecks");
                        entity.HasKey(m => m.Id);
                        entity.Property(m => m.Mood).IsRequired().HasMaxLength(20);
                        entity.Property(m => m.SavedAt).IsRequired();
                        entity.HasOne(m => m.User)
                              .WithMany()
                              .HasForeignKey(m => m.UserId)
                              .OnDelete(DeleteBehavior.Cascade);
                  });

            }
      }
}
