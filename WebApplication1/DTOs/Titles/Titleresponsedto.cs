namespace WebApplication1.DTOs.Titles
{
    public class ProviderResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string? WatchUrl { get; set; }

        /// <summary>Subscription / Rent / Buy / Free.</summary>
        public string Type { get; set; } = string.Empty;
    }

    public class TitleResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? OriginalName { get; set; }
        public string? Description { get; set; }

        /// <summary>Movie / Series.</summary>
        public string Type { get; set; } = string.Empty;

        public DateTime? ReleaseDate { get; set; }
        public double? Rating { get; set; }
        public string? PosterUrl { get; set; }
        public string? BackdropUrl { get; set; }

        public List<string> Genres { get; set; } = [];
        public List<ProviderResponseDto> Providers { get; set; } = [];
    }
}