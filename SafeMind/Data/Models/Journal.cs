using SafeMind.Data.Enums;

namespace Data.Models
{
    public class Journal
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public JournalMood Mood { get; set; }
        public JournalCategories Category { get; set; }

    }
}