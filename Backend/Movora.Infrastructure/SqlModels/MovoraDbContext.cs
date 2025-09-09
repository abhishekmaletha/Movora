using Microsoft.EntityFrameworkCore;

namespace Movora.Infrastructure.SqlModels;

public class MovoraDbContext : DbContext
{
    public MovoraDbContext(DbContextOptions<MovoraDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<User> Users { get; set; }
    public DbSet<Movie> Movies { get; set; }
    public DbSet<Series> Series { get; set; }
    public DbSet<Episode> Episodes { get; set; }
    public DbSet<UserWatched> UserWatched { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<SearchHistory> SearchHistories { get; set; }
    public DbSet<Recommendation> Recommendations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure user table
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.GoogleId).IsUnique();
        });

        // Configure movie table
        modelBuilder.Entity<Movie>(entity =>
        {
            entity.HasIndex(e => e.TmdbId).IsUnique();
            entity.HasIndex(e => e.Title);
            entity.HasIndex(e => e.ReleaseDate);
        });

        // Configure series table
        modelBuilder.Entity<Series>(entity =>
        {
            entity.HasIndex(e => e.TmdbId).IsUnique();
            entity.HasIndex(e => e.Title);
            entity.HasIndex(e => e.FirstAirDate);
        });

        // Configure episode table
        modelBuilder.Entity<Episode>(entity =>
        {
            entity.HasIndex(e => new { e.SeriesId, e.SeasonNumber, e.EpisodeNumber }).IsUnique();
        });

        // Configure user_watched table
        modelBuilder.Entity<UserWatched>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.ContentId, e.ContentType }).IsUnique();
            entity.HasIndex(e => e.WatchedDate);
        });

        // Configure review table
        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.ContentId, e.ContentType }).IsUnique();
            entity.HasIndex(e => e.Rating);
            entity.HasIndex(e => e.CreatedDate);
        });

        // Configure comment table
        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasIndex(e => e.ReviewId);
            entity.HasIndex(e => e.CreatedDate);
        });

        // Configure search_history table
        modelBuilder.Entity<SearchHistory>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedDate);
            entity.HasIndex(e => e.Query);
        });

        // Configure recommendation table
        modelBuilder.Entity<Recommendation>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Score);
            entity.HasIndex(e => e.CreatedDate);
            entity.HasIndex(e => new { e.UserId, e.ContentId, e.ContentType }).IsUnique();
        });

        // Configure relationships for UserWatched
        modelBuilder.Entity<UserWatched>()
            .HasOne(uw => uw.Movie)
            .WithMany(m => m.WatchedByUsers)
            .HasForeignKey(uw => uw.ContentId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_UserWatched_Movie");

        modelBuilder.Entity<UserWatched>()
            .HasOne(uw => uw.Series)
            .WithMany(s => s.WatchedByUsers)
            .HasForeignKey(uw => uw.ContentId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_UserWatched_Series");

        // Configure relationships for Review
        modelBuilder.Entity<Review>()
            .HasOne(r => r.Movie)
            .WithMany(m => m.Reviews)
            .HasForeignKey(r => r.ContentId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_Review_Movie");

        modelBuilder.Entity<Review>()
            .HasOne(r => r.Series)
            .WithMany(s => s.Reviews)
            .HasForeignKey(r => r.ContentId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_Review_Series");

        // Configure relationships for Recommendation
        modelBuilder.Entity<Recommendation>()
            .HasOne(r => r.Movie)
            .WithMany(m => m.Recommendations)
            .HasForeignKey(r => r.ContentId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_Recommendation_Movie");

        modelBuilder.Entity<Recommendation>()
            .HasOne(r => r.Series)
            .WithMany(s => s.Recommendations)
            .HasForeignKey(r => r.ContentId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_Recommendation_Series");
    }
}
