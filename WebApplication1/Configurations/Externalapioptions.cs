namespace WebApplication1.Configurations
{
    public class TmdbOptions
    {
        public const string SectionName = "Tmdb";

        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.themoviedb.org/3/";
        public string ImageBaseUrl { get; set; } = "https://image.tmdb.org/t/p";
        public string Language { get; set; } = "pt-BR";
    }

    public class WatchmodeOptions
    {
        public const string SectionName = "Watchmode";

        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.watchmode.com/v1";
        public string DefaultRegion { get; set; } = "BR";
    }

    public class SyncOptions
    {
        public const string SectionName = "Sync";

        /// <summary>
        /// Chave simples enviada no header X-Sync-Key para proteger o endpoint de sincronização
        /// enquanto a autenticação por roles (admin) ainda não está implementada.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;
    }
}