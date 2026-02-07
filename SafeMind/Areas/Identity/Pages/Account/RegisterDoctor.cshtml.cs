using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SafeMind.Data;
using Data.Models;
using System.ComponentModel.DataAnnotations;
using SafeMind.Services;
using Data.Constants;

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
        private readonly IDeterministicHasher _hasher;

        public RegisterDoctorModel(
            UserManager<IdentityUser> userManager,
            IUserStore<IdentityUser> userStore,
            SignInManager<IdentityUser> signInManager,
            DoctorLicensingDbContext licensingContext,
            SafeMindDbContext context,
            ILogger<RegisterDoctorModel> logger,
            IDeterministicHasher hasher)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _licensingContext = licensingContext;
            _context = context;
            _logger = logger;
            _hasher = hasher;
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

            [Required]
            [Range(DoctorConstants.MinPrice, DoctorConstants.MaxPrice, ErrorMessage = "Price must be between {1} and {2}.")]
            [Display(Name = "Price per session")]
            public decimal Price { get; set; } = (decimal)DoctorConstants.MinPrice;

            [Required]
            [StringLength(DoctorConstants.BiographyMaxLength, MinimumLength = DoctorConstants.BiographyMinLength, ErrorMessage = "Biography must be between {2} and {1} characters.")]
            [Display(Name = "Biography")]
            public string Biography { get; set; } = string.Empty;
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
                var hashedNationalId = _hasher.Hash(Input.NationalId);
                var hashedLicenseNumber = _hasher.Hash(Input.DoctorId);

                var license = await _licensingContext.DoctorLicenses
                    .Include(dl => dl.DoctorLicenseSpecialties)
                    .ThenInclude(dls => dls.Specialty)
                    .FirstOrDefaultAsync(dl =>
                        dl.FullName == Input.FullName &&
                        dl.NationalId == hashedNationalId &&
                        dl.LicenseNumber == hashedLicenseNumber);

                // Backfill if legacy records were stored unhashed
                if (license == null)
                {
                    license = await _licensingContext.DoctorLicenses
                        .Include(dl => dl.DoctorLicenseSpecialties)
                        .ThenInclude(dls => dls.Specialty)
                        .FirstOrDefaultAsync(dl =>
                            dl.FullName == Input.FullName &&
                            dl.NationalId == Input.NationalId &&
                            dl.LicenseNumber == Input.DoctorId);

                    if (license != null)
                    {
                        license.NationalId = hashedNationalId;
                        license.LicenseNumber = hashedLicenseNumber;
                        await _licensingContext.SaveChangesAsync();
                    }
                }

                if (license == null || license.Status != "Active" || license.ExpiresOn <= DateTime.UtcNow)
                {
                    ModelState.AddModelError(string.Empty, 
                        "The provided credentials do not match our licensing records or license is not active.");
                    return Page();
                }

                var licensedSpecialties = license.DoctorLicenseSpecialties
                    .Select(dls => dls.Specialty.Name)
                    .ToHashSet();

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
                        Rating = (decimal)GeneralConstants.RatingMinNumber,
                        Biography = Input.Biography,
                        Price = Input.Price
                    };
                    _context.Doctors.Add(doctor);
                    await _context.SaveChangesAsync();

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
