namespace WebApplication1.Models
{
    public class Episode
    {
        public int Id { get; set; }

        public int SeasonId { get; set; }
        public Season Season { get; set; } = null!;

        public int EpisodeNumber { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime? AirDate { get; set; }

        public string? ThumbnailUrl { get; set; }

        public int? TmdbId { get; set; }

        public int? WatchmodeId { get; set; }
    }
}
