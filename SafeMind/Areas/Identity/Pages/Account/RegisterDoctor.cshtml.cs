using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SafeMind.Data;
using Data.Models;
using System.ComponentModel.DataAnnotations;

namespace SafeMind.Areas.Identity.Pages.Account
{
    public class RegisterDoctorModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserStore<IdentityUser> _userStore;
        private readonly IUserEmailStore<IdentityUser> _emailStore;
        private readonly DoctorLicensingDbContext _licensingContext;
        private readonly SafeMindDbContext _context;
        private readonly ILogger<RegisterDoctorModel> _logger;

        public RegisterDoctorModel(
            UserManager<IdentityUser> userManager,
            IUserStore<IdentityUser> userStore,
            SignInManager<IdentityUser> signInManager,
            DoctorLicensingDbContext licensingContext,
            SafeMindDbContext context,
            ILogger<RegisterDoctorModel> logger)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _licensingContext = licensingContext;
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = null!;

        public string ReturnUrl { get; set; } = string.Empty;

        public List<string> AvailableSpecialties { get; set; } = new List<string>();
        public List<string> AvailableLanguages { get; set; } = new List<string>();

        public class InputModel
        {
            [Required]
            [StringLength(160)]
            [Display(Name = "Full Name")]
            public string FullName { get; set; } = string.Empty;

            [Required]
            [StringLength(32)]
            [Display(Name = "National ID")]
            public string NationalId { get; set; } = string.Empty;

            [Required]
            [StringLength(10, MinimumLength = 10, ErrorMessage = "License number must be exactly 10 characters.")]
            [Display(Name = "Medical License Number")]
            public string DoctorId { get; set; } = string.Empty;

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Specialties")]
            public List<string> SelectedSpecialties { get; set; } = new List<string>();

            [Display(Name = "Languages")]
            public List<string> SelectedLanguages { get; set; } = new List<string>();

            [Required]
            [Display(Name = "Work Start Time")]
            public TimeOnly WorkStart { get; set; } = new TimeOnly(9, 0);

            [Required]
            [Display(Name = "🏁 Office Closing Time")]
            public TimeOnly WorkEnd { get; set; } = new TimeOnly(17, 0);

            [Required]
            [Range(30, 120, ErrorMessage = "Session duration must be between 30 and 120 minutes")]
            [Display(Name = "Session Duration (minutes)")]
            public int SessionDuration { get; set; } = 50;
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? string.Empty;
            AvailableSpecialties = await _context.Specialties
                .Select(s => s.Name)
                .OrderBy(n => n)
                .ToListAsync();

            AvailableLanguages = await _context.Languages
                .Select(l => l.Name)
                .OrderBy(n => n)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
            AvailableSpecialties = await _context.Specialties.Select(s => s.Name).OrderBy(n => n).ToListAsync();
            AvailableLanguages = await _context.Languages.Select(l => l.Name).OrderBy(n => n).ToListAsync();

            if (ModelState.IsValid)
            {
                // Validate license and get licensed specialties
                var license = await _licensingContext.DoctorLicenses
                    .Include(dl => dl.DoctorLicenseSpecialties)
                    .ThenInclude(dls => dls.Specialty)
                    .FirstOrDefaultAsync(dl =>
                        dl.FullName == Input.FullName &&
                        dl.NationalId == Input.NationalId &&
                        dl.LicenseNumber == Input.DoctorId);

                if (license == null || license.Status != "Active" || license.ExpiresOn <= DateTime.UtcNow)
                {
                    ModelState.AddModelError(string.Empty, 
                        "The provided credentials do not match our licensing records or license is not active.");
                    return Page();
                }

                // Get licensed specialty names
                var licensedSpecialties = license.DoctorLicenseSpecialties
                    .Select(dls => dls.Specialty.Name)
                    .ToHashSet();

                // Validate selected specialties are in licensed specialties
                var invalidSpecialties = Input.SelectedSpecialties
                    .Where(s => !licensedSpecialties.Contains(s))
                    .ToList();

                if (invalidSpecialties.Any())
                {
                    var message = $"The following specialties are not in your license: {string.Join(", ", invalidSpecialties)}";
                    ModelState.AddModelError(string.Empty, message);
                    ModelState.AddModelError("Input.SelectedSpecialties", message);
                    return Page();
                }

                // Create user account
                var user = CreateUser();
                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Doctor created a new account with password.");
                    user.EmailConfirmed = true;
                    await _userManager.UpdateAsync(user);
                    await _userManager.AddToRoleAsync(user, "Doctor");
                    var doctor = new Doctor
                    {
                        UserId = user.Id,
                        Name = Input.FullName,
                        WorkStart = Input.WorkStart,
                        WorkEnd = Input.WorkEnd,
                        SessionDuration = Input.SessionDuration,
                        Rating = 0,
                        Biography = string.Empty,
                        Price = 0
                    };
                    _context.Doctors.Add(doctor);
                    await _context.SaveChangesAsync();

                    // Link selected specialties to doctor
                    var specialtyIds = await _context.Specialties
                        .Where(s => Input.SelectedSpecialties.Contains(s.Name))
                        .Select(s => s.Id)
                        .ToListAsync();

                    foreach (var specialtyId in specialtyIds)
                    {
                        _context.DoctorSpecialties.Add(new DoctorSpecialty
                        {
                            DoctorId = doctor.Id,
                            SpecialtyId = specialtyId
                        });
                    }

                        // Link selected languages to doctor (no license check required)
                        var languageIds = await _context.Languages
                            .Where(l => Input.SelectedLanguages.Contains(l.Name))
                            .Select(l => l.Id)
                            .ToListAsync();

                        foreach (var languageId in languageIds)
                        {
                            _context.DoctorLanguages.Add(new DoctorLanguage
                            {
                                DoctorId = doctor.Id,
                                LanguageId = languageId
                            });
                        }

                    await _context.SaveChangesAsync();

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(ReturnUrl);
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return Page();
        }

        private IdentityUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<IdentityUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(IdentityUser)}'. " +
                    $"Ensure that '{nameof(IdentityUser)}' is not an abstract class and has a parameterless constructor.");
            }
        }

        private IUserEmailStore<IdentityUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<IdentityUser>)_userStore;
        }
    }
}
