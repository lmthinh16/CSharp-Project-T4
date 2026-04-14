using ProjectApp.Models;

namespace ProjectApp.Services
{
    public class GeofencingService
    {
        private const double EARTH_RADIUS_METERS = 6371000;
        private const int DEFAULT_RADIUS_METERS = 50;
        private const int COOLDOWN_SECONDS = 300;

        private Dictionary<int, DateTime> _lastTriggered = new Dictionary<int, DateTime>();

        public double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return EARTH_RADIUS_METERS * c;
        }

        private double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        public async Task<Restaurant?> CheckNearbyRestaurant(double userLat, double userLon)
        {
            var restaurants = await App.Database.GetRestaurantsAsync();
            Restaurant? topPriorityNearest = null;

            foreach (var restaurant in restaurants)
            {
                double distance = CalculateDistance(userLat, userLon,
                    restaurant.Lat, restaurant.Lng);

                // Use the restaurant's specific radius or default
                double triggerRadius = restaurant.Radius > 0 ? restaurant.Radius : DEFAULT_RADIUS_METERS;

                if (distance <= triggerRadius)
                {
                    if (CanTrigger(restaurant.Id))
                    {
                        if (topPriorityNearest == null || restaurant.Priority > topPriorityNearest.Priority)
                        {
                            topPriorityNearest = restaurant;
                        }
                    }
                }
            }

            if (topPriorityNearest != null)
            {
                _lastTriggered[topPriorityNearest.Id] = DateTime.Now;
                
                // Fire and forget
                _ = App.Audio.PlayPoiAudioAsync(topPriorityNearest);
            }

            return topPriorityNearest;
        }

        private bool CanTrigger(int restaurantId)
        {
            if (!_lastTriggered.ContainsKey(restaurantId))
                return true;

            var elapsed = (DateTime.Now - _lastTriggered[restaurantId]).TotalSeconds;
            return elapsed >= COOLDOWN_SECONDS;
        }
    }
}