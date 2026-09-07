namespace WebApplication1.Models
{
    public class Favorite
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public ApplicationUser User { get; set; } = null!;

        public int TitleId { get; set; }
        public Title Title { get; set; } = null!;

        public DateTime CreatedAt { get; set; }
    }
}
