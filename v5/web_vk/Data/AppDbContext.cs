using Microsoft.EntityFrameworkCore;
using web_vk.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Restaurant> Restaurants { get; set; }
    public DbSet<Audio> Audios { get; set; }
    public DbSet<Tour> Tours { get; set; }
    public DbSet<TourDetail> TourDetails { get; set; }
    public DbSet<UserActivityLog> UserActivityLogs { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TourDetail>()
            .HasKey(td => new { td.TourId, td.RestaurantId });

        // Chỉ cấu hình 1 lần duy nhất
        modelBuilder.Entity<Audio>()
            .HasOne(a => a.Restaurant)
            .WithMany(r => r.Audios)
            .HasForeignKey(a => a.RestaurantId)
            .IsRequired(false);

        modelBuilder.Entity<TourDetail>()
            .HasOne(td => td.Tour)
            .WithMany(t => t.TourDetails)
            .HasForeignKey(td => td.TourId);

        modelBuilder.Entity<TourDetail>()
            .HasOne(td => td.Restaurant)
            .WithMany()
            .HasForeignKey(td => td.RestaurantId);
    }
}