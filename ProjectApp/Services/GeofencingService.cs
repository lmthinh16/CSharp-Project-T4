using ProjectApp.Models;

namespace ProjectApp.Services
{
    /// <summary>
    /// Kiểm tra nhà hàng gần nhất trong bán kính cho phép.
    /// Tích hợp Cooldown: tránh phát audio lặp lại khi người dùng đứng yên.
    /// </summary>
    public class GeofencingService
    {
        // Lưu thời gian trigger gần nhất theo restaurantId
        private readonly Dictionary<int, DateTime> _lastTriggeredAt = new();

        // CooldownMinutes mặc định nếu AudioGuide không chỉ định
        private const int DefaultCooldownMinutes = 5;

        /// <summary>
        /// Tìm POI gần nhất trong bán kính. 
        /// Trả về null nếu đang trong thời gian Cooldown.
        /// </summary>
        public async Task<Restaurant?> CheckNearbyRestaurant(double lat, double lng)
        {
            var restaurants = await App.Database.GetRestaurantsAsync();
            Restaurant? nearest = null;
            double minDist = double.MaxValue;

            foreach (var r in restaurants)
            {
                double dist = CalculateDistance(lat, lng, r.Latitude, r.Longitude);
                // Dùng Radius từng nhà hàng (PRD: Restaurants.Radius)
                double triggerRadius = r.Radius > 0 ? r.Radius : 100;
                if (dist <= triggerRadius && dist < minDist)
                {
                    minDist = dist;
                    nearest = r;
                }
            }

            if (nearest == null) return null;

            // Kiểm tra Cooldown
            if (IsOnCooldown(nearest.Id, DefaultCooldownMinutes))
                return null; // Đang trong Cooldown — bỏ qua trigger

            return nearest;
        }

        /// <summary>
        /// Ghi nhận đã trigger POI này (gọi sau khi bắt đầu phát audio).
        /// </summary>
        public void RecordTrigger(int restaurantId)
        {
            _lastTriggeredAt[restaurantId] = DateTime.UtcNow;
        }

        /// <summary>
        /// Kiểm tra xem POI đang trong Cooldown hay chưa.
        /// </summary>
        public bool IsOnCooldown(int restaurantId, int cooldownMinutes = DefaultCooldownMinutes)
        {
            if (!_lastTriggeredAt.TryGetValue(restaurantId, out var lastTime))
                return false; // Chưa từng trigger → không Cooldown

            return (DateTime.UtcNow - lastTime).TotalMinutes < cooldownMinutes;
        }

        public double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000;
            var φ1 = lat1 * Math.PI / 180;
            var φ2 = lat2 * Math.PI / 180;
            var Δφ = (lat2 - lat1) * Math.PI / 180;
            var Δλ = (lon2 - lon1) * Math.PI / 180;
            var a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2)
                  + Math.Cos(φ1) * Math.Cos(φ2)
                  * Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        /// <summary>
        /// Cập nhật khoảng cách và đánh dấu nearest cho danh sách restaurant.
        /// </summary>
        public void UpdateDistances(List<Restaurant> restaurants, double userLat, double userLng)
        {
            foreach (var r in restaurants)
            {
                r.DistanceMeters = CalculateDistance(userLat, userLng, r.Latitude, r.Longitude);
                r.IsNearest = false;
            }
            var nearest = restaurants.MinBy(r => r.DistanceMeters);
            if (nearest != null) nearest.IsNearest = true;
        }
    }
}
