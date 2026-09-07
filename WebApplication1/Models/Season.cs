namespace WebApplication1.Models
{
    public class Season
    {
        public int Id { get; set; }

        public int TitleId { get; set; }
        public Title Title { get; set; } = null!;

        public int SeasonNumber { get; set; }

        public string? Name { get; set; }

        public DateTime? AirDate { get; set; }

        public string? PosterUrl { get; set; }

        public ICollection<Episode> Episodes { get; set; } = [];
    }
}
