using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data
{
    /// <summary>
    /// Contexto principal do EF Core. Herda de IdentityDbContext para já trazer
    /// as tabelas do ASP.NET Core Identity (usuários, roles, claims, tokens etc.)
    /// usando ApplicationUser (chave int) como usuário.
    /// </summary>
    public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Title> Titles => Set<Title>();
        public DbSet<Genre> Genres => Set<Genre>();
        public DbSet<TitleGenre> TitleGenres => Set<TitleGenre>();
        public DbSet<Provider> Providers => Set<Provider>();
        public DbSet<TitleProvider> TitleProviders => Set<TitleProvider>();
        public DbSet<Season> Seasons => Set<Season>();
        public DbSet<Episode> Episodes => Set<Episode>();
        public DbSet<Favorite> Favorites => Set<Favorite>();
        public DbSet<WatchHistory> WatchHistories => Set<WatchHistory>();
        public DbSet<SyncLog> SyncLogs => Set<SyncLog>();
        public DbSet<UserProvider> UserProviders => Set<UserProvider>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ---------------- Title ----------------
            builder.Entity<Title>(entity =>
            {
                entity.ToTable("Titles");
                entity.HasIndex(t => t.Name);
                entity.HasIndex(t => t.WatchmodeId);
                entity.Property(t => t.Rating).HasPrecision(3, 1);
                entity.Property(t => t.Type).HasMaxLength(20); // Movie / Series

                // O TMDB usa o mesmo espaço de IDs numéricos pra filmes e séries -
                // um filme id=550 e uma série id=550 podem existir ao mesmo tempo.
                // Por isso a unicidade é por (TmdbId, Type), não só por TmdbId.
                entity.HasIndex(t => new { t.TmdbId, t.Type }).IsUnique();
            });

            // ---------------- Genre ----------------
            builder.Entity<Genre>(entity =>
            {
                entity.ToTable("Genres");
                entity.HasIndex(g => g.Name).IsUnique();
            });

            // ---------------- TitleGenre (N:N) ----------------
            builder.Entity<TitleGenre>(entity =>
            {
                entity.ToTable("TitleGenres");
                entity.HasKey(tg => new { tg.TitleId, tg.GenreId });

                entity.HasOne(tg => tg.Title)
                    .WithMany(t => t.Genres)
                    .HasForeignKey(tg => tg.TitleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(tg => tg.Genre)
                    .WithMany(g => g.Titles)
                    .HasForeignKey(tg => tg.GenreId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ---------------- Provider ----------------
            builder.Entity<Provider>(entity =>
            {
                entity.ToTable("Providers");
                entity.HasIndex(p => p.Name);
                entity.HasIndex(p => p.WatchmodeSourceId);
            });

            // ---------------- TitleProvider ----------------
            builder.Entity<TitleProvider>(entity =>
            {
                entity.ToTable("TitleProviders");
                entity.Property(tp => tp.Price).HasPrecision(10, 2);
                entity.Property(tp => tp.Country).HasMaxLength(2);
                entity.Property(tp => tp.Type).HasMaxLength(20);

                entity.HasOne(tp => tp.Title)
                    .WithMany(t => t.Providers)
                    .HasForeignKey(tp => tp.TitleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(tp => tp.Provider)
                    .WithMany(p => p.Titles)
                    .HasForeignKey(tp => tp.ProviderId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Evita duplicar o mesmo provider/tipo/país para o mesmo título
                entity.HasIndex(tp => new { tp.TitleId, tp.ProviderId, tp.Country, tp.Type }).IsUnique();
            });

            // ---------------- Season ----------------
            builder.Entity<Season>(entity =>
            {
                entity.ToTable("Seasons");

                entity.HasOne(s => s.Title)
                    .WithMany(t => t.Seasons)
                    .HasForeignKey(s => s.TitleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(s => new { s.TitleId, s.SeasonNumber }).IsUnique();
            });

            // ---------------- Episode ----------------
            builder.Entity<Episode>(entity =>
            {
                entity.ToTable("Episodes");

                entity.HasOne(e => e.Season)
                    .WithMany(s => s.Episodes)
                    .HasForeignKey(e => e.SeasonId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.SeasonId, e.EpisodeNumber }).IsUnique();
            });

            // ---------------- Favorite ----------------
            builder.Entity<Favorite>(entity =>
            {
                entity.ToTable("Favorites");

                entity.HasOne(f => f.User)
                    .WithMany(u => u.Favorites)
                    .HasForeignKey(f => f.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(f => f.Title)
                    .WithMany(t => t.Favorites)
                    .HasForeignKey(f => f.TitleId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Um usuário não pode favoritar o mesmo título duas vezes
                entity.HasIndex(f => new { f.UserId, f.TitleId }).IsUnique();
            });

            // ---------------- WatchHistory ----------------
            builder.Entity<WatchHistory>(entity =>
            {
                entity.ToTable("WatchHistories");

                entity.HasOne(w => w.User)
                    .WithMany(u => u.WatchHistory)
                    .HasForeignKey(w => w.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(w => w.Title)
                    .WithMany(t => t.WatchHistory)
                    .HasForeignKey(w => w.TitleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(w => new { w.UserId, w.TitleId });
            });

            // ---------------- SyncLog ----------------
            builder.Entity<SyncLog>(entity =>
            {
                entity.ToTable("SyncLogs");
                entity.HasIndex(s => s.Source);
                entity.HasIndex(s => s.StartedAt);
                entity.Property(s => s.Source).HasMaxLength(50);
                entity.Property(s => s.SyncType).HasMaxLength(50);
            });

            // ---------------- UserProvider ----------------
            builder.Entity<UserProvider>(entity =>
            {
                entity.ToTable("UserProviders");

                entity.HasOne(up => up.User)
                    .WithMany(u => u.Providers)
                    .HasForeignKey(up => up.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(up => up.Provider)
                    .WithMany()
                    .HasForeignKey(up => up.ProviderId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Um usuário não pode marcar o mesmo streaming duas vezes
                entity.HasIndex(up => new { up.UserId, up.ProviderId }).IsUnique();
            });

            // Identity usa nomes de tabela AspNetUsers, AspNetRoles etc. por padrão.
            // Renomeando para ficar mais organizado dentro do schema (opcional).
            builder.Entity<ApplicationUser>().ToTable("Users");
            builder.Entity<IdentityRole<int>>().ToTable("Roles");
            builder.Entity<IdentityUserRole<int>>().ToTable("UserRoles");
            builder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
            builder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
            builder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");
            builder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");
        }
    }
}