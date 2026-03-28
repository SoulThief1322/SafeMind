using Microsoft.AspNetCore.Identity;
using System.Net.Mail;
using System.Text.RegularExpressions;

public class CustomUserValidator : UserValidator<IdentityUser>
{
    private static readonly HashSet<string> DomainBlacklist = new(StringComparer.OrdinalIgnoreCase)
    {
        "mailinator.com",
        "tempmail.com",
        "10minutemail.com",
        "guerrillamail.com"
    };

    public override async Task<IdentityResult> ValidateAsync(UserManager<IdentityUser> manager, IdentityUser user)
    {
        var errors = new List<IdentityError>();

        var baseResult = await base.ValidateAsync(manager, user);
        if (!baseResult.Succeeded)
        {
            errors.AddRange(baseResult.Errors);
        }

        if (string.IsNullOrWhiteSpace(user.UserName) || user.UserName.Length < 4)
        {
            errors.Add(new IdentityError { Description = "Username must be at least 4 characters long." });
        }

        var email = user.Email ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email))
        {
            errors.Add(new IdentityError { Description = "Email is required." });
            return IdentityResult.Failed(errors.ToArray());
        }

        if (email.Length > 254)
        {
            errors.Add(new IdentityError { Description = "Email is too long." });
        }

        try
        {
            var addr = new MailAddress(email);
            if (email.Contains(' '))
            {
                errors.Add(new IdentityError { Description = "Email must not contain spaces." });
            }
            var parts = email.Split('@');
            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
            {
                errors.Add(new IdentityError { Description = "Email format is invalid." });
            }
            else
            {
                var local = parts[0];
                var domain = parts[1];
                if (!domain.Contains('.'))
                {
                    errors.Add(new IdentityError { Description = "Email domain must contain a '.' (e.g. example.com)." });
                }
                if (local.Length > 64)
                {
                    errors.Add(new IdentityError { Description = "Email local part is too long." });
                }
                var domainLower = domain.ToLowerInvariant();
                if (DomainBlacklist.Contains(domainLower))
                {
                    errors.Add(new IdentityError { Description = "Disposable email addresses are not allowed." });
                }
                var localOk = Regex.IsMatch(local, "^[^\\s\\x00-\\x1F\\x7F]+$");
                if (!localOk)
                {
                    errors.Add(new IdentityError { Description = "Email local part contains invalid characters." });
                }
            }
        }
        catch
        {
            errors.Add(new IdentityError { Description = "Email format is invalid." });
        }

        return errors.Count > 0 ? IdentityResult.Failed(errors.ToArray()) : IdentityResult.Success;
    }
}