using System.ComponentModel.DataAnnotations;
using SafeMind.Data.Enums;

namespace SafeMind.Models
{
    public class NewJournalEntryRequest
    {
        [Required(ErrorMessage = "Title is required.")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 100 characters.")]
        public string Title { get; set; } = string.Empty;
        public JournalMood Mood { get; set; }
        public JournalCategories Category { get; set; }
        [Required(ErrorMessage = "Content is required.")]
        [StringLength(5000, MinimumLength = 1, ErrorMessage = "Content cannot be empty.")]
        public string Content { get; set; } = string.Empty;
    }
}