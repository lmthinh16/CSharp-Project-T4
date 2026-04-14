using SQLite;
using ProjectApp.Models;

namespace ProjectApp.Services
{
    public class DatabaseService
    {
        private readonly SQLiteAsyncConnection _database;

        public DatabaseService()
        {
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "vinhkhanh.db");
            _database = new SQLiteAsyncConnection(dbPath);
            _database.CreateTableAsync<Restaurant>().Wait();
            _database.CreateTableAsync<VisitHistory>().Wait();
            _database.CreateTableAsync<Audio>().Wait();
        }

        // ── Audio ──────────────────────────────────────────────────

        public Task<List<Audio>> GetAudiosForRestaurantAsync(int restaurantId, string langCode = null)
        {
            var query = _database.Table<Audio>().Where(a => a.RestaurantId == restaurantId);
            if (!string.IsNullOrEmpty(langCode))
            {
                query = query.Where(a => a.LanguageCode == langCode);
            }
            return query.ToListAsync();
        }

        public Task<int> SaveAudioAsync(Audio audio)
            => _database.InsertAsync(audio);

        public Task<int> ClearAudiosAsync()
            => _database.DeleteAllAsync<Audio>();


        // ── Restaurant ─────────────────────────────────────────────

        public Task<List<Restaurant>> GetRestaurantsAsync()
            => _database.Table<Restaurant>().ToListAsync();

        public Task<int> SaveRestaurantAsync(Restaurant restaurant)
            => _database.InsertAsync(restaurant);

        public Task<int> UpdateRestaurantAsync(Restaurant restaurant)
            => _database.UpdateAsync(restaurant);

        public async Task DeleteRestaurantAsync(int id)
            => await _database.DeleteAsync<Restaurant>(id);

        public Task<int> ClearRestaurantsAsync()
            => _database.DeleteAllAsync<Restaurant>();

        // ── Visit ──────────────────────────────────────────────────

        public Task<int> SaveVisitAsync(VisitHistory visit)
            => _database.InsertAsync(visit);

        public Task<List<VisitHistory>> GetVisitHistoryAsync()
            => _database.Table<VisitHistory>()
                        .OrderByDescending(v => v.VisitedAt)
                        .ToListAsync();



        public Task<int> ClearVisitHistoryAsync()
            => _database.DeleteAllAsync<VisitHistory>();
    }
}