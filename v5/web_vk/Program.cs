using System.Linq;
using Microsoft.EntityFrameworkCore;
using web_vk.Models;
using web_vk.Middleware;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddPolicy("MobileApp", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});
builder.Services.AddMemoryCache();
// Services
builder.Services.AddRazorPages();
// Đã dọn dẹp ElevenLabsService và TextToSpeechService cũ tại đây
builder.Services.AddHttpClient();
builder.Services.AddControllers();          // ← thêm
builder.Services.AddEndpointsApiExplorer(); // ← thêm (optional, cho Swagger)


var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsPath); // tạo nếu chưa có
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});
app.UseCors("MobileApp");

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();
app.UseOnlineTracker();  // ✅ Track user online (web + app)
app.UseAuthorization();

app.MapRazorPages();
app.UseCors("AllowAll");
app.MapControllers();

// --- API ĐỒNG BỘ DỮ LIỆU ĐA NGÔN NGỮ ---
// Endpoint này sẽ được App .NET MAUI gọi để tải dữ liệu về chạy Offline
app.MapGet("/api/sync-data", async (AppDbContext db) =>
{
    var data = await db.Restaurants
        .Include(r => r.Audios) // Lấy kịch bản từ bảng Audios
        .AsNoTracking()
        .Select(r => new {
            Id = r.Id,
            Name = r.Name,
            Lat = r.Lat,
            Lng = r.Lng,
            Radius = r.Radius, // Bán kính Geofence (mặc định 50m)
            // Lấy danh sách kịch bản kèm mã ngôn ngữ chuẩn (vi-VN, en-US, ko-KR...)
            Scripts = r.Audios.Select(a => new {
                a.Title,
                a.TextContent,
                a.LanguageCode
            }).ToList()
        })
        .ToListAsync();

    return Results.Ok(data);
});

// Các API quản lý địa điểm cơ bản
app.MapGet("/api/restaurants", async (AppDbContext db) => await db.Restaurants.ToListAsync());

app.Run();