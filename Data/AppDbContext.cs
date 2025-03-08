using Microsoft.EntityFrameworkCore;
using EPinAPI.Models;

namespace EPinAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Epin> Epins { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Game> Games { get; set; }
    public DbSet<GameProductType> GameProductTypes { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<AdminLog> AdminLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 📌 Category - Game (1-N İlişkisi)
        modelBuilder.Entity<Game>()
            .HasOne(g => g.Category)
            .WithMany(c => c.Games)
            .HasForeignKey(g => g.CategoryId)
            .OnDelete(DeleteBehavior.Cascade); // Kategori silinirse bağlı oyunlar da silinir

        // 📌 Game - GameProductType (1-N İlişkisi)
        modelBuilder.Entity<GameProductType>()
            .HasOne(gp => gp.Game)
            .WithMany(g => g.ProductTypes)
            .HasForeignKey(gp => gp.GameId)
            .OnDelete(DeleteBehavior.Cascade); // Oyun silinirse bağlı ürünler de silinir

        modelBuilder.Entity<Order>()
            .HasOne(o => o.User)
            .WithMany()
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Epin)
            .WithMany()
            .HasForeignKey(o => o.EpinId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RefreshToken>()
           .HasOne(rt => rt.User)
           .WithMany()
           .HasForeignKey(rt => rt.UserId)
           .OnDelete(DeleteBehavior.Cascade); // 📌 Kullanıcı silinirse ona ait refresh token'lar da silinir

        modelBuilder.Entity<AdminLog>()
            .HasOne(al => al.Admin)
            .WithMany()
            .HasForeignKey(al => al.AdminId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
