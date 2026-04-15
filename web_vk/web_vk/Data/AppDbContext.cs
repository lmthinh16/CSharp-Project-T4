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
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Khai báo khóa chính kép cho TourDetails dựa trên file SQL của bạn
        modelBuilder.Entity<TourDetail>()
            .HasKey(td => new { td.TourId, td.RestaurantId });
    }
}