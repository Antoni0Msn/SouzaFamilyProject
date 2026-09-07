namespace WebApplication1.Models
{
    public class TitleProvider
    {
        public int Id { get; set; }

        public int TitleId { get; set; }
        public Title Title { get; set; } = null!;

        public int ProviderId { get; set; }
        public Provider Provider { get; set; } = null!;

        public string Country { get; set; } = "BR";

        public string Type { get; set; } = string.Empty;
        // Subscription / Rent / Buy / Free

        public string? WatchUrl { get; set; }

        public decimal? Price { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
