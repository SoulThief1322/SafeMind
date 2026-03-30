using SafeMind.Data.Enums;

namespace SafeMind.Models
{
    public class NewJournalEntryRequest
    {
        public string Title { get; set; } = string.Empty;
        public JournalMood Mood { get; set; }
        public JournalCategories Category { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}