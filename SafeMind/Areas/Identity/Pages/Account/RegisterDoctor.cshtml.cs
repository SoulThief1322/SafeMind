using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace SafeMind.Areas.Identity.Pages.Account
{
    public class RegisterDoctorModel : PageModel
    {
        [BindProperty]
        public InputModel Input { get; set; } = null!;

        public string ReturnUrl { get; set; } = string.Empty;

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
        }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? string.Empty;
        }

        public IActionResult OnPost(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
            
            // Static for now - just return to page
            return Page();
        }
    }
}
