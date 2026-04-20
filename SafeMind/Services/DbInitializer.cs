using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SafeMind.Data;
using SafeMind.Data.Models;
using SafeMind.Data.Enums;

namespace SafeMind.Services
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var provider = scope.ServiceProvider;

            var mainContext = provider.GetRequiredService<SafeMindDbContext>();
            var hasher = provider.GetRequiredService<IDeterministicHasher>();
            var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = provider.GetRequiredService<UserManager<IdentityUser>>();

            await mainContext.Database.MigrateAsync();

            await EnsureRolesAsync(roleManager, new[] { "Admin", "Doctor", "User" });

            var users = await EnsureUsersAsync(userManager);

            await SeedDoctorLicensesAsync(mainContext, hasher);
            await SeedCoreLookupsAsync(mainContext);
            await SeedDoctorsAsync(mainContext, users);
            await SeedArticlesAsync(mainContext, users);
            await SeedGoalTemplatesAsync(mainContext);
            await SeedDemoUserDataAsync(mainContext, users);
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
                ("lyubomira.hristova@safemind.bg", "Admin123!", "Doctor"),
                ("aleksandar.dimitrov@safemind.bg", "Password123!", "Doctor"),
                ("borislava.ivanova@safemind.bg", "Password123!", "Doctor"),
                ("viktor.petrov@safemind.bg", "Password123!", "Doctor"),
                ("desislava.georgieva@safemind.bg", "Password123!", "Doctor"),
                ("emil.nikolov@safemind.bg", "Password123!", "Doctor"),
                ("gabriela.stoyanova@safemind.bg", "Password123!", "Doctor"),
                ("hristo.kolev@safemind.bg", "Password123!", "Doctor"),
                ("iva.marinova@safemind.bg", "Password123!", "Doctor"),
                ("kalin.todorov@safemind.bg", "Password123!", "Doctor"),
                ("alex@gmail.com", "Password1!", "User"),
            };


            foreach (var seed in seeds)
            {
                // Use ToUpperInvariant for normalized email
                var normalizedEmail = seed.Email.ToUpperInvariant();
                var usersWithEmail = await userManager.Users
                    .Where(u => u.NormalizedEmail == normalizedEmail)
                    .ToListAsync();
                IdentityUser user = null;
                if (usersWithEmail.Count > 0)
                {
                    // Use the first user found (if duplicates exist, just pick the first)
                    user = usersWithEmail[0];
                }
                else
                {
                    user = new IdentityUser { UserName = seed.Email, Email = seed.Email, EmailConfirmed = true };
                    await userManager.CreateAsync(user, seed.Password);
                }

                if (!user.EmailConfirmed)
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

        private static async Task SeedDoctorLicensesAsync(SafeMindDbContext ctx, IDeterministicHasher hasher)
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
                    "Psychiatry", "Clinical Psychology", "Counseling", "Cognitive Behavioral Therapy", "Psychotherapy", 
                    "Child & Adolescent Psychology", "Addiction Counseling", "Neuropsychology", "Trauma Therapy", "Family & Couples Therapy"
                };
                ctx.LicenceSpecialties.AddRange(specialties.Select(name => new LicenceSpecialty { Name = name }));
                await ctx.SaveChangesAsync();
            }

            var specialtyMap = await ctx.LicenceSpecialties.ToDictionaryAsync(s => s.Name, s => s.Id);

            var licenseData = new[]
            {
                new { LicenseNumber = "1000000001", FullName = "Aleksandar Dimitrov", NationalId = "8001012345", Specialties = new[]{ "Psychiatry", "Psychotherapy" } },
                new { LicenseNumber = "1000000002", FullName = "Borislava Ivanova", NationalId = "8205123456", Specialties = new[]{ "Clinical Psychology", "Cognitive Behavioral Therapy" } },
                new { LicenseNumber = "1000000003", FullName = "Viktor Petrov", NationalId = "7909234567", Specialties = new[]{ "Counseling", "Family & Couples Therapy" } },
                new { LicenseNumber = "1000000004", FullName = "Desislava Georgieva", NationalId = "8507045678", Specialties = new[]{ "Neuropsychology", "Trauma Therapy" } },
                new { LicenseNumber = "1000000005", FullName = "Emil Nikolov", NationalId = "8803156789", Specialties = new[]{ "Psychotherapy", "Counseling" } },
                new { LicenseNumber = "1000000006", FullName = "Gabriela Stoyanova", NationalId = "9008267890", Specialties = new[]{ "Child & Adolescent Psychology", "Family & Couples Therapy" } },
                new { LicenseNumber = "1000000007", FullName = "Hristo Kolev", NationalId = "7701078901", Specialties = new[]{ "Psychiatry", "Addiction Counseling" } },
                new { LicenseNumber = "1000000008", FullName = "Iva Marinova", NationalId = "8602189012", Specialties = new[]{ "Clinical Psychology", "Neuropsychology" } },
                new { LicenseNumber = "1000000009", FullName = "Kalin Todorov", NationalId = "8403290123", Specialties = new[]{ "Addiction Counseling", "Cognitive Behavioral Therapy" } },
                new { LicenseNumber = "1000000010", FullName = "Lyubomira Hristova", NationalId = "9104301234", Specialties = new[]{ "Child & Adolescent Psychology", "Trauma Therapy" } },

                new { LicenseNumber = "2000000031", FullName = "Petar Vasilev", NationalId = "7301011111", Specialties = new[]{ "Psychiatry", "Cognitive Behavioral Therapy" } },
                new { LicenseNumber = "2000000032", FullName = "Stanimira Koleva", NationalId = "7402022222", Specialties = new[]{ "Clinical Psychology", "Trauma Therapy" } },
                new { LicenseNumber = "2000000033", FullName = "Miroslav Genov", NationalId = "7503033333", Specialties = new[]{ "Psychotherapy", "Neuropsychology" } },
                new { LicenseNumber = "2000000034", FullName = "Elena Bogdanova", NationalId = "7604044444", Specialties = new[]{ "Child & Adolescent Psychology", "Family & Couples Therapy" } },
                new { LicenseNumber = "2000000035", FullName = "Tihomir Slavev", NationalId = "7705055555", Specialties = new[]{ "Addiction Counseling", "Counseling" } },
                new { LicenseNumber = "2000000036", FullName = "Mariyana Kostova", NationalId = "7806066666", Specialties = new[]{ "Clinical Psychology", "Psychotherapy" } },
                new { LicenseNumber = "2000000037", FullName = "Rosen Iliev", NationalId = "7907077777", Specialties = new[]{ "Cognitive Behavioral Therapy", "Trauma Therapy" } },
                new { LicenseNumber = "2000000038", FullName = "Silvia Kirilova", NationalId = "8008088888", Specialties = new[]{ "Counseling", "Addiction Counseling" } },
                new { LicenseNumber = "2000000039", FullName = "Nikolay Berov", NationalId = "8109099999", Specialties = new[]{ "Psychotherapy", "Trauma Therapy" } },
                new { LicenseNumber = "2000000040", FullName = "Denitsa Popova", NationalId = "8201010001", Specialties = new[]{ "Clinical Psychology", "Child & Adolescent Psychology" } },

                new { LicenseNumber = "2000000041", FullName = "Boyan Trenchev", NationalId = "8302020002", Specialties = new[]{ "Psychiatry", "Addiction Counseling" } },
                new { LicenseNumber = "2000000042", FullName = "Kalina Borisova", NationalId = "8403030003", Specialties = new[]{ "Family & Couples Therapy", "Trauma Therapy" } },
                new { LicenseNumber = "2000000043", FullName = "Ognyan Radev", NationalId = "8504040004", Specialties = new[]{ "Cognitive Behavioral Therapy", "Psychotherapy" } },
                new { LicenseNumber = "2000000044", FullName = "Raya Staneva", NationalId = "8605050005", Specialties = new[]{ "Child & Adolescent Psychology", "Trauma Therapy" } },
                new { LicenseNumber = "2000000045", FullName = "Plamen Ganchev", NationalId = "8706060006", Specialties = new[]{ "Counseling", "Clinical Psychology" } },
                new { LicenseNumber = "2000000046", FullName = "Vesela Daskalova", NationalId = "8807070007", Specialties = new[]{ "Neuropsychology", "Psychotherapy" } },
                new { LicenseNumber = "2000000047", FullName = "Georgi Mitov", NationalId = "8908080008", Specialties = new[]{ "Addiction Counseling", "Psychiatry" } },
                new { LicenseNumber = "2000000048", FullName = "Yoana Petrova", NationalId = "9009090009", Specialties = new[]{ "Clinical Psychology", "Family & Couples Therapy" } },
                new { LicenseNumber = "2000000049", FullName = "Simeon Apostolov", NationalId = "9101010010", Specialties = new[]{ "Cognitive Behavioral Therapy", "Trauma Therapy" } },
                new { LicenseNumber = "2000000050", FullName = "Iveta Manolova", NationalId = "9202020011", Specialties = new[]{ "Psychotherapy", "Child & Adolescent Psychology" } },

                new { LicenseNumber = "2000000051", FullName = "Kristian Zhelev", NationalId = "9303030012", Specialties = new[]{ "Counseling", "Addiction Counseling" } },
                new { LicenseNumber = "2000000052", FullName = "Albena Racheva", NationalId = "9404040013", Specialties = new[]{ "Clinical Psychology", "Trauma Therapy" } },
                new { LicenseNumber = "2000000053", FullName = "Lyuben Kolev", NationalId = "9505050014", Specialties = new[]{ "Psychiatry", "Cognitive Behavioral Therapy" } },
                new { LicenseNumber = "2000000054", FullName = "Magdalena Encheva", NationalId = "9606060015", Specialties = new[]{ "Child & Adolescent Psychology", "Family & Couples Therapy" } },
                new { LicenseNumber = "2000000055", FullName = "Vanya Tsvetanova", NationalId = "9707070016", Specialties = new[]{ "Psychotherapy", "Neuropsychology" } },
                new { LicenseNumber = "2000000056", FullName = "Dimitar Naydenov", NationalId = "9808080017", Specialties = new[]{ "Cognitive Behavioral Therapy", "Counseling" } },
                new { LicenseNumber = "2000000057", FullName = "Stanislava Marin", NationalId = "9909090018", Specialties = new[]{ "Addiction Counseling", "Trauma Therapy" } },
                new { LicenseNumber = "2000000058", FullName = "Todor Asenov", NationalId = "0101010019", Specialties = new[]{ "Psychiatry", "Clinical Psychology" } },
                new { LicenseNumber = "2000000059", FullName = "Ralitsa Videnova", NationalId = "0202020020", Specialties = new[]{ "Family & Couples Therapy", "Counseling" } },
                new { LicenseNumber = "2000000060", FullName = "Mila Georgieva", NationalId = "0303030021", Specialties = new[]{ "Child & Adolescent Psychology", "Trauma Therapy" } }
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
                    "Psychiatry", "Clinical Psychology", "Counseling", "Cognitive Behavioral Therapy", "Psychotherapy", "Child & Adolescent Psychology", "Addiction Counseling", "Neuropsychology", "Trauma Therapy", "Family & Couples Therapy"
                };
                ctx.Specialties.AddRange(specialties.Select((name, i) => new Specialty { Name = name }));
                await ctx.SaveChangesAsync();
            }

            if (!await ctx.Languages.AnyAsync())
            {
                var languages = new[] { "Bulgarian", "English", "Russian", "German", "French", "Turkish", "Greek", "Romanian", "Spanish", "Italian" };
                ctx.Languages.AddRange(languages.Select(l => new Language { Name = l }));
                await ctx.SaveChangesAsync();
            }

            if (!await ctx.Categories.AnyAsync())
            {
                var categories = new[] { "Mind", "Wellness", "Sleep", "Therapy", "Insights", "Stress", "Anxiety" };
                ctx.Categories.AddRange(categories.Select(c => new Category { Name = c }));
                await ctx.SaveChangesAsync();
            }
            else
            {
                // Ensure new categories exist even on existing databases
                var existing = await ctx.Categories.Select(c => c.Name).ToListAsync();
                var toAdd = new[] { "Stress", "Anxiety" }.Where(c => !existing.Contains(c)).ToArray();
                if (toAdd.Length > 0)
                {
                    ctx.Categories.AddRange(toAdd.Select(c => new Category { Name = c }));
                    await ctx.SaveChangesAsync();
                }
            }
        }

        private static async Task SeedDoctorsAsync(SafeMindDbContext ctx, Dictionary<string, IdentityUser> users)
        {
            if (await ctx.Doctors.AnyAsync()) return;

            var specialtyMap = await ctx.Specialties.ToDictionaryAsync(s => s.Name, s => s.Id);
            var languageMap = await ctx.Languages.ToDictionaryAsync(l => l.Name, l => l.Id);

            var doctorSeeds = new[]
            {
                new { Email = "aleksandar.dimitrov@safemind.bg", Name = "Aleksandar Dimitrov", License = "1000000001", Specialty = "Psychiatry", Languages = new[]{"Bulgarian","English","Russian"}, WorkStart = new TimeOnly(8,0), WorkEnd = new TimeOnly(16,0), Duration = 50, Price = 120m, Biography = "Psychiatrist focused on mood and anxiety disorders, combining medication management with CBT principles, sleep and lifestyle coaching, and measurement-based care. He designs phased care plans with clear milestones, coordinates with primary care when needed, and offers short, structured check-ins to keep gains stable. Fluent in Bulgarian, English, and Russian, he works with adults who want practical steps, transparent medication decisions, and a steady partner through recovery." },
                new { Email = "borislava.ivanova@safemind.bg", Name = "Borislava Ivanova", License = "1000000002", Specialty = "Clinical Psychology", Languages = new[]{"Bulgarian","English"}, WorkStart = new TimeOnly(9,0), WorkEnd = new TimeOnly(17,0), Duration = 60, Price = 95m, Biography = "Clinical psychologist helping adults navigate stress, burnout, perfectionism, and relationship strain. She uses CBT, ACT, and compassion-based work to reduce rumination, build emotion regulation, and restore boundaries. Sessions include practical experiments between visits, concise progress tracking, and skills you can apply the same week. Borislava tailors pacing to each client and collaborates closely with medical teams when somatic factors matter." },
                new { Email = "viktor.petrov@safemind.bg", Name = "Viktor Petrov", License = "1000000003", Specialty = "Counseling", Languages = new[]{"Bulgarian","English","German"}, WorkStart = new TimeOnly(10,0), WorkEnd = new TimeOnly(18,0), Duration = 45, Price = 75m, Biography = "Counselor supporting young professionals through life transitions, relocation stress, and career pivots. Viktor blends solution-focused counseling with strengths-based coaching, helping clients clarify priorities, set realistic habits, and communicate confidently at work and home. Sessions emphasize actionable steps, short feedback loops, and tools for managing pressure without losing momentum. He works in Bulgarian, English, and German and keeps plans concise and measurable." },
                new { Email = "desislava.georgieva@safemind.bg", Name = "Desislava Georgieva", License = "1000000004", Specialty = "Neuropsychology", Languages = new[]{"Bulgarian","English","French"}, WorkStart = new TimeOnly(7,30), WorkEnd = new TimeOnly(15,30), Duration = 50, Price = 130m, Biography = "Neuropsychologist specializing in trauma recovery and the connection between brain function and emotional well-being. Desislava uses evidence-based assessments to understand cognitive patterns, then builds targeted intervention plans combining EMDR, somatic experiencing, and cognitive rehabilitation. She helps clients process traumatic experiences while strengthening attention, memory, and executive function. Her approach is structured and educational, with clear progress markers." },
                new { Email = "emil.nikolov@safemind.bg", Name = "Emil Nikolov", License = "1000000005", Specialty = "Psychotherapy", Languages = new[]{"Bulgarian","English","Russian"}, WorkStart = new TimeOnly(8,30), WorkEnd = new TimeOnly(16,30), Duration = 55, Price = 80m, Biography = "Psychotherapist working with adults navigating anxiety, depression, and relationship difficulties. Emil integrates psychodynamic and humanistic approaches to help clients uncover recurring patterns, develop self-awareness, and build healthier coping strategies. He emphasizes the therapeutic relationship as a foundation for change and creates a safe, nonjudgmental space for exploration. Fluent in Bulgarian, English, and Russian, he supports clients through major life transitions." },
                new { Email = "gabriela.stoyanova@safemind.bg", Name = "Gabriela Stoyanova", License = "1000000006", Specialty = "Child & Adolescent Psychology", Languages = new[]{"Bulgarian","English"}, WorkStart = new TimeOnly(11,0), WorkEnd = new TimeOnly(19,0), Duration = 50, Price = 85m, Biography = "Child and adolescent psychologist using play-based therapy, art therapy, and parent coaching to support emotional development in children aged four to seventeen. Gabriela helps young clients with anxiety, behavioral challenges, ADHD, and school-related stress. She works closely with caregivers and teachers, creating structured home routines and individualized strategies for attention, social skills, and emotional regulation." },
                new { Email = "hristo.kolev@safemind.bg", Name = "Hristo Kolev", License = "1000000007", Specialty = "Psychiatry", Languages = new[]{"Bulgarian","English"}, WorkStart = new TimeOnly(8,0), WorkEnd = new TimeOnly(14,0), Duration = 40, Price = 95m, Biography = "Psychiatrist focusing on depression, trauma recovery, and sleep-related issues. Hristo offers careful medication oversight, teaches grounding and pacing strategies, and works in short, focused sessions that balance symptom relief with resilience-building. He coordinates with therapists for integrated care and helps patients make informed, low-drama medication decisions." },
                new { Email = "iva.marinova@safemind.bg", Name = "Iva Marinova", License = "1000000008", Specialty = "Clinical Psychology", Languages = new[]{"Bulgarian","English","French"}, WorkStart = new TimeOnly(9,30), WorkEnd = new TimeOnly(17,30), Duration = 45, Price = 100m, Biography = "Clinical psychologist with a neuropsychology focus, supporting adults experiencing cognitive concerns, chronic stress, and life transitions. Iva conducts thorough cognitive assessments and designs personalized intervention plans that combine cognitive training, mindfulness, and lifestyle adjustments. She collaborates with psychiatrists and other specialists to ensure integrated care and helps clients maintain independence, clarity, and emotional balance." },
                new { Email = "kalin.todorov@safemind.bg", Name = "Kalin Todorov", License = "1000000009", Specialty = "Addiction Counseling", Languages = new[]{"Bulgarian","English"}, WorkStart = new TimeOnly(12,0), WorkEnd = new TimeOnly(20,0), Duration = 60, Price = 115m, Biography = "Addiction counselor experienced in relapse prevention, motivational interviewing, and cognitive behavioral approaches to substance use and behavioral addictions. Kalin builds transparent recovery plans with craving management tools, routine accountability check-ins, and clear progress markers. He works collaboratively with clients to stabilize work, relationships, and daily routines, balancing structure with compassionate support." },
                new { Email = "lyubomira.hristova@safemind.bg", Name = "Lyubomira Hristova", License = "1000000010", Specialty = "Child & Adolescent Psychology", Languages = new[]{"Bulgarian","English"}, WorkStart = new TimeOnly(6,0), WorkEnd = new TimeOnly(14,0), Duration = 50, Price = 90m, Biography = "Child psychologist using play-based approaches and parent coaching to build emotional regulation and school readiness. Lyubomira works closely with caregivers and teachers, creates simple home routines, and tailors strategies for attention, anxiety, and social skills. She keeps feedback concrete so families know exactly what to practice between visits." }
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
                    Price = d.Price,
                    Rating = 0
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

        private static async Task SeedArticlesAsync(SafeMindDbContext ctx, Dictionary<string, IdentityUser> users)
        {
            var existingHeadlines = await ctx.Articles.Select(a => a.Headline).ToListAsync();
            var existingSet = new HashSet<string>(existingHeadlines);

            var categoryMap = await ctx.Categories.ToDictionaryAsync(c => c.Name, c => c.Id);

            // Use the admin user as the author for all seeded articles
            var author = users.Values.FirstOrDefault();
            if (author == null) return;

            var articleSeeds = new[]
            {
                new { Headline = "Quick Breathing Reset", Content = "Feeling overwhelmed? Try box breathing: inhale for four counts, hold for four, exhale for four, hold for four. Repeat three to five cycles. This simple technique activates your parasympathetic nervous system, slowing your heart rate and calming racing thoughts. You can do it at your desk, on the bus, or before a difficult conversation. Over time, regular practice rewires your stress response so you recover faster from everyday pressure. Pair it with a brief body scan — notice tension in your shoulders, jaw, and hands, then consciously release each area. Even thirty seconds of focused breathing can shift your entire afternoon.", Categories = new[] { "Mind", "Wellness" }, Views = 1420, WeeklyViews = 210, Likes = 87, ImagePath = "https://images.unsplash.com/photo-1506126613408-eca07ce68773?w=600&h=450&fit=crop" },
                new { Headline = "Why Night Routines Matter", Content = "A consistent wind-down routine tells your brain it is time to transition from alertness to rest. Start dimming lights an hour before bed, put screens away, and choose a calming activity like reading, gentle stretching, or journaling. Keep your bedroom cool and dark. Avoid caffeine after midday and heavy meals close to bedtime. Research shows that people who follow a predictable pre-sleep ritual fall asleep faster and experience deeper, more restorative sleep stages. Quality sleep strengthens memory consolidation, emotional regulation, and immune function. Small changes compound — even adding one relaxing step tonight can improve how you feel tomorrow morning.", Categories = new[] { "Sleep" }, Views = 980, WeeklyViews = 145, Likes = 63, ImagePath = "https://images.unsplash.com/photo-1511295742362-92c96b1cf484?w=600&h=450&fit=crop" },
                new { Headline = "Finding Peace in Nature Walks", Content = "Time in green spaces lowers cortisol, reduces rumination, and lifts mood. You do not need a mountain hike — a twenty-minute walk through a local park counts. Pay attention to textures underfoot, birdsong, the feel of wind on your skin. This informal mindfulness practice grounds you in the present moment and breaks the cycle of anxious thinking. Studies show that even viewing nature photos provides a small mood boost, but being physically immersed is significantly more effective. Try scheduling a short outdoor walk after lunch three times a week and notice how your afternoon focus improves.", Categories = new[] { "Wellness", "Mind" }, Views = 860, WeeklyViews = 130, Likes = 72, ImagePath = "https://images.unsplash.com/photo-1545389336-cf090694435e?w=600&h=450&fit=crop" },
                new { Headline = "Breathing Exercises for Beginners", Content = "If you are new to breathwork, start with the simplest pattern: four seconds in through the nose, six seconds out through the mouth. The longer exhale is the key — it stimulates the vagus nerve and signals safety to your body. Practice for just two minutes at first, ideally at the same time each day so it becomes habitual. Once comfortable, explore 4-7-8 breathing or alternate-nostril breathing. Keep a small log of how you feel before and after each session to build motivation. Many people report noticeable stress reduction within the first week of daily practice.", Categories = new[] { "Therapy", "Mind" }, Views = 750, WeeklyViews = 98, Likes = 55, ImagePath = "https://images.unsplash.com/photo-1544367567-0f2fcb009e0b?w=600&h=450&fit=crop" },
                new { Headline = "The Power of Daily Journaling", Content = "Writing even three sentences about your day creates distance between you and your emotions, making them easier to process. Journaling reduces intrusive thoughts, clarifies priorities, and helps you spot patterns in mood or behaviour over time. You do not need fancy prompts — simply describe what happened, how you felt, and one thing you are grateful for. Keep a notebook by your bed or use a notes app on your phone. Consistency matters more than length. Research links expressive writing to improved immune function, lower anxiety, and better sleep. Start tonight and see what surfaces.", Categories = new[] { "Insights", "Mind" }, Views = 1120, WeeklyViews = 175, Likes = 91, ImagePath = "https://images.unsplash.com/photo-1499209974431-9dddcece7f88?w=600&h=450&fit=crop" },
                new { Headline = "Morning Rituals That Set Your Mood", Content = "How you spend your first hour shapes the rest of the day. Instead of reaching for your phone, try a screen-free start: hydrate, stretch for five minutes, and set one clear intention. Exposure to natural light within thirty minutes of waking helps regulate your circadian rhythm and boosts serotonin. A short gratitude list or two minutes of breathing grounds your mindset before external demands arrive. These small anchors build resilience — when stressful moments hit later, you have a calm baseline to return to. Experiment for one week and compare your energy levels before and after.", Categories = new[] { "Wellness" }, Views = 640, WeeklyViews = 88, Likes = 48, ImagePath = "https://images.unsplash.com/photo-1528715471579-d1bcf0ba5e83?w=600&h=450&fit=crop" },
                new { Headline = "What Therapists Wish You Knew", Content = "Therapy works best when you arrive with honesty, not rehearsed answers. Therapists are not there to judge — they are trained to sit with discomfort alongside you. It is okay to not know what to say; silence is useful too. Progress is rarely linear, and a 'bad' session often precedes a breakthrough. Share feedback about what helps and what does not — your therapist wants to adjust. Between sessions, small experiments matter more than big revelations. And remember: seeking help is not a sign of weakness, it is an investment in the quality of your entire life.", Categories = new[] { "Therapy", "Insights" }, Views = 1350, WeeklyViews = 200, Likes = 105, ImagePath = "https://images.unsplash.com/photo-1573497620053-ea5300f94f21?w=600&h=450&fit=crop" },
                new { Headline = "Understanding Sleep Cycles", Content = "Your body cycles through light sleep, deep sleep, and REM roughly every ninety minutes. Deep sleep repairs tissue and strengthens immunity, while REM consolidates memories and processes emotions. Waking mid-cycle leaves you groggy; timing your alarm to land at the end of a cycle helps you feel refreshed. Most adults need four to six full cycles per night, which translates to about seven and a half to nine hours. Alcohol and screens before bed suppress REM, robbing you of emotional processing time. Tracking your sleep patterns for a week reveals where simple adjustments can radically improve how you feel.", Categories = new[] { "Sleep", "Insights" }, Views = 890, WeeklyViews = 115, Likes = 67, ImagePath = "https://images.unsplash.com/photo-1495197359483-d092478c170a?w=600&h=450&fit=crop" },
                new { Headline = "Building Emotional Resilience", Content = "Resilience is not about avoiding difficulty — it is about recovering effectively when difficulty arrives. Three evidence-based pillars support it: strong social connections, a sense of purpose, and flexible thinking. Practice reframing setbacks as data rather than failure. Maintain at least two or three relationships where you can be vulnerable. Invest in activities that give your life meaning, even small ones like cooking a good meal or mentoring a colleague. Physical health underpins mental toughness, so protect your sleep, movement, and nutrition. Resilience is a skill you build, not a trait you are born with.", Categories = new[] { "Mind", "Wellness" }, Views = 1050, WeeklyViews = 160, Likes = 82, ImagePath = "https://images.unsplash.com/photo-1508672019048-805c876b67e2?w=600&h=450&fit=crop" },
                new { Headline = "Your Weekly Grounding Check-In", Content = "Set aside ten minutes each Sunday to review your week. Ask yourself: What drained me? What energised me? Did I move my body enough? Did I connect with someone I care about? Rate your overall mood on a simple one-to-ten scale and jot down one thing you want to do differently next week. This micro-reflection habit catches downward trends early, before they snowball into burnout or low mood. Over a month you will see clear patterns — maybe Tuesdays are consistently hard, or exercise days correlate with better sleep. Data about yourself is the first step to meaningful change.", Categories = new[] { "Insights" }, Views = 720, WeeklyViews = 105, Likes = 58, ImagePath = "https://images.unsplash.com/photo-1515894203077-9cd36032142f?w=600&h=450&fit=crop" },
                new { Headline = "Stress Is Not the Enemy — Your Response Is", Content = "Stress itself is a natural survival mechanism. The problem starts when it becomes chronic and we lose the ability to return to baseline. Understanding the difference between acute stress, which sharpens focus, and chronic stress, which erodes health, is the first step toward managing it. Common signs of chronic stress include persistent fatigue, irritability, muscle tension, and difficulty concentrating. Start by identifying your top three stressors and rating each from one to ten. Then ask: which of these can I influence, and which do I need to accept? For controllable stressors, break the problem into the smallest possible next step. For the rest, practice radical acceptance — acknowledging reality without wasting energy fighting what you cannot change. Pair this with daily nervous system resets like cold water on your wrists, a two-minute walk, or progressive muscle relaxation. Over weeks you will notice your recovery window shrinking and your capacity growing.", Categories = new[] { "Stress", "Mind" }, Views = 930, WeeklyViews = 140, Likes = 74, ImagePath = "https://images.unsplash.com/photo-1474418397713-7ede21d49118?w=600&h=450&fit=crop" },
                new { Headline = "Five-Minute Stress Relief You Can Do Anywhere", Content = "When stress hits during a meeting, commute, or school run, you need tools that work in under five minutes with no equipment. Start with physiological sighing: take two short inhales through your nose followed by one long exhale through your mouth. Research from Stanford shows this is the fastest way to calm the autonomic nervous system. Next, try the five-four-three-two-one grounding technique — name five things you see, four you can touch, three you hear, two you smell, and one you taste. This pulls your attention out of the stress loop and into the present. Finally, shake it out: animals literally shake after a threat passes to discharge adrenaline, and you can too. Stand up, shake your hands and arms loosely for thirty seconds, roll your shoulders, and take one slow breath. These three micro-tools, practiced consistently, become your personal fire extinguisher for daily stress. Keep a reminder on your phone until they become second nature.", Categories = new[] { "Stress", "Wellness" }, Views = 1180, WeeklyViews = 195, Likes = 96, ImagePath = "https://images.unsplash.com/photo-1499728603263-13726abce5fd?w=600&h=450&fit=crop" },
                new { Headline = "Understanding Anxiety: When Worry Takes Over", Content = "Anxiety is the mind treating a possible future threat as though it were happening right now. In small doses it keeps you prepared, but when it becomes constant it hijacks your attention, disrupts sleep, and drains your energy. Common types include generalised anxiety, where worry floats freely from topic to topic, social anxiety, which centres on judgment by others, and panic disorder, which produces sudden intense physical symptoms. The first step is learning to recognise anxiety as a signal, not a fact. Just because your body floods with adrenaline does not mean you are in danger. Practice labelling the experience: say to yourself I am noticing anxiety rather than I am anxious. This small linguistic shift activates the prefrontal cortex and reduces amygdala reactivity. Combine labelling with slow diaphragmatic breathing and you create a pause between the trigger and your response. If anxiety consistently interferes with work, relationships, or sleep, professional support through therapy or medication can make a significant difference. You do not have to manage it alone.", Categories = new[] { "Anxiety", "Therapy" }, Views = 1260, WeeklyViews = 185, Likes = 102, ImagePath = "https://images.unsplash.com/photo-1541199249251-f713e6145474?w=600&h=450&fit=crop" },
                new { Headline = "Breaking the Anxiety Cycle: Practical Steps", Content = "Anxiety feeds on avoidance. The more you dodge situations that make you uncomfortable, the louder the anxiety becomes next time. Breaking the cycle requires graduated exposure — facing feared situations in small, manageable doses while your nervous system learns that the threat is survivable. Start by listing situations you avoid, ranked from mildly uncomfortable to very distressing. Begin with the easiest one and stay in the situation until your anxiety naturally drops, which it always does given enough time. This process, called habituation, teaches your brain that the alarm was a false one. Between exposures, maintain a worry journal: write the anxious thought, rate its intensity, note what actually happened, and compare. Over weeks you will see a clear gap between prediction and reality. Support your progress with regular sleep, reduced caffeine, and daily movement — all of which lower your baseline anxiety level. Celebrate each step forward, no matter how small, because courage is not the absence of fear but action in its presence.", Categories = new[] { "Anxiety", "Insights" }, Views = 1050, WeeklyViews = 165, Likes = 88, ImagePath = "https://images.unsplash.com/photo-1493836512294-502baa1986e2?w=600&h=450&fit=crop" },
                new { Headline = "Recognizing Burnout Before It Takes Over", Content = "Burnout is not the same as being tired. It is a state of chronic emotional, physical, and mental exhaustion caused by prolonged stress, usually at work. The three hallmarks are emotional exhaustion, where you feel drained and unable to cope; depersonalization, where you become cynical or detached from your responsibilities; and reduced personal accomplishment, where nothing you do feels meaningful. Burnout develops gradually, which makes it easy to dismiss early warning signs like irritability, trouble sleeping, or losing interest in things you used to enjoy. Recovery starts with honest assessment: are you pushing through because you have to, or because you have forgotten how to stop? Reduce where you can, delegate what is possible, and reclaim at least one activity each week that is purely for enjoyment. Talk to a therapist or counselor if you feel stuck — burnout responds well to structured support. Prevention is always easier than recovery, so build regular check-ins with yourself into your routine.", Categories = new[] { "Stress", "Wellness" }, Views = 870, WeeklyViews = 125, Likes = 69, ImagePath = "https://images.unsplash.com/photo-1504439468489-c8920d796a29?w=600&h=450&fit=crop" },
                new { Headline = "How Social Connection Protects Mental Health", Content = "Humans are wired for connection. Loneliness activates the same brain regions as physical pain, and chronic social isolation is linked to higher rates of depression, anxiety, and cognitive decline. You do not need a large social circle — two or three relationships where you feel genuinely seen and heard are enough. Quality matters far more than quantity. Start small: send a message to someone you have not spoken to in a while, accept an invitation you would normally decline, or simply make eye contact and exchange a few words with a neighbor. If social anxiety makes connection difficult, consider joining a structured group activity like a class or volunteer project where interaction happens naturally around a shared task. Online communities can supplement in-person contact but should not replace it entirely. Prioritize at least one meaningful social interaction each week and notice how it affects your mood over time.", Categories = new[] { "Mind", "Wellness" }, Views = 760, WeeklyViews = 110, Likes = 61, ImagePath = "https://images.unsplash.com/photo-1529156069898-49953e39b3ac?w=600&h=450&fit=crop" },
                new { Headline = "Self-Compassion: Treating Yourself Like a Friend", Content = "Most people are far harder on themselves than they would ever be on a friend facing the same struggle. Self-compassion is not self-indulgence — it is meeting your own pain with the same kindness you would offer someone you care about. Research by Kristin Neff identifies three components: self-kindness instead of self-judgment, common humanity instead of isolation, and mindfulness instead of over-identification with negative thoughts. When you catch yourself in a harsh inner dialogue, pause and ask what you would say to a friend in this situation. Place a hand on your chest if it helps ground you. Acknowledge that suffering is part of the shared human experience, not a personal failing. Studies consistently show that self-compassion reduces anxiety and depression, increases motivation, and improves relationship satisfaction. It is a skill that strengthens with practice, not a personality trait you either have or lack.", Categories = new[] { "Mind", "Therapy" }, Views = 940, WeeklyViews = 138, Likes = 78, ImagePath = "https://images.unsplash.com/photo-1516534775068-ba3e7458af70?w=600&h=450&fit=crop" },
                new { Headline = "Mindfulness for People Who Cannot Sit Still", Content = "Mindfulness does not require a meditation cushion, incense, or twenty minutes of silence. If sitting still feels impossible, try moving mindfulness instead. Walk slowly and pay attention to the sensation of each foot touching the ground. Wash dishes and notice the temperature of the water, the weight of each plate. Eat a meal without your phone and focus on textures and flavors. The essence of mindfulness is simply paying attention to the present moment without judgment. Even brushing your teeth can become a mindful practice if you notice the taste of the toothpaste, the motion of the brush, and the sensation of clean teeth afterward. Start with one routine activity per day and practice full attention during that activity alone. Over a few weeks, you will find it easier to bring this quality of attention to other moments, including stressful ones. Mindfulness is not about emptying your mind — it is about noticing what is already there.", Categories = new[] { "Wellness", "Insights" }, Views = 1090, WeeklyViews = 158, Likes = 84, ImagePath = "https://images.unsplash.com/photo-1507120878965-54b2d3939100?w=600&h=450&fit=crop" },
                new { Headline = "When Depression Feels Like Nothing at All", Content = "Depression is often misunderstood as constant sadness, but for many people it feels more like numbness — a flat, grey absence of feeling where motivation, interest, and energy simply disappear. You might go through the motions of your day without registering any of it. Tasks that used to be easy feel impossibly heavy. You may withdraw from people not because you dislike them but because you cannot summon the energy to engage. If this sounds familiar, know that you are not lazy, broken, or making it up. Depression is a medical condition that affects brain chemistry, sleep, appetite, and cognition. Small steps matter: getting out of bed, showering, eating something, stepping outside for even five minutes. Tell one person how you are feeling. Contact a mental health professional — therapy, medication, or a combination can make a real difference. Recovery is possible, and you do not have to figure it out alone.", Categories = new[] { "Mind", "Therapy" }, Views = 1310, WeeklyViews = 192, Likes = 107, ImagePath = "https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=600&h=450&fit=crop" },
                new { Headline = "Setting Boundaries Without Guilt", Content = "Saying no is one of the most important skills for mental health, and one of the hardest to practice. Boundaries are not walls — they are guidelines that protect your energy, time, and emotional well-being. Without them, resentment builds, stress escalates, and relationships suffer. Start by identifying where you feel most drained: is it a coworker who always adds to your plate, a family member who dismisses your feelings, or a social obligation you dread? Practice saying no to low-stakes requests first to build the muscle. Use clear, simple language: I cannot take that on right now, or I need some time to myself this evening. You do not owe a lengthy explanation. Guilt is normal at first, especially if you are used to people-pleasing, but it fades as you experience the relief that comes with protecting your space. Healthy boundaries actually strengthen relationships by replacing silent resentment with honest communication.", Categories = new[] { "Wellness", "Insights" }, Views = 990, WeeklyViews = 147, Likes = 79, ImagePath = "https://images.unsplash.com/photo-1517486808906-6ca8b3f04846?w=600&h=450&fit=crop" },
                new { Headline = "The Science of Gratitude and Why It Works", Content = "Gratitude is more than a feel-good exercise — it has measurable effects on the brain. Regularly noticing what you appreciate activates the prefrontal cortex and releases dopamine and serotonin, the same neurotransmitters targeted by many antidepressants. A landmark study by Emmons and McCullough found that people who wrote down three things they were grateful for each week reported higher well-being, more optimism, and fewer physical complaints after just ten weeks. The practice works because it trains your brain to scan for positives rather than defaulting to threat detection. You do not need to feel grateful for everything or ignore real problems. Start small and specific: the warmth of your morning coffee, a colleague who helped you, ten minutes of quiet before the day began. Write it down — the act of writing deepens the neural impact. Over time, gratitude shifts your baseline mood upward without requiring any external change in circumstances.", Categories = new[] { "Insights", "Mind" }, Views = 830, WeeklyViews = 118, Likes = 66, ImagePath = "https://images.unsplash.com/photo-1506784365847-bbad939e9335?w=600&h=450&fit=crop" },
                new { Headline = "How to Support Someone Who Is Struggling", Content = "When someone you care about is going through a difficult time, the instinct is often to fix, advise, or cheer them up. But what most people need first is to feel heard. Start by simply being present: I am here and I am listening. Avoid phrases like just think positive or it could be worse, which minimize their experience even when well-intentioned. Ask open-ended questions like how are you really doing or what would be most helpful right now. Respect their pace — do not push them to talk before they are ready, but do check in again later so they know you have not forgotten. If you are worried about their safety, ask directly: are you having thoughts of hurting yourself? Research shows that asking does not plant the idea — it opens a door. Encourage professional support without making it an ultimatum. And take care of yourself too — supporting someone in distress is emotionally demanding, and you cannot pour from an empty cup.", Categories = new[] { "Therapy", "Mind" }, Views = 1150, WeeklyViews = 170, Likes = 93, ImagePath = "https://images.unsplash.com/photo-1516302752625-fcc3c50ae61f?w=600&h=450&fit=crop" },
                new { Headline = "Digital Detox: Reclaiming Your Attention", Content = "The average person checks their phone over ninety times a day. Each notification triggers a small dopamine hit, training your brain to crave constant stimulation and making sustained focus increasingly difficult. Social media compounds the problem by inviting comparison, which research links directly to increased anxiety and lower self-esteem. A full digital detox is not realistic for most people, but structured boundaries make a significant difference. Try these: no screens for the first and last thirty minutes of your day, turn off non-essential notifications, and designate one meal per day as phone-free. Use screen time tracking to build awareness — most people are surprised by their actual usage. Replace scrolling with a brief alternative like reading a page, stretching, or looking out a window. The goal is not to eliminate technology but to use it intentionally rather than reflexively. After even a week of small changes, many people report better sleep, improved mood, and a surprising sense of calm.", Categories = new[] { "Wellness", "Stress" }, Views = 1070, WeeklyViews = 155, Likes = 85, ImagePath = "https://images.unsplash.com/photo-1512941937669-90a1b58e7e9c?w=600&h=450&fit=crop" },
                new { Headline = "Understanding Panic Attacks and What to Do", Content = "A panic attack is a sudden surge of intense fear that peaks within minutes. Symptoms include a racing heart, shortness of breath, chest tightness, dizziness, tingling, and a terrifying feeling that you are dying or losing control. Although deeply frightening, panic attacks are not dangerous — your body is activating its fight-or-flight response in the absence of real danger. The single most important thing to remember during a panic attack is that it will pass. Ground yourself by placing both feet flat on the floor and focusing on slow exhales — breathe out for longer than you breathe in. Name five things you can see around you to anchor your attention in the present. Avoid fighting the sensations or telling yourself to calm down, which can increase the panic. After it passes, drink water, move gently, and be compassionate with yourself. If panic attacks become frequent, cognitive behavioral therapy is highly effective at reducing their intensity and frequency.", Categories = new[] { "Anxiety", "Therapy" }, Views = 1380, WeeklyViews = 205, Likes = 112, ImagePath = "https://images.unsplash.com/photo-1474631245212-32dc3c8310c6?w=600&h=450&fit=crop" },
                new { Headline = "Sleep and Mental Health: The Two-Way Street", Content = "Poor sleep worsens anxiety and depression, and anxiety and depression disrupt sleep — creating a cycle that is hard to break without deliberate effort. During sleep, your brain processes emotional memories, clears metabolic waste, and consolidates learning. When you consistently get fewer than seven hours, your amygdala becomes more reactive, making you more emotionally volatile during the day. Start improving your sleep by anchoring your wake time — getting up at the same time every day, including weekends, is the single most powerful sleep hygiene change. Keep your bedroom cool, dark, and reserved for sleep. If you lie awake for more than twenty minutes, get up and do something quiet in dim light until you feel drowsy, then return to bed. Avoid alcohol as a sleep aid — it may help you fall asleep faster but fragments your sleep architecture and suppresses REM. If sleep problems persist beyond a few weeks, speak with a professional. Cognitive behavioral therapy for insomnia is the gold-standard treatment and is more effective long-term than medication.", Categories = new[] { "Sleep", "Mind" }, Views = 1010, WeeklyViews = 148, Likes = 81, ImagePath = "https://images.unsplash.com/photo-1531353826977-0941b4779a1c?w=600&h=450&fit=crop" }
            };

            foreach (var seed in articleSeeds)
            {
                if (existingSet.Contains(seed.Headline)) continue;

                var article = new Article
                {
                    Headline = seed.Headline,
                    Content = seed.Content,
                    AuthorId = author.Id,
                    PublishedOn = DateTimeOffset.UtcNow.AddDays(-new Random(seed.Headline.GetHashCode()).Next(1, 60)),
                    ViewCount = seed.Views,
                    ViewsInLastWeek = seed.WeeklyViews,
                    Likes = seed.Likes,
                    ImagePath = seed.ImagePath
                };

                ctx.Articles.Add(article);
                await ctx.SaveChangesAsync();

                foreach (var catName in seed.Categories)
                {
                    if (categoryMap.TryGetValue(catName, out var catId))
                    {
                        ctx.ArticleCategories.Add(new ArticleCategory
                        {
                            ArticleId = article.Id,
                            CategoryId = catId
                        });
                    }
                }

                await ctx.SaveChangesAsync();
            }
        }

        private static async Task SeedGoalTemplatesAsync(SafeMindDbContext ctx)
        {
            if (await ctx.GoalTemplates.AnyAsync()) return;

            var goals = new[]
            {
                "Drink 8 glasses of water",
                "Take a 20-minute walk outside",
                "Write 3 things you are grateful for",
                "Meditate for 10 minutes",
                "Do a 5-minute breathing exercise",
                "Stretch for 10 minutes",
                "Eat a healthy breakfast",
                "Go to bed before 11 PM",
                "Read for 15 minutes",
                "Limit screen time to 2 hours after work",
                "Call or text a friend you haven't spoken to recently",
                "Compliment someone genuinely",
                "Spend 10 minutes tidying your space",
                "Write down one thing you did well today",
                "Take a break every 90 minutes while working",
                "Listen to a calming song or playlist",
                "Avoid caffeine after 2 PM",
                "Eat at least one serving of vegetables",
                "Practice saying no to one unnecessary commitment",
                "Spend 5 minutes sitting quietly with no phone",
                "Write a short journal entry about your day",
                "Try a new healthy recipe",
                "Do 10 minutes of light exercise",
                "Take a different route on your walk today",
                "Set one clear intention for the day",
                "Smile at a stranger",
                "Unfollow one negative social media account",
                "Drink herbal tea instead of coffee in the evening",
                "Spend time with a pet or in nature",
                "Watch a sunrise or sunset",
                "Do something creative for 15 minutes",
                "Write a kind note to yourself",
                "Organize one small area of your home",
                "Practice deep listening in a conversation today",
                "Take a warm bath or shower mindfully",
                "Identify one worry and write it down to let it go",
                "Plan something fun for the weekend",
                "Eat a meal without any screens",
                "Do a random act of kindness",
                "Spend 10 minutes in sunlight",
                "Try a body scan relaxation exercise",
                "Set your phone to do-not-disturb for 1 hour",
                "Reflect on a positive memory for 5 minutes",
                "Avoid comparing yourself to others today",
                "Take three slow deep breaths before each meal",
                "Write down your top 3 priorities for tomorrow",
                "Say something encouraging to yourself in the mirror",
                "Laugh \u2014 watch something funny for 10 minutes",
                "Forgive yourself for one small mistake today",
                "End the day by naming one good thing that happened"
            };

            ctx.GoalTemplates.AddRange(goals.Select(d => new GoalTemplate { Description = d }));
            await ctx.SaveChangesAsync();
        }

        private static async Task SeedDemoUserDataAsync(SafeMindDbContext ctx, Dictionary<string, IdentityUser> users)
        {
            if (!users.TryGetValue("alex@gmail.com", out var alexUser)) return;
            if (await ctx.Sessions.AnyAsync(s => s.PatientId == alexUser.Id)) return;

            var doctorsList = await ctx.Doctors.Include(d => d.User).ToListAsync();
            var doctorByEmail = doctorsList.ToDictionary(d => d.User!.Email!, d => d);

            if (!doctorByEmail.TryGetValue("aleksandar.dimitrov@safemind.bg", out var dAleks) ||
                !doctorByEmail.TryGetValue("borislava.ivanova@safemind.bg", out var dBori) ||
                !doctorByEmail.TryGetValue("viktor.petrov@safemind.bg", out var dViktor) ||
                !doctorByEmail.TryGetValue("emil.nikolov@safemind.bg", out var dEmil) ||
                !doctorByEmail.TryGetValue("hristo.kolev@safemind.bg", out var dHristo)) return;

            var contact = new SessionContact { FullName = "Alex Johnson", PhoneNumber = "+35988123456", Email = "alex@gmail.com" };
            ctx.SessionContacts.Add(contact);
            await ctx.SaveChangesAsync();

            static DateTimeOffset Dt(int y, int mo, int d, int h, int mi) =>
                new DateTimeOffset(y, mo, d, h, mi, 0, TimeSpan.Zero);

            // ── Past sessions ─────────────────────────────────────────────────────
            // s1–s3: expired rating window, rated
            var s1  = new Session { StartTime = Dt(2026,1,15,10, 0), EndTime = Dt(2026,1,15,10,50), DoctorId = dAleks.Id, PatientId = alexUser.Id, Price = dAleks.Price, TimeOfBooking = Dt(2026,1,10, 9,0), SessionStatus = SessionStatus.Completed, PaymentStatus = PaymentStatus.Paid, ContactId = contact.Id, Notes = "Patient reported persistent low mood and disrupted sleep. Discussed CBT techniques for rumination." };
            var s2  = new Session { StartTime = Dt(2026,1,29,14, 0), EndTime = Dt(2026,1,29,15, 0), DoctorId = dBori.Id,  PatientId = alexUser.Id, Price = dBori.Price,  TimeOfBooking = Dt(2026,1,22,10,0), SessionStatus = SessionStatus.Completed, PaymentStatus = PaymentStatus.Paid, ContactId = contact.Id, Notes = "Worked on stress response patterns and identifying emotional triggers." };
            var s3  = new Session { StartTime = Dt(2026,2,12,10, 0), EndTime = Dt(2026,2,12,10,50), DoctorId = dAleks.Id, PatientId = alexUser.Id, Price = dAleks.Price, TimeOfBooking = Dt(2026,2, 7, 9,0), SessionStatus = SessionStatus.Completed, PaymentStatus = PaymentStatus.Paid, ContactId = contact.Id, Notes = "Follow-up on sleep improvements. Patient starting to see results with the scheduled wind-down routine." };
            // s4: expired rating window, no rating
            var s4  = new Session { StartTime = Dt(2026,2,26,11, 0), EndTime = Dt(2026,2,26,11,45), DoctorId = dViktor.Id, PatientId = alexUser.Id, Price = dViktor.Price, TimeOfBooking = Dt(2026,2,20,14,0), SessionStatus = SessionStatus.Completed, PaymentStatus = PaymentStatus.Paid, ContactId = contact.Id, Notes = "Focused on work-life balance strategies. Patient identified key boundaries to establish with colleagues." };
            // s5–s6, s8: within 30-day window, already rated
            var s5  = new Session { StartTime = Dt(2026,3, 5, 9, 0), EndTime = Dt(2026,3, 5, 9,55), DoctorId = dEmil.Id,  PatientId = alexUser.Id, Price = dEmil.Price,  TimeOfBooking = Dt(2026,3, 1,11,0), SessionStatus = SessionStatus.Completed, PaymentStatus = PaymentStatus.Paid, ContactId = contact.Id, Notes = "Explored anxiety patterns and their impact on daily energy and mood. Introduced grounding techniques and discussed the role of self-care routines in maintaining progress." };
            var s6  = new Session { StartTime = Dt(2026,3,12,14, 0), EndTime = Dt(2026,3,12,15, 0), DoctorId = dBori.Id,  PatientId = alexUser.Id, Price = dBori.Price,  TimeOfBooking = Dt(2026,3, 7,10,0), SessionStatus = SessionStatus.Completed, PaymentStatus = PaymentStatus.Paid, ContactId = contact.Id, Notes = "Reviewed progress on ACT exercises. Patient reporting reduced intensity of anxious thoughts." };
            // s7: within 30-day window, CanRate = true (no rating yet)
            var s7  = new Session { StartTime = Dt(2026,3,19,10, 0), EndTime = Dt(2026,3,19,10,40), DoctorId = dHristo.Id, PatientId = alexUser.Id, Price = dHristo.Price, TimeOfBooking = Dt(2026,3,14, 9,0), SessionStatus = SessionStatus.Completed, PaymentStatus = PaymentStatus.Paid, ContactId = contact.Id, Notes = "Reviewed sleep hygiene and discussed medication management. Patient responding well." };
            var s8  = new Session { StartTime = Dt(2026,3,26,11, 0), EndTime = Dt(2026,3,26,11,50), DoctorId = dAleks.Id, PatientId = alexUser.Id, Price = dAleks.Price, TimeOfBooking = Dt(2026,3,20,10,0), SessionStatus = SessionStatus.Completed, PaymentStatus = PaymentStatus.Paid, ContactId = contact.Id, Notes = "Significant improvement noted. Discussed maintaining gains and preparing for reduced session frequency." };
            // s9: within 30-day window, CanRate = true (no rating yet)
            var s9  = new Session { StartTime = Dt(2026,4, 1, 9, 0), EndTime = Dt(2026,4, 1, 9,55), DoctorId = dEmil.Id,  PatientId = alexUser.Id, Price = dEmil.Price,  TimeOfBooking = Dt(2026,3,27,14,0), SessionStatus = SessionStatus.Completed, PaymentStatus = PaymentStatus.Paid, ContactId = contact.Id, Notes = "Progress review on anxiety management strategies. Positive momentum continuing. Reinforced mindfulness practices and self-care routines discussed in previous sessions." };
            // s10: past but doctor not yet marked complete
            var s10 = new Session { StartTime = Dt(2026,4, 1,16, 0), EndTime = Dt(2026,4, 1,16,45), DoctorId = dViktor.Id, PatientId = alexUser.Id, Price = dViktor.Price, TimeOfBooking = Dt(2026,3,28,11,0), SessionStatus = SessionStatus.Confirmed, PaymentStatus = PaymentStatus.Paid, ContactId = contact.Id };
            // ── Upcoming sessions ─────────────────────────────────────────────────
            var s11 = new Session { StartTime = Dt(2026,4, 8,10, 0), EndTime = Dt(2026,4, 8,11, 0), DoctorId = dBori.Id,  PatientId = alexUser.Id, Price = dBori.Price,  TimeOfBooking = Dt(2026,4, 1,12,0), SessionStatus = SessionStatus.Confirmed,  PaymentStatus = PaymentStatus.Paid,    ContactId = contact.Id };
            var s12 = new Session { StartTime = Dt(2026,4,15,14, 0), EndTime = Dt(2026,4,15,14,50), DoctorId = dAleks.Id, PatientId = alexUser.Id, Price = dAleks.Price, TimeOfBooking = Dt(2026,4, 2, 9,0), SessionStatus = SessionStatus.Scheduled,  PaymentStatus = PaymentStatus.Paid,    ContactId = contact.Id };
            var s13 = new Session { StartTime = Dt(2026,4,22,11, 0), EndTime = Dt(2026,4,22,11,40), DoctorId = dHristo.Id, PatientId = alexUser.Id, Price = dHristo.Price, TimeOfBooking = Dt(2026,4, 2,10,0), SessionStatus = SessionStatus.Scheduled,  PaymentStatus = PaymentStatus.Pending, ContactId = contact.Id };

            ctx.Sessions.AddRange(s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12, s13);
            await ctx.SaveChangesAsync();

            // ── Ratings ────────────────────────────────────────────────────────────
            ctx.SessionRatings.AddRange(
                new SessionRating { SessionId = s1.Id, PatientId = alexUser.Id, Stars = 5, CreatedAt = Dt(2026,1,16, 9,0) },
                new SessionRating { SessionId = s2.Id, PatientId = alexUser.Id, Stars = 4, CreatedAt = Dt(2026,1,30,10,0) },
                new SessionRating { SessionId = s3.Id, PatientId = alexUser.Id, Stars = 5, CreatedAt = Dt(2026,2,13, 9,0) },
                new SessionRating { SessionId = s5.Id, PatientId = alexUser.Id, Stars = 4, CreatedAt = Dt(2026,3, 6,10,0) },
                new SessionRating { SessionId = s6.Id, PatientId = alexUser.Id, Stars = 4, CreatedAt = Dt(2026,3,13,10,0) },
                new SessionRating { SessionId = s8.Id, PatientId = alexUser.Id, Stars = 5, CreatedAt = Dt(2026,3,27,10,0) }
            );
            await ctx.SaveChangesAsync();

            // ── Recalculate doctor ratings ─────────────────────────────────────────
            foreach (var doctorId in new[] { dAleks.Id, dBori.Id, dEmil.Id })
            {
                var avg = await ctx.SessionRatings
                    .Where(r => r.Session.DoctorId == doctorId)
                    .AverageAsync(r => (double?)r.Stars);
                if (avg.HasValue)
                {
                    var doc = await ctx.Doctors.FindAsync(doctorId);
                    if (doc != null) doc.Rating = (decimal)Math.Round(avg.Value, 2);
                }
            }
            await ctx.SaveChangesAsync();

            // ── Journals ───────────────────────────────────────────────────────────
            ctx.Journals.AddRange(
                new Journal { Title = "A difficult start to the year", Content = "Woke up feeling heavy today. The anxiety has been creeping back and I am not sure why. Work deadlines feel unmanageable and I keep second-guessing decisions I made months ago. Writing this out helps a little. I have my first session with Dr. Dimitrov next week, hoping that gives some clarity.", Mood = JournalMood.Anxious, Category = JournalCategories.Personal, UserId = alexUser.Id, CreatedAt = Dt(2026,1,10,21,30) },
                new Journal { Title = "First therapy session reflections", Content = "Came back from seeing Dr. Dimitrov today. It was more intense than I expected, in a good way. He asked questions I had not thought to ask myself. We talked about the link between sleep and mood, and he gave me a simple breathing exercise for the 3am wake-ups. I feel cautiously hopeful.", Mood = JournalMood.Calm, Category = JournalCategories.Health, UserId = alexUser.Id, CreatedAt = Dt(2026,1,15,20, 0) },
                new Journal { Title = "Work has been overwhelming", Content = "Three back-to-back delivery deadlines this week. I have been skipping lunch and staying late every day. I know this is not sustainable but I cannot see a way out right now. Session with Dr. Ivanova on Thursday and I need to talk through the boundary stuff we touched on last time.", Mood = JournalMood.Sad, Category = JournalCategories.Work, UserId = alexUser.Id, CreatedAt = Dt(2026,2,20,22,15) },
                new Journal { Title = "Noticing small improvements", Content = "Something shifted this week. Slept through the night twice, which has not happened in months. The wind-down routine Dr. Dimitrov suggested is working. I am less reactive in meetings too. Not fixed by any means, but for the first time in a while I feel like I am moving in the right direction.", Mood = JournalMood.Happy, Category = JournalCategories.Health, UserId = alexUser.Id, CreatedAt = Dt(2026,3, 1,20,30) },
                new Journal { Title = "Weekend trip with friends", Content = "Went to the mountains for two nights. No laptop, limited phone. Ate real meals, walked a lot, laughed properly. I had forgotten what it felt like to fully disengage. There is something about being in nature that quiets everything down. Coming back recharged feels different from just coming back rested.", Mood = JournalMood.Excited, Category = JournalCategories.Travel, UserId = alexUser.Id, CreatedAt = Dt(2026,3,15,19, 0) },
                new Journal { Title = "Staying consistent with check-ins", Content = "Did my daily check-in three days running this week. Noticed my stress score drops significantly on days when I exercise first thing. This app has been genuinely useful and I can now see the pattern instead of just assuming it. Want to build on this next month.", Mood = JournalMood.Calm, Category = JournalCategories.Personal, UserId = alexUser.Id, CreatedAt = Dt(2026,3,28,21, 0) },
                new Journal { Title = "Feeling more grounded", Content = "April already. Six months ago I was not sure I would get through the next week. Today I felt genuinely okay, not pretending, not pushing through. Just okay. And that feels like a lot. Still have work to do but I know how to do it now. These sessions have been worth every minute.", Mood = JournalMood.Happy, Category = JournalCategories.Ideas, UserId = alexUser.Id, CreatedAt = Dt(2026,4, 1,21, 0) }
            );

            // ── Daily checks ───────────────────────────────────────────────────────
            ctx.DailyChecks.AddRange(
                new DailyCheck { CreatedOn = Dt(2026,1,20, 8, 0), Mood = JournalMood.Anxious, Energy = EnergyLevel.Low,    Stress = StressLevel.High,   Sleep = SleepQuality.Poor,      Notes = "Woke at 3am again. Mind racing about the project kickoff. Dreading the week.", UserId = alexUser.Id },
                new DailyCheck { CreatedOn = Dt(2026,2, 5, 8,15), Mood = JournalMood.Sad,     Energy = EnergyLevel.Low,    Stress = StressLevel.High,   Sleep = SleepQuality.Fair,      Notes = "Exhausted but managed to sleep a bit more. Work pressure not letting up. Skipped breakfast.", UserId = alexUser.Id },
                new DailyCheck { CreatedOn = Dt(2026,2,18, 7,45), Mood = JournalMood.Calm,    Energy = EnergyLevel.Medium, Stress = StressLevel.Medium, Sleep = SleepQuality.Good,      Notes = "Had a session with Dr. Dimitrov yesterday. Slept better. Tried the breathing exercise before bed and it helped.", UserId = alexUser.Id },
                new DailyCheck { CreatedOn = Dt(2026,3, 3, 8, 0), Mood = JournalMood.Happy,   Energy = EnergyLevel.Medium, Stress = StressLevel.Low,    Sleep = SleepQuality.Good,      Notes = "Good week. Managed to keep to the wind-down routine four nights running. Work feels more manageable.", UserId = alexUser.Id },
                new DailyCheck { CreatedOn = Dt(2026,3,20, 7,30), Mood = JournalMood.Calm,    Energy = EnergyLevel.High,   Stress = StressLevel.Low,    Sleep = SleepQuality.Excellent, Notes = "Best night I can remember. Eight solid hours. Woke up clear-headed and looking forward to the day.", UserId = alexUser.Id },
                new DailyCheck { CreatedOn = Dt(2026,4, 1, 8, 0), Mood = JournalMood.Happy,   Energy = EnergyLevel.High,   Stress = StressLevel.Low,    Sleep = SleepQuality.Good,      Notes = "Productive morning, went for a run before work. Session with Dr. Nikolov this afternoon. Feeling good about the progress.", UserId = alexUser.Id }
            );
            await ctx.SaveChangesAsync();
        }
    }
}
