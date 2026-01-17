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

            var licenses = new List<DoctorLicense>
            {
                new DoctorLicense { LicenseNumber = "LIC-BG-PSY-0001", FullName = "Aleksandar Dimitrov", NationalId = "8001012345", IssuingAuthority = "Bulgarian Medical Association - Sofia", IssuedOn = new DateTime(2018, 4, 12), ExpiresOn = new DateTime(2027, 4, 11), Specialty = "Psychiatry", Status = "Active" },
                new DoctorLicense { LicenseNumber = "LIC-BG-PSY-0002", FullName = "Borislava Ivanova", NationalId = "8205123456", IssuingAuthority = "Bulgarian Medical Association - Plovdiv", IssuedOn = new DateTime(2019, 9, 1), ExpiresOn = new DateTime(2028, 8, 31), Specialty = "Clinical Psychology", Status = "Active" },
                new DoctorLicense { LicenseNumber = "LIC-BG-CNS-0003", FullName = "Viktor Petrov", NationalId = "7909234567", IssuingAuthority = "Bulgarian Medical Association - Varna", IssuedOn = new DateTime(2020, 2, 15), ExpiresOn = new DateTime(2029, 2, 14), Specialty = "Counseling", Status = "Active" },
                new DoctorLicense { LicenseNumber = "LIC-BG-NEU-0004", FullName = "Desislava Georgieva", NationalId = "8507045678", IssuingAuthority = "Bulgarian Medical Association - Sofia", IssuedOn = new DateTime(2017, 11, 5), ExpiresOn = new DateTime(2026, 11, 4), Specialty = "Neurology", Status = "Active" },
                new DoctorLicense { LicenseNumber = "LIC-BG-FAM-0005", FullName = "Emil Nikolov", NationalId = "8803156789", IssuingAuthority = "Bulgarian Medical Association - Burgas", IssuedOn = new DateTime(2016, 6, 20), ExpiresOn = new DateTime(2026, 6, 19), Specialty = "Family Medicine", Status = "Active" },
                new DoctorLicense { LicenseNumber = "LIC-BG-PED-0006", FullName = "Gabriela Stoyanova", NationalId = "9008267890", IssuingAuthority = "Bulgarian Medical Association - Pleven", IssuedOn = new DateTime(2021, 3, 10), ExpiresOn = new DateTime(2030, 3, 9), Specialty = "Pediatrics", Status = "Active" },
                new DoctorLicense { LicenseNumber = "LIC-BG-PSY-0007", FullName = "Hristo Kolev", NationalId = "7701078901", IssuingAuthority = "Bulgarian Medical Association - Stara Zagora", IssuedOn = new DateTime(2015, 1, 2), ExpiresOn = new DateTime(2025, 1, 1), Specialty = "Psychiatry", Status = "Suspended" },
                new DoctorLicense { LicenseNumber = "LIC-BG-GER-0008", FullName = "Iva Marinova", NationalId = "8602189012", IssuingAuthority = "Bulgarian Medical Association - Ruse", IssuedOn = new DateTime(2014, 9, 14), ExpiresOn = new DateTime(2025, 9, 13), Specialty = "Geriatrics", Status = "Active" },
                new DoctorLicense { LicenseNumber = "LIC-BG-ADD-0009", FullName = "Kalin Todorov", NationalId = "8403290123", IssuingAuthority = "Bulgarian Medical Association - Plovdiv", IssuedOn = new DateTime(2022, 7, 18), ExpiresOn = new DateTime(2031, 7, 17), Specialty = "Addiction Medicine", Status = "Active" },
                new DoctorLicense { LicenseNumber = "LIC-BG-CHD-0010", FullName = "Lyubomira Hristova", NationalId = "9104301234", IssuingAuthority = "Bulgarian Medical Association - Varna", IssuedOn = new DateTime(2013, 12, 1), ExpiresOn = new DateTime(2024, 11, 30), Specialty = "Child Psychology", Status = "Expired" }
            };

            ctx.DoctorLicenses.AddRange(licenses);
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
                new { Email = "aleksandar.dimitrov@safemind.bg", Name = "Aleksandar Dimitrov", License = "LIC-BG-PSY-0001", Specialty = "Psychiatry", Languages = new[]{"English","Russian"}, WorkStart = new TimeOnly(8,0), WorkEnd = new TimeOnly(16,0), Duration = 50, Rating = 4.8m },
                new { Email = "borislava.ivanova@safemind.bg", Name = "Borislava Ivanova", License = "LIC-BG-PSY-0002", Specialty = "Clinical Psychology", Languages = new[]{"English"}, WorkStart = new TimeOnly(9,0), WorkEnd = new TimeOnly(17,0), Duration = 60, Rating = 4.6m },
                new { Email = "viktor.petrov@safemind.bg", Name = "Viktor Petrov", License = "LIC-BG-CNS-0003", Specialty = "Counseling", Languages = new[]{"English","German"}, WorkStart = new TimeOnly(10,0), WorkEnd = new TimeOnly(18,0), Duration = 45, Rating = 4.5m },
                new { Email = "desislava.georgieva@safemind.bg", Name = "Desislava Georgieva", License = "LIC-BG-NEU-0004", Specialty = "Neurology", Languages = new[]{"English"}, WorkStart = new TimeOnly(7,30), WorkEnd = new TimeOnly(15,30), Duration = 50, Rating = 4.1m },
                new { Email = "emil.nikolov@safemind.bg", Name = "Emil Nikolov", License = "LIC-BG-FAM-0005", Specialty = "Family Medicine", Languages = new[]{"English","Russian"}, WorkStart = new TimeOnly(8,30), WorkEnd = new TimeOnly(16,30), Duration = 55, Rating = 4.7m },
                new { Email = "gabriela.stoyanova@safemind.bg", Name = "Gabriela Stoyanova", License = "LIC-BG-PED-0006", Specialty = "Pediatrics", Languages = new[]{"English"}, WorkStart = new TimeOnly(11,0), WorkEnd = new TimeOnly(19,0), Duration = 50, Rating = 4.3m },
                new { Email = "hristo.kolev@safemind.bg", Name = "Hristo Kolev", License = "LIC-BG-PSY-0007", Specialty = "Psychiatry", Languages = new[]{"English"}, WorkStart = new TimeOnly(8,0), WorkEnd = new TimeOnly(14,0), Duration = 40, Rating = 4.0m },
                new { Email = "iva.marinova@safemind.bg", Name = "Iva Marinova", License = "LIC-BG-GER-0008", Specialty = "Geriatrics", Languages = new[]{"English","French"}, WorkStart = new TimeOnly(9,30), WorkEnd = new TimeOnly(17,30), Duration = 45, Rating = 4.4m },
                new { Email = "kalin.todorov@safemind.bg", Name = "Kalin Todorov", License = "LIC-BG-ADD-0009", Specialty = "Addiction Medicine", Languages = new[]{"English"}, WorkStart = new TimeOnly(12,0), WorkEnd = new TimeOnly(20,0), Duration = 60, Rating = 4.2m },
                new { Email = "lyubomira.hristova@safemind.bg", Name = "Lyubomira Hristova", License = "LIC-BG-CHD-0010", Specialty = "Child Psychology", Languages = new[]{"English"}, WorkStart = new TimeOnly(6,0), WorkEnd = new TimeOnly(14,0), Duration = 50, Rating = 4.9m }
            };

            foreach (var d in doctorSeeds)
            {
                if (!users.TryGetValue(d.Email, out var user)) continue;
                if (!specialtyMap.TryGetValue(d.Specialty, out var specId)) continue;

                var doctor = new Doctor
                {
                    UserId = user.Id,
                    Name = d.Name,
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
                        ctx.DoctorLanguages.Add(new DoctorLanguages { DoctorId = doctor.Id, LanguageId = langId });
                    }
                }

                await ctx.SaveChangesAsync();
            }
        }
    }
}
