using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using MovieApi.Domain.Entities;

namespace MovieApi.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<MovieCategory> MovieCategories => Set<MovieCategory>();
    public DbSet<Episode> Episodes => Set<Episode>();
    public DbSet<Video> Videos => Set<Video>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // users
        b.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).IsRequired().HasMaxLength(255);
            e.Property(x => x.PasswordHash).IsRequired().HasMaxLength(500);
            e.Property(x => x.Status).IsRequired();
            e.Property(x => x.CreatedAt).IsRequired();
        });

        // roles
        b.Entity<Role>(e =>
        {
            e.ToTable("roles");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Name).IsRequired().HasMaxLength(50);
        });

        // user_roles (composite key)
        b.Entity<UserRole>(e =>
        {
            e.ToTable("user_roles");
            e.HasKey(x => new { x.UserId, x.RoleId });
            e.HasOne(x => x.User).WithMany(u => u.UserRoles).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Role).WithMany(r => r.UserRoles).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
        });

        // refresh_tokens
        b.Entity<RefreshToken>(e =>
        {
            e.ToTable("refresh_tokens");

            e.HasKey(x => x.Id);

            e.HasIndex(x => new { x.UserId, x.DeviceId }).IsUnique();

            e.Property(x => x.TokenHash)
                .IsRequired()
                .HasMaxLength(500);

            e.Property(x => x.DeviceId)
                .IsRequired()
                .HasMaxLength(100);

            e.Property(x => x.ExpiresAt)
                .IsRequired();

            e.Property(x => x.CreatedAt)
                .IsRequired();

            e.HasOne(x => x.User)
                .WithMany(x => x.RefreshTokens)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // movies
        b.Entity<Movie>(e =>
        {
            e.ToTable("movies");
            e.Property(x => x.Title).HasMaxLength(300);
            e.Property(x => x.Type).HasMaxLength(30);
        });

        // categories
        b.Entity<Category>(e =>
        {
            e.ToTable("categories");
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.Slug).HasMaxLength(120);
        });

        // movie_categories
        b.Entity<MovieCategory>(e =>
        {
            e.ToTable("movie_categories");
            e.HasKey(x => new { x.MovieId, x.CategoryId });
            e.HasOne(x => x.Movie).WithMany(m => m.MovieCategories).HasForeignKey(x => x.MovieId);
            e.HasOne(x => x.Category).WithMany(c => c.MovieCategories).HasForeignKey(x => x.CategoryId);
        });

        // episodes (unique movie_id + episode_number)
        b.Entity<Episode>(e =>
        {
            e.ToTable("episodes");
            e.HasIndex(x => new { x.MovieId, x.EpisodeNumber }).IsUnique();
            e.Property(x => x.Title).HasMaxLength(300);
        });

        // videos
        b.Entity<Video>(e =>
        {
            e.ToTable("videos");
            e.Property(x => x.ServerName).HasMaxLength(50);
            e.Property(x => x.Quality).HasMaxLength(20);
            e.Property(x => x.Url).HasMaxLength(2000);
        });

        // audit_logs
        b.Entity<AuditLog>(e =>
        {
            e.ToTable("audit_logs");

            e.HasKey(x => x.Id);

            e.HasIndex(x => x.EventId).IsUnique();

            e.Property(x => x.EventId)
                .IsRequired()
                .HasMaxLength(80);

            e.Property(x => x.Action)
                .IsRequired()
                .HasMaxLength(50);

            e.Property(x => x.Entity)
                .IsRequired()
                .HasMaxLength(50);

            e.Property(x => x.EntityId)
                .IsRequired();

            e.Property(x => x.PayloadJson)
                .IsRequired()
                .HasColumnType("json");

            e.Property(x => x.CreatedAt)
                .IsRequired();
        });
        base.OnModelCreating(b);
        b.Entity<Role>().HasData(
            //Admin, Editor, Viewer
            new Role { Id = 1, Name = "Admin" },
            new Role { Id = 2, Name = "Editor" },
            new Role { Id = 3, Name = "Viewer" }
        );
    }
}