namespace WebApplication1.Models
{
    public class WatchHistory
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public ApplicationUser User { get; set; } = null!;

        public int TitleId { get; set; }
        public Title Title { get; set; } = null!;

        public int? SeasonNumber { get; set; }
        public int? EpisodeNumber { get; set; }

        public DateTime LastAccessedAt { get; set; }
    }
}
