namespace WebApplication1.Models
{
    /// <summary>
    /// Representa "este usuário assina este streaming" — diferente de TitleProvider
    /// (que é "este título está disponível neste streaming"). Usado pra personalizar
    /// futuramente o catálogo pelos streamings que a família realmente tem.
    /// </summary>
    public class UserProvider
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public ApplicationUser User { get; set; } = null!;

        public int ProviderId { get; set; }
        public Provider Provider { get; set; } = null!;
    }
}