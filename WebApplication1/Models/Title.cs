namespace WebApplication1.Models
{
    public class Title
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string? OriginalName { get; set; }

        public string? Description { get; set; }

        public string Type { get; set; } = string.Empty; // Movie / Series

        public DateTime? ReleaseDate { get; set; }

        public double? Rating { get; set; }

        public string? PosterUrl { get; set; }
        public string? BackdropUrl { get; set; }

        public int? TmdbId { get; set; }
        public int? WatchmodeId { get; set; }
        public string? ImdbId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<TitleGenre> Genres { get; set; } = [];
        public ICollection<TitleProvider> Providers { get; set; } = [];
        public ICollection<Favorite> Favorites { get; set; } = [];
        public ICollection<WatchHistory> WatchHistory { get; set; } = [];

        public ICollection<Season> Seasons { get; set; } = [];
    }
}
