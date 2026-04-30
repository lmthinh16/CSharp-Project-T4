using SQLite;
using ProjectApp.Models;

namespace ProjectApp.Services
{
    /// <summary>
    /// SQLite local cache.
    /// Dữ liệu gốc từ API → ghi vào đây để offline vẫn dùng được.
    /// </summary>
    public class DatabaseService
    {
        private readonly SQLiteAsyncConnection _db;

        public DatabaseService()
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "VinhKhanh.db");
            _db = new SQLiteAsyncConnection(dbPath,
                SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
            
            // Dùng Task.Run để tránh deadlock trên UI Thread khi gọi async trong constructor
            Task.Run(() => InitAsync()).Wait();
        }

        private async Task InitAsync()
        {
            await _db.EnableWriteAheadLoggingAsync();
            await _db.CreateTableAsync<Restaurant>();
            await _db.CreateTableAsync<Tour>();
            await _db.CreateTableAsync<VisitHistory>();
            await _db.CreateTableAsync<AudioGuide>();
            await _db.CreateTableAsync<AppLanguage>();
            await _db.CreateTableAsync<User>();
            await _db.CreateTableAsync<Booking>();
            await _db.CreateTableAsync<AnalyticsEvent>();
        }

        // ── Restaurant ────────────────────────────────────────────

        public Task<List<Restaurant>> GetRestaurantsAsync()
            => _db.Table<Restaurant>().OrderBy(r => r.Name).ToListAsync();

        public Task<List<Restaurant>> SearchRestaurantsAsync(string keyword)
            => _db.Table<Restaurant>()
                  .Where(r => r.Name.Contains(keyword) || r.Description.Contains(keyword))
                  .ToListAsync();

        public Task<Restaurant?> GetRestaurantByIdAsync(int id)
            => _db.Table<Restaurant>().Where(r => r.Id == id).FirstOrDefaultAsync();

        public async Task<int> SaveRestaurantAsync(Restaurant restaurant)
        {
            restaurant.UpdatedAt = DateTimeOffset.UtcNow;
            if (restaurant.Id == 0) return await _db.InsertAsync(restaurant);
            return await _db.UpdateAsync(restaurant);
        }

        /// Ghi đè toàn bộ restaurants từ API (sync)
        public async Task SyncRestaurantsAsync(List<Restaurant> apiData)
        {
            await _db.DeleteAllAsync<Restaurant>();
            await _db.DeleteAllAsync<AudioGuide>(); // Reset Audios

            if (apiData.Count == 0) return;

            var audiosToInsert = new List<AudioGuide>();

            foreach (var r in apiData)
            {
                // *** Quan trọng: InsertAsync từng cái để nhận Id thật từ SQLite ***
                var cmsId = r.Id; // lưu Id gốc từ CMS
                await _db.InsertAsync(r);
                
                // Sau InsertAsync, r.Id đã được SQLite cập nhật về Id thật (nếu AutoIncrement)
                var realId = r.Id > 0 ? r.Id : cmsId;

                // TH1: CMS trả về danh sách Audios chi tiết
                if (r.Audios != null && r.Audios.Count > 0)
                {
                    foreach (var a in r.Audios)
                    {
                        a.Id = 0; // reset để SQLite tự sinh Id mới
                        a.RestaurantId = realId;
                        a.SortOrder = audiosToInsert.Count;
                        audiosToInsert.Add(a);
                    }
                }
                // TH2: CMS chỉ trả về Script văn bản (Chạy thuần script/TTS)
                else
                {
                    // Tự động tạo AudioGuide Tiếng Việt
                    if (!string.IsNullOrEmpty(r.TtsScript))
                    {
                        audiosToInsert.Add(new AudioGuide
                        {
                            RestaurantId = realId,
                            Title = $"🎙️ Giới thiệu: {r.Name}",
                            LanguageCode = "vi-VN",
                            TextContent = r.TtsScript,
                            IsGeneratedByTTS = true,
                            SortOrder = 0
                        });
                    }

                    // Tự động tạo AudioGuide Tiếng Anh
                    if (!string.IsNullOrEmpty(r.TtsScriptEn))
                    {
                        audiosToInsert.Add(new AudioGuide
                        {
                            RestaurantId = realId,
                            Title = $"🎙️ English: {r.Name}",
                            LanguageCode = "en-US",
                            TextContent = r.TtsScriptEn,
                            IsGeneratedByTTS = true,
                            SortOrder = 1
                        });
                    }
                }
            }

            if (audiosToInsert.Count > 0)
                await _db.InsertAllAsync(audiosToInsert);
        }


