using System.Linq;
using Microsoft.EntityFrameworkCore;
using web_vk.Models;
using web_vk.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Một policy duy nhất cho cả Web CMS và MAUI app
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
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

// HTTPS redirect tắt khi dev local — cert localhost không hợp lệ trên điện thoại thật
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();

// CORS phải đặt sau UseRouting, trước UseAuthorization
app.UseCors("AllowAll");

app.UseSession();
app.UseOnlineTracker();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.Run();