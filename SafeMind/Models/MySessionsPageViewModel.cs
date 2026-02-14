namespace SafeMind.Models
{
    public class MySessionsPageViewModel
    {
        public List<MySessionsViewModel> Upcoming { get; set; } = new();
        public List<MySessionsViewModel> Unpaid { get; set; } = new();
        public List<MySessionsViewModel> Past { get; set; } = new();
        public int PaidCount { get; set; }
        public int ProgressPercent { get; set; }
    }
}
