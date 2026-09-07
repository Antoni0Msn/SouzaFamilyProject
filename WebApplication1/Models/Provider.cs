using System;

namespace WebApplication1.Models
{
    public class Provider
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? LogoUrl { get; set; }

        public int? WatchmodeSourceId { get; set; }

        public ICollection<TitleProvider> Titles { get; set; } = [];
    }
}