        // ── Tour ──────────────────────────────────────────────────

        public Task<List<Tour>> GetToursAsync()
            => _db.Table<Tour>().ToListAsync();

        public Task<int> SaveTourAsync(Tour tour)
            => tour.Id == 0
                ? _db.InsertAsync(tour)
                : _db.InsertOrReplaceAsync(tour);

        public async Task SyncToursAsync(List<Tour> apiData)
        {
            await _db.DeleteAllAsync<Tour>();
            if (apiData.Count > 0)
                await _db.InsertAllAsync(apiData);
        }

        // ── AppLanguage ───────────────────────────────────────────

        public Task<List<AppLanguage>> GetLanguagesAsync()
            => _db.Table<AppLanguage>().OrderBy(l => l.SortOrder).ToListAsync();

        public async Task SyncLanguagesAsync(List<AppLanguage> apiData)
        {
            await _db.DeleteAllAsync<AppLanguage>();
            if (apiData.Count > 0)
                await _db.InsertAllAsync(apiData);
        }

        // ── AudioGuide ────────────────────────────────────────────

        public Task<List<AudioGuide>> GetAudioGuidesAsync(int restaurantId)
            => _db.Table<AudioGuide>()
                  .Where(a => a.RestaurantId == restaurantId)
                  .OrderBy(a => a.SortOrder)
                  .ToListAsync();

        public Task<int> SaveAudioGuideAsync(AudioGuide guide)
            => guide.Id == 0 ? _db.InsertAsync(guide) : _db.UpdateAsync(guide);

        // ── VisitHistory ──────────────────────────────────────────

        public Task<List<VisitHistory>> GetVisitHistoryAsync()
            => _db.Table<VisitHistory>().OrderByDescending(v => v.VisitedAt).ToListAsync();

        public Task<int> GetVisitCountAsync()
            => _db.Table<VisitHistory>().CountAsync();

        public async Task RecordVisitAsync(int restaurantId)
        {
            await _db.InsertAsync(new VisitHistory
            {
                RestaurantId = restaurantId,
                VisitedAt    = DateTimeOffset.UtcNow
            });
        }

        // ── Favorites ─────────────────────────────────────────────

        public async Task ToggleFavoriteAsync(int restaurantId)
        {
            var r = await GetRestaurantByIdAsync(restaurantId);
            if (r == null) return;
            r.IsFavorite = !r.IsFavorite;
            await _db.UpdateAsync(r);
        }

        public Task<List<Restaurant>> GetFavoritesAsync()
            => _db.Table<Restaurant>().Where(r => r.IsFavorite).ToListAsync();

        // ── Booking ────────────────────────────────────────────────────

        public Task<int> SaveBookingAsync(Booking booking)
            => _db.InsertAsync(booking);

        public Task<int> UpdateBookingAsync(Booking booking)
            => _db.UpdateAsync(booking);

        public Task<List<Booking>> GetAllBookingsAsync()
            => _db.Table<Booking>()
                  .OrderByDescending(b => b.CreatedAtTicks)
                  .ToListAsync();

        public Task<List<Booking>> GetPendingBookingsAsync()
            => _db.Table<Booking>()
                  .Where(b => b.SyncStatus == "pending")
                  .ToListAsync();

        public async Task<Booking?> GetBookingByIdAsync(int id)
            => await _db.Table<Booking>()
                        .FirstOrDefaultAsync(b => b.Id == id);

        public Task<int> DeleteBookingAsync(int id)
            => _db.DeleteAsync<Booking>(id);

        // ── Analytics ──────────────────────────────────────────────────

        public Task<int> InsertAnalyticsEventAsync(AnalyticsEvent evt)
            => _db.InsertAsync(evt);

        public Task<List<AnalyticsEvent>> GetAllAnalyticsEventsAsync()
            => _db.Table<AnalyticsEvent>().ToListAsync();

        public Task<List<AnalyticsEvent>> GetAnalyticsEventsAsync(string eventType)
            => _db.Table<AnalyticsEvent>().Where(e => e.EventType == eventType).ToListAsync();

        public Task<int> ClearAnalyticsAsync()
            => _db.DeleteAllAsync<AnalyticsEvent>();
    }
}
