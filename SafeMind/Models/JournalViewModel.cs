using System.ComponentModel.DataAnnotations;
using Data.Enums;
namespace SafeMind.Models
{
    public class JournalViewModel
    {
        public DateTimeOffset CreatedOn { get; set; }
        public JournalMood Mood { get; set; }
        public string Title { get; set; } = string.Empty;
        public JournalCategories Category { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}