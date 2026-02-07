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
            var hasher = provider.GetRequiredService<IDeterministicHasher>();
            var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = provider.GetRequiredService<UserManager<IdentityUser>>();

            await mainContext.Database.MigrateAsync();
            await licensingContext.Database.MigrateAsync();

            await EnsureRolesAsync(roleManager, new[] { "Admin", "Doctor", "User" });

            var users = await EnsureUsersAsync(userManager);

            await SeedDoctorLicensesAsync(licensingContext, hasher);
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

        private static async Task SeedDoctorLicensesAsync(DoctorLicensingDbContext ctx, IDeterministicHasher hasher)
        {
            var existing = await ctx.DoctorLicenses.ToListAsync();

            // Upgrade legacy plaintext records in place
            var upgraded = false;
            foreach (var license in existing)
            {
                if (IsLegacyValue(license.LicenseNumber))
                {
                    license.LicenseNumber = hasher.Hash(license.LicenseNumber);
                    upgraded = true;
                }

                if (IsLegacyValue(license.NationalId))
                {
                    license.NationalId = hasher.Hash(license.NationalId);
                    upgraded = true;
                }
            }

            if (upgraded)
            {
                await ctx.SaveChangesAsync();
            }

            var existingLicenses = existing.Select(l => l.LicenseNumber).ToHashSet();

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
                new { LicenseNumber = "1000000001", FullName = "Aleksandar Dimitrov", NationalId = "8001012345", Specialties = new[]{ "Psychiatry", "Behavioral Therapy" } },
                new { LicenseNumber = "1000000002", FullName = "Borislava Ivanova", NationalId = "8205123456", Specialties = new[]{ "Clinical Psychology", "Counseling" } },
                new { LicenseNumber = "1000000003", FullName = "Viktor Petrov", NationalId = "7909234567", Specialties = new[]{ "Counseling", "Behavioral Therapy" } },
                new { LicenseNumber = "1000000004", FullName = "Desislava Georgieva", NationalId = "8507045678", Specialties = new[]{ "Neurology", "Family Medicine" } },
                new { LicenseNumber = "1000000005", FullName = "Emil Nikolov", NationalId = "8803156789", Specialties = new[]{ "Family Medicine", "Geriatrics" } },
                new { LicenseNumber = "1000000006", FullName = "Gabriela Stoyanova", NationalId = "9008267890", Specialties = new[]{ "Pediatrics", "Child Psychology" } },
                new { LicenseNumber = "1000000007", FullName = "Hristo Kolev", NationalId = "7701078901", Specialties = new[]{ "Psychiatry", "Addiction Medicine" } },
                new { LicenseNumber = "1000000008", FullName = "Iva Marinova", NationalId = "8602189012", Specialties = new[]{ "Geriatrics", "Family Medicine" } },
                new { LicenseNumber = "1000000009", FullName = "Kalin Todorov", NationalId = "8403290123", Specialties = new[]{ "Addiction Medicine", "Behavioral Therapy" } },
                new { LicenseNumber = "1000000010", FullName = "Lyubomira Hristova", NationalId = "9104301234", Specialties = new[]{ "Child Psychology", "Pediatrics" } },

                new { LicenseNumber = "2000000031", FullName = "Petar Vasilev", NationalId = "7301011111", Specialties = new[]{ "Psychiatry", "Neurology" } },
                new { LicenseNumber = "2000000032", FullName = "Stanimira Koleva", NationalId = "7402022222", Specialties = new[]{ "Clinical Psychology", "Behavioral Therapy" } },
                new { LicenseNumber = "2000000033", FullName = "Miroslav Genov", NationalId = "7503033333", Specialties = new[]{ "Family Medicine", "Geriatrics" } },
                new { LicenseNumber = "2000000034", FullName = "Elena Bogdanova", NationalId = "7604044444", Specialties = new[]{ "Pediatrics", "Child Psychology" } },
                new { LicenseNumber = "2000000035", FullName = "Tihomir Slavev", NationalId = "7705055555", Specialties = new[]{ "Addiction Medicine", "Counseling" } },
                new { LicenseNumber = "2000000036", FullName = "Mariyana Kostova", NationalId = "7806066666", Specialties = new[]{ "Clinical Psychology", "Family Medicine" } },
                new { LicenseNumber = "2000000037", FullName = "Rosen Iliev", NationalId = "7907077777", Specialties = new[]{ "Neurology", "Behavioral Therapy" } },
                new { LicenseNumber = "2000000038", FullName = "Silvia Kirilova", NationalId = "8008088888", Specialties = new[]{ "Counseling", "Addiction Medicine" } },
                new { LicenseNumber = "2000000039", FullName = "Nikolay Berov", NationalId = "8109099999", Specialties = new[]{ "Family Medicine", "Behavioral Therapy" } },
                new { LicenseNumber = "2000000040", FullName = "Denitsa Popova", NationalId = "8201010001", Specialties = new[]{ "Clinical Psychology", "Pediatrics" } },

                new { LicenseNumber = "2000000041", FullName = "Boyan Trenchev", NationalId = "8302020002", Specialties = new[]{ "Psychiatry", "Addiction Medicine" } },
                new { LicenseNumber = "2000000042", FullName = "Kalina Borisova", NationalId = "8403030003", Specialties = new[]{ "Child Psychology", "Behavioral Therapy" } },
                new { LicenseNumber = "2000000043", FullName = "Ognyan Radev", NationalId = "8504040004", Specialties = new[]{ "Neurology", "Family Medicine" } },
                new { LicenseNumber = "2000000044", FullName = "Raya Staneva", NationalId = "8605050005", Specialties = new[]{ "Pediatrics", "Behavioral Therapy" } },
                new { LicenseNumber = "2000000045", FullName = "Plamen Ganchev", NationalId = "8706060006", Specialties = new[]{ "Counseling", "Clinical Psychology" } },
                new { LicenseNumber = "2000000046", FullName = "Vesela Daskalova", NationalId = "8807070007", Specialties = new[]{ "Geriatrics", "Family Medicine" } },
                new { LicenseNumber = "2000000047", FullName = "Georgi Mitov", NationalId = "8908080008", Specialties = new[]{ "Addiction Medicine", "Psychiatry" } },
                new { LicenseNumber = "2000000048", FullName = "Yoana Petrova", NationalId = "9009090009", Specialties = new[]{ "Clinical Psychology", "Child Psychology" } },
                new { LicenseNumber = "2000000049", FullName = "Simeon Apostolov", NationalId = "9101010010", Specialties = new[]{ "Neurology", "Behavioral Therapy" } },
                new { LicenseNumber = "2000000050", FullName = "Iveta Manolova", NationalId = "9202020011", Specialties = new[]{ "Family Medicine", "Pediatrics" } },

                new { LicenseNumber = "2000000051", FullName = "Kristian Zhelev", NationalId = "9303030012", Specialties = new[]{ "Counseling", "Addiction Medicine" } },
                new { LicenseNumber = "2000000052", FullName = "Albena Racheva", NationalId = "9404040013", Specialties = new[]{ "Clinical Psychology", "Behavioral Therapy" } },
                new { LicenseNumber = "2000000053", FullName = "Lyuben Kolev", NationalId = "9505050014", Specialties = new[]{ "Psychiatry", "Neurology" } },
                new { LicenseNumber = "2000000054", FullName = "Magdalena Encheva", NationalId = "9606060015", Specialties = new[]{ "Pediatrics", "Child Psychology" } },
                new { LicenseNumber = "2000000055", FullName = "Vanya Tsvetanova", NationalId = "9707070016", Specialties = new[]{ "Family Medicine", "Geriatrics" } },
                new { LicenseNumber = "2000000056", FullName = "Dimitar Naydenov", NationalId = "9808080017", Specialties = new[]{ "Neurology", "Counseling" } },
                new { LicenseNumber = "2000000057", FullName = "Stanislava Marin", NationalId = "9909090018", Specialties = new[]{ "Addiction Medicine", "Behavioral Therapy" } },
                new { LicenseNumber = "2000000058", FullName = "Todor Asenov", NationalId = "0101010019", Specialties = new[]{ "Psychiatry", "Clinical Psychology" } },
                new { LicenseNumber = "2000000059", FullName = "Ralitsa Videnova", NationalId = "0202020020", Specialties = new[]{ "Child Psychology", "Counseling" } },
                new { LicenseNumber = "2000000060", FullName = "Mila Georgieva", NationalId = "0303030021", Specialties = new[]{ "Pediatrics", "Behavioral Therapy" } }
            };

            foreach (var data in licenseData)
            {
                var hashedLicense = hasher.Hash(data.LicenseNumber);

                if (existingLicenses.Contains(hashedLicense))
                {
                    continue;
                }

                var license = new DoctorLicense
                {
                    LicenseNumber = hashedLicense,
                    FullName = data.FullName,
                    NationalId = hasher.Hash(data.NationalId),
                    IssuingAuthority = "Bulgarian Medical Association",
                    IssuedOn = DateTime.Now.AddYears(-5),
                    ExpiresOn = DateTime.Now.AddYears(5),
                    Status = "Active"
                };
                ctx.DoctorLicenses.Add(license);
                await ctx.SaveChangesAsync();

                foreach (var specialtyName in data.Specialties)
                {
                    if (specialtyMap.TryGetValue(specialtyName, out var specialtyId))
                    {
                        ctx.LicenceDoctorSpecialties.Add(new LicenceDoctorSpecialty
                        {
                            DoctorLicenseId = license.Id,
                            SpecialtyId = specialtyId
                        });
                    }
                }
            }

            await ctx.SaveChangesAsync();
        }

        private static bool IsLegacyValue(string value) => !string.IsNullOrWhiteSpace(value) && value.All(char.IsDigit);

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
