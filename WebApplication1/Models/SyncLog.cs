namespace WebApplication1.Models
{
    public class SyncLog
    {
        public int Id { get; set; }

        public string Source { get; set; } = string.Empty;
        // TMDB / Watchmode

        public string SyncType { get; set; } = string.Empty;

        public DateTime StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }

        public int RecordsProcessed { get; set; }
        public int RecordsCreated { get; set; }
        public int RecordsUpdated { get; set; }

        public bool Success { get; set; }

        public string? ErrorMessage { get; set; }
    }
}
