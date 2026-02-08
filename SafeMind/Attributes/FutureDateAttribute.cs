using System.ComponentModel.DataAnnotations;

namespace SafeMind.Attributes
{
    public class FutureDateAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is string expiryString && !string.IsNullOrWhiteSpace(expiryString))
            {
                var parts = expiryString.Split('/');
                if (parts.Length == 2 && 
                    int.TryParse(parts[0], out int month) && 
                    int.TryParse(parts[1], out int year))
                {                    int fullYear = 2000 + year;
                    
                    var currentDate = DateTime.UtcNow;
                    var cardExpiryEndOfMonth = new DateTime(fullYear, month, 
                        DateTime.DaysInMonth(fullYear, month), 23, 59, 59);

                    if (cardExpiryEndOfMonth < currentDate)
                    {
                        return new ValidationResult(ErrorMessage ?? "Card has expired.");
                    }
                }
            }
            
            return ValidationResult.Success;
        }
    }
}
