using WebApplication1.DTOs.Titles;
using WebApplication1.Models;

namespace WebApplication1.Mappings
{
    public static class TitleMapper
    {
        public static TitleResponseDto ToDto(Title title) => new()
        {
            Id = title.Id,
            Name = title.Name,
            OriginalName = title.OriginalName,
            Description = title.Description,
            Type = title.Type,
            ReleaseDate = title.ReleaseDate,
            Rating = title.Rating,
            PosterUrl = title.PosterUrl,
            BackdropUrl = title.BackdropUrl,
            Genres = title.Genres
                .Where(tg => tg.Genre != null)
                .Select(tg => tg.Genre.Name)
                .ToList(),
            Providers = title.Providers
                .Where(tp => tp.Provider != null)
                .Select(tp => new ProviderResponseDto
                {
                    Id = tp.ProviderId,
                    Name = tp.Provider.Name,
                    LogoUrl = tp.Provider.LogoUrl,
                    WatchUrl = tp.WatchUrl,
                    Type = tp.Type
                })
                .ToList()
        };
    }
}