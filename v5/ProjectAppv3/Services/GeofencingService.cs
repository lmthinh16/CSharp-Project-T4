using ProjectApp.Models;

namespace ProjectApp.Services
{
    /// <summary>
    /// Geofencing service — phát hiện khi user vào/ra khỏi vùng POI.
    /// Gọi UpdateDistances() mỗi khi location thay đổi.
    /// </summary>
    public class GeofencingService
    {
        private const double EARTH_RADIUS_METERS = 6371000;
        private const int DEFAULT_RADIUS_METERS = 50;
        private const int COOLDOWN_SECONDS = 300;

        private readonly Dictionary<int, DateTime> _lastTriggered = [];
        private readonly HashSet<int> _insidePois = [];

        // ── Events ────────────────────────────────────────────────
        public event EventHandler<Restaurant>? PoiEntered;
        public event EventHandler<Restaurant>? PoiExited;

        // ── Static helper ─────────────────────────────────────────
        public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return EARTH_RADIUS_METERS * c;
        }

        private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;

        // ── Main update ───────────────────────────────────────────
        /// <summary>
        /// Gọi mỗi khi location thay đổi để kiểm tra enter/exit geofence.
        /// </summary>
        public void UpdateDistances(double userLat, double userLon, List<Restaurant> pois)
        {
            foreach (var poi in pois)
            {
                // FIX: Latitude/Longitude là double? -> bỏ qua POI không có tọa độ
                if (!poi.Latitude.HasValue || !poi.Longitude.HasValue) continue;

                double radius = poi.Radius > 0 ? poi.Radius : DEFAULT_RADIUS_METERS;
                double dist = CalculateDistance(userLat, userLon, poi.Latitude.Value, poi.Longitude.Value);
                bool inside = dist <= radius;
                bool wasInside = _insidePois.Contains(poi.Id);

                if (inside && !wasInside)
                {
                    _insidePois.Add(poi.Id);
                    if (CanTrigger(poi.Id))
                    {
                        _lastTriggered[poi.Id] = DateTime.Now;
                        PoiEntered?.Invoke(this, poi);
                    }
                }
                else if (!inside && wasInside)
                {
                    _insidePois.Remove(poi.Id);
                    PoiExited?.Invoke(this, poi);
                }
            }
        }

        // ── Legacy helper (dùng bởi code cũ) ─────────────────────
        public async Task<Restaurant?> CheckNearbyRestaurant(double userLat, double userLon)
        {
            var restaurants = await App.Database.GetRestaurantsAsync();
            Restaurant? nearest = null;
            double minDist = double.MaxValue;

            foreach (var r in restaurants)
            {
                if (!r.Latitude.HasValue || !r.Longitude.HasValue) continue;
                double dist = CalculateDistance(userLat, userLon, r.Latitude.Value, r.Longitude.Value);
                if (dist <= DEFAULT_RADIUS_METERS && CanTrigger(r.Id) && dist < minDist)
                {
                    minDist = dist;
                    nearest = r;
                }
            }

            if (nearest != null) _lastTriggered[nearest.Id] = DateTime.Now;
            return nearest;
        }

        private bool CanTrigger(int restaurantId)
        {
            if (!_lastTriggered.TryGetValue(restaurantId, out var lastTime)) return true;
            return (DateTime.Now - lastTime).TotalSeconds >= COOLDOWN_SECONDS;
        }
    }
}