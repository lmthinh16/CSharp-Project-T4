using Microsoft.EntityFrameworkCore;
using web_vk.Models;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddRazorPages();
builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAll");
app.UseSession();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

// ── Analytics: nuốt log từ app mobile (tránh 404) ────────────
app.MapPost("/api/analytics", (object payload) =>
{
    Console.WriteLine($"[Analytics] {System.Text.Json.JsonSerializer.Serialize(payload)}");
    return Results.Ok(new { success = true });
});

// ── /api/languages: danh sách ngôn ngữ hỗ trợ (BCP-47) ──────
// App gọi endpoint này khi sync. Nếu sau này muốn quản lý dynamic
// thì chuyển thành DB table; hiện tại trả hardcode là đủ.
app.MapGet("/api/languages", () =>
{
    var languages = new[]
    {
        new { Code = "vi-VN", Name = "Tiếng Việt", Flag = "🇻🇳", IsDefault = true,  SortOrder = 0 },
        new { Code = "en-US", Name = "English",    Flag = "🇺🇸", IsDefault = false, SortOrder = 1 },
        new { Code = "zh-CN", Name = "中文",        Flag = "🇨🇳", IsDefault = false, SortOrder = 2 },
    };
    return Results.Ok(languages);
});

app.Run();
