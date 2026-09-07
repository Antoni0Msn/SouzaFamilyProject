namespace WebApplication1.Models
{
    public class Genre
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public ICollection<TitleGenre> Titles { get; set; } = [];
    }
}
