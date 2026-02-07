using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SafeMind.Data;

namespace SafeMind.Services
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var provider = scope.ServiceProvider;

            var mainContext = provider.GetRequiredService<SafeMindDbContext>();
            var licensingContext = provider.GetRequiredService<DoctorLicensingDbContext>();
            var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = provider.GetRequiredService<UserManager<IdentityUser>>();

            await mainContext.Database.MigrateAsync();
            await licensingContext.Database.MigrateAsync();

            await EnsureRolesAsync(roleManager, new[] { "Admin", "Doctor", "User" });

            var users = await EnsureUsersAsync(userManager);

            await SeedDoctorLicensesAsync(licensingContext);
            await SeedCoreLookupsAsync(mainContext);
            await SeedDoctorsAsync(mainContext, users);
        }

        private static async Task EnsureRolesAsync(RoleManager<IdentityRole> roleManager, IEnumerable<string> roles)
        {
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        private static async Task<Dictionary<string, IdentityUser>> EnsureUsersAsync(UserManager<IdentityUser> userManager)
        {
            var results = new Dictionary<string, IdentityUser>();

            var seeds = new (string Email, string Password, string Role)[]
            {
                ("lyubomira.hristova@safemind.bg", "Admin123!", "Admin"),
                ("aleksandar.dimitrov@safemind.bg", "Password123!", "Doctor"),
                ("borislava.ivanova@safemind.bg", "Password123!", "Doctor"),
                ("viktor.petrov@safemind.bg", "Password123!", "Doctor"),
                ("desislava.georgieva@safemind.bg", "Password123!", "Doctor"),
                ("emil.nikolov@safemind.bg", "Password123!", "Doctor"),
                ("gabriela.stoyanova@safemind.bg", "Password123!", "User"),
                ("hristo.kolev@safemind.bg", "Password123!", "User"),
                ("iva.marinova@safemind.bg", "Password123!", "User"),
                ("kalin.todorov@safemind.bg", "Password123!", "User"),
            };

            foreach (var seed in seeds)
            {
                var user = await userManager.FindByEmailAsync(seed.Email);
                if (user == null)
                {
                    user = new IdentityUser { UserName = seed.Email, Email = seed.Email, EmailConfirmed = true };
                    await userManager.CreateAsync(user, seed.Password);
                }
                else if (!user.EmailConfirmed)
                {
                    user.EmailConfirmed = true;
                    await userManager.UpdateAsync(user);
                }

                if (!await userManager.IsInRoleAsync(user, seed.Role))
                {
                    await userManager.AddToRoleAsync(user, seed.Role);
                }

                results[seed.Email] = user;
            }

            return results;
        }

        private static async Task SeedDoctorLicensesAsync(DoctorLicensingDbContext ctx)
        {
            if (await ctx.DoctorLicenses.AnyAsync()) return;

            // Seed specialties first
            if (!await ctx.LicenceSpecialties.AnyAsync())
            {
                var specialties = new[]
                {
                    "Psychiatry", "Clinical Psychology", "Counseling", "Neurology", "Family Medicine", 
                    "Pediatrics", "Addiction Medicine", "Geriatrics", "Behavioral Therapy", "Child Psychology"
                };
                ctx.LicenceSpecialties.AddRange(specialties.Select(name => new LicenceSpecialty { Name = name }));
                await ctx.SaveChangesAsync();
            }

            var specialtyMap = await ctx.LicenceSpecialties.ToDictionaryAsync(s => s.Name, s => s.Id);

            var licenseData = new[]
            {
                new { LicenseNumber = "1000000001", FullName = "Aleksandar Dimitrov", NationalId = "8001012345", Specialty = "Psychiatry" },
                new { LicenseNumber = "1000000002", FullName = "Borislava Ivanova", NationalId = "8205123456", Specialty = "Clinical Psychology" },
                new { LicenseNumber = "1000000003", FullName = "Viktor Petrov", NationalId = "7909234567", Specialty = "Counseling" },
                new { LicenseNumber = "1000000004", FullName = "Desislava Georgieva", NationalId = "8507045678", Specialty = "Neurology" },
                new { LicenseNumber = "1000000005", FullName = "Emil Nikolov", NationalId = "8803156789", Specialty = "Family Medicine" },
                new { LicenseNumber = "1000000006", FullName = "Gabriela Stoyanova", NationalId = "9008267890", Specialty = "Pediatrics" },
                new { LicenseNumber = "1000000007", FullName = "Hristo Kolev", NationalId = "7701078901", Specialty = "Psychiatry" },
                new { LicenseNumber = "1000000008", FullName = "Iva Marinova", NationalId = "8602189012", Specialty = "Geriatrics" },
                new { LicenseNumber = "1000000009", FullName = "Kalin Todorov", NationalId = "8403290123", Specialty = "Addiction Medicine" },
                new { LicenseNumber = "1000000010", FullName = "Lyubomira Hristova", NationalId = "9104301234", Specialty = "Child Psychology" }
            };

            foreach (var data in licenseData)
            {
                var license = new DoctorLicense
                {
                    LicenseNumber = data.LicenseNumber,
                    FullName = data.FullName,
                    NationalId = data.NationalId,
                    IssuingAuthority = "Bulgarian Medical Association",
                    IssuedOn = DateTime.Now.AddYears(-5),
                    ExpiresOn = DateTime.Now.AddYears(5),
                    Status = "Active"
                };
                ctx.DoctorLicenses.Add(license);
                await ctx.SaveChangesAsync();

                if (specialtyMap.TryGetValue(data.Specialty, out var specialtyId))
                {
                    ctx.LicenceDoctorSpecialties.Add(new LicenceDoctorSpecialty
                    {
                        DoctorLicenseId = license.Id,
                        SpecialtyId = specialtyId
                    });
                }
            }

            await ctx.SaveChangesAsync();
        }

        private static async Task SeedCoreLookupsAsync(SafeMindDbContext ctx)
        {
            if (!await ctx.Specialties.AnyAsync())
            {
                var specialties = new[]
                {
                    "Psychiatry", "Clinical Psychology", "Counseling", "Neurology", "Family Medicine", "Pediatrics", "Addiction Medicine", "Geriatrics", "Behavioral Therapy", "Child Psychology"
                };
                ctx.Specialties.AddRange(specialties.Select((name, i) => new Specialty { Name = name }));
                await ctx.SaveChangesAsync();
            }

            if (!await ctx.Languages.AnyAsync())
            {
                var languages = new[] { "English", "Spanish", "French", "German", "Mandarin", "Arabic", "Hindi", "Portuguese", "Russian", "Japanese" };
                ctx.Languages.AddRange(languages.Select(l => new Language { Name = l }));
                await ctx.SaveChangesAsync();
            }
        }

        private static async Task SeedDoctorsAsync(SafeMindDbContext ctx, Dictionary<string, IdentityUser> users)
        {
            if (await ctx.Doctors.AnyAsync()) return;

            var specialtyMap = await ctx.Specialties.ToDictionaryAsync(s => s.Name, s => s.Id);
            var languageMap = await ctx.Languages.ToDictionaryAsync(l => l.Name, l => l.Id);

            var doctorSeeds = new[]
            {
                new { Email = "aleksandar.dimitrov@safemind.bg", Name = "Aleksandar Dimitrov", License = "1000000001", Specialty = "Psychiatry", Languages = new[]{"English","Russian"}, WorkStart = new TimeOnly(8,0), WorkEnd = new TimeOnly(16,0), Duration = 50, Rating = 4.8m, Biography = "Psychiatrist focused on mood and anxiety disorders, combining medication management with CBT principles, sleep and lifestyle coaching, and measurement-based care. He designs phased care plans with clear milestones, coordinates with primary care when needed, and offers short, structured check-ins to keep gains stable. Fluent in English and Russian, he works with adults who want practical steps, transparent medication decisions, and a steady partner through recovery." },
                new { Email = "borislava.ivanova@safemind.bg", Name = "Borislava Ivanova", License = "1000000002", Specialty = "Clinical Psychology", Languages = new[]{"English"}, WorkStart = new TimeOnly(9,0), WorkEnd = new TimeOnly(17,0), Duration = 60, Rating = 4.6m, Biography = "Clinical psychologist helping adults navigate stress, burnout, perfectionism, and relationship strain. She uses CBT, ACT, and compassion-based work to reduce rumination, build emotion regulation, and restore boundaries. Sessions include practical experiments between visits, concise progress tracking, and skills you can apply the same week. Borislava tailors pacing to each client and collaborates closely with medical teams when somatic factors matter." },
                new { Email = "viktor.petrov@safemind.bg", Name = "Viktor Petrov", License = "1000000003", Specialty = "Counseling", Languages = new[]{"English","German"}, WorkStart = new TimeOnly(10,0), WorkEnd = new TimeOnly(18,0), Duration = 45, Rating = 4.5m, Biography = "Counselor supporting young professionals through life transitions, relocation stress, and career pivots. Viktor blends solution-focused counseling with strengths-based coaching, helping clients clarify priorities, set realistic habits, and communicate confidently at work and home. Sessions emphasize actionable steps, short feedback loops, and tools for managing pressure without losing momentum. He works in English and German and keeps plans concise and measurable." },
                new { Email = "desislava.georgieva@safemind.bg", Name = "Desislava Georgieva", License = "1000000004", Specialty = "Neurology", Languages = new[]{"English"}, WorkStart = new TimeOnly(7,30), WorkEnd = new TimeOnly(15,30), Duration = 50, Rating = 4.1m, Biography = "Neurologist with focus on headache medicine and cognitive health. Desislava pairs thorough assessment with lifestyle and sleep guidance, teaches patients to spot triggers early, and coordinates with mental health teams when stress drives symptoms. Her style is direct and educational, using clear care plans, medication safety reviews, and practical next steps patients can implement between visits." },
                new { Email = "emil.nikolov@safemind.bg", Name = "Emil Nikolov", License = "1000000005", Specialty = "Family Medicine", Languages = new[]{"English","Russian"}, WorkStart = new TimeOnly(8,30), WorkEnd = new TimeOnly(16,30), Duration = 55, Rating = 4.7m, Biography = "Family physician emphasizing preventive care, chronic condition stabilization, and collaborative decision-making. Emil helps patients build sustainable routines around sleep, movement, and nutrition, and he keeps care plans readable and realistic. He is comfortable coordinating with specialists, aligning medications, and making sure families understand the why behind each recommendation." },
                new { Email = "gabriela.stoyanova@safemind.bg", Name = "Gabriela Stoyanova", License = "1000000006", Specialty = "Pediatrics", Languages = new[]{"English"}, WorkStart = new TimeOnly(11,0), WorkEnd = new TimeOnly(19,0), Duration = 50, Rating = 4.3m, Biography = "Pediatrician who partners with families to support healthy development and early intervention. Gabriela focuses on building trust with children, offering practical guidance on sleep routines, nutrition, and school readiness, and coordinating with teachers when needed. She explains care steps simply, uses play to reduce anxiety, and keeps parents equipped with clear next actions." },
                new { Email = "hristo.kolev@safemind.bg", Name = "Hristo Kolev", License = "1000000007", Specialty = "Psychiatry", Languages = new[]{"English"}, WorkStart = new TimeOnly(8,0), WorkEnd = new TimeOnly(14,0), Duration = 40, Rating = 4.0m, Biography = "Psychiatrist focusing on depression, trauma recovery, and sleep-related issues. Hristo offers careful medication oversight, teaches grounding and pacing strategies, and works in short, focused sessions that balance symptom relief with resilience-building. He coordinates with therapists for integrated care and helps patients make informed, low-drama medication decisions." },
                new { Email = "iva.marinova@safemind.bg", Name = "Iva Marinova", License = "1000000008", Specialty = "Geriatrics", Languages = new[]{"English","French"}, WorkStart = new TimeOnly(9,30), WorkEnd = new TimeOnly(17,30), Duration = 45, Rating = 4.4m, Biography = "Geriatric specialist supporting older adults with cognitive changes, mobility concerns, and medication complexity. Iva takes time to include families in planning, simplifies regimens, and sets practical goals that respect independence. She collaborates with physiotherapists and mental health clinicians to keep patients active, oriented, and confident." },
                new { Email = "kalin.todorov@safemind.bg", Name = "Kalin Todorov", License = "1000000009", Specialty = "Addiction Medicine", Languages = new[]{"English"}, WorkStart = new TimeOnly(12,0), WorkEnd = new TimeOnly(20,0), Duration = 60, Rating = 4.2m, Biography = "Addiction medicine physician experienced with medication-assisted treatment, relapse prevention, and family engagement. Kalin builds transparent plans with craving management tools, routine check-ins, and clear markers of progress. He keeps sessions practical, balancing accountability with supportive coaching so patients can stabilize work, relationships, and health." },
                new { Email = "lyubomira.hristova@safemind.bg", Name = "Lyubomira Hristova", License = "1000000010", Specialty = "Child Psychology", Languages = new[]{"English"}, WorkStart = new TimeOnly(6,0), WorkEnd = new TimeOnly(14,0), Duration = 50, Rating = 4.9m, Biography = "Child psychologist using play-based approaches and parent coaching to build emotional regulation and school readiness. Lyubomira works closely with caregivers and teachers, creates simple home routines, and tailors strategies for attention, anxiety, and social skills. She keeps feedback concrete so families know exactly what to practice between visits." }
            };

            foreach (var d in doctorSeeds)
            {
                if (!users.TryGetValue(d.Email, out var user)) continue;
                if (!specialtyMap.TryGetValue(d.Specialty, out var specId)) continue;

                var doctor = new Doctor
                {
                    UserId = user.Id,
                    Name = d.Name,
                    Biography = d.Biography,
                    WorkStart = d.WorkStart,
                    WorkEnd = d.WorkEnd,
                    SessionDuration = d.Duration,
                    Rating = d.Rating
                };

                ctx.Doctors.Add(doctor);
                await ctx.SaveChangesAsync();

                ctx.DoctorSpecialties.Add(new DoctorSpecialty { DoctorId = doctor.Id, SpecialtyId = specId });

                foreach (var langName in d.Languages)
                {
                    if (languageMap.TryGetValue(langName, out var langId))
                    {
                        ctx.DoctorLanguages.Add(new DoctorLanguage { DoctorId = doctor.Id, LanguageId = langId });
                    }
                }

                await ctx.SaveChangesAsync();
            }
        }
    }
}
