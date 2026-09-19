using Microsoft.AspNetCore.Identity;

namespace WebApplication1.Models
{
    public class ApplicationUser : IdentityUser<int>
    {
        public string DisplayName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public ICollection<Favorite> Favorites { get; set; } = [];
        public ICollection<WatchHistory> WatchHistory { get; set; } = [];
        public ICollection<UserProvider> Providers { get; set; } = [];
    }
}