namespace WebApplication1.Configurations
{
    public class JwtOptions
    {
        public const string SectionName = "Jwt";

        /// <summary>Chave secreta usada para assinar o token — nunca commitar, sempre via user-secrets/env var.</summary>
        public string Key { get; set; } = string.Empty;

        public string Issuer { get; set; } = "FamiliaSouza";
        public string Audience { get; set; } = "FamiliaSouzaClients";

        /// <summary>Duração do token em minutos. Padrão: 1 dia.</summary>
        public int ExpiryMinutes { get; set; } = 60 * 24;
    }
}