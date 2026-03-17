using Microsoft.Maui.Controls;
using System.Text;

namespace ProjectApp
{
    public partial class MapPage : ContentPage
    {
        private WebView webView;
        private bool isPageLoaded = false;

        public MapPage()
        {
            Title = "Bản đồ Vĩnh Khánh";

            webView = new WebView
            {
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };

            webView.Navigated += OnWebViewNavigated;

            Content = webView;

            LoadMap();
            StartLocationTracking();
        }

        private void OnWebViewNavigated(object sender, WebNavigatedEventArgs e)
        {
            if (e.Result == WebNavigationResult.Success)
            {
                isPageLoaded = true;
            }
        }

        private async void LoadMap()
        {
            var restaurants = await App.Database.GetRestaurantsAsync();

            var markersJson = new StringBuilder("[");
            for (int i = 0; i < restaurants.Count; i++)
            {
                var r = restaurants[i];
                markersJson.Append($@"
                {{
                    ""lat"": {r.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},
                    ""lng"": {r.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},
                    ""name"": ""{r.Name}"",
                    ""description"": ""{r.Description}"",
                    ""rating"": {r.Rating.ToString(System.Globalization.CultureInfo.InvariantCulture)}
                }}");

                if (i < restaurants.Count - 1) markersJson.Append(",");
            }
            markersJson.Append("]");

            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css' />
    <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
    <style>
        body, html {{ margin: 0; padding: 0; height: 100%; }}
        #map {{ width: 100vw; height: 100vh; }}
    </style>
</head>
<body>
    <div id='map'></div>
    <script>
        var map = L.map('map').setView([10.761500, 106.704500], 15);
        
        L.tileLayer('https://{{s}}.basemaps.cartocdn.com/light_all/{{z}}/{{x}}/{{y}}{{r}}.png', {{
            maxZoom: 19,
            attribution: '© OpenStreetMap contributors, © CARTO'
        }}).addTo(map);

        // Red circle marker SVG
        var redCircleSvg = 'data:image/svg+xml;base64,' + btoa(`
            <svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24'>
                <circle cx='12' cy='12' r='10' fill='#FF6B6B' stroke='white' stroke-width='2'/>
            </svg>
        `);

        var redIcon = L.icon({{
            iconUrl: redCircleSvg,
            iconSize: [24, 24],
            iconAnchor: [12, 12],
            popupAnchor: [0, -12]
        }});

        // Blue circle marker for user location
        var blueCircleSvg = 'data:image/svg+xml;base64,' + btoa(`
            <svg xmlns='http://www.w3.org/2000/svg' width='32' height='32' viewBox='0 0 32 32'>
                <circle cx='16' cy='16' r='8' fill='#4285F4' stroke='white' stroke-width='3'/>
                <circle cx='16' cy='16' r='14' fill='none' stroke='#4285F4' stroke-width='2' opacity='0.3'/>
            </svg>
        `);

        var userIcon = L.icon({{
            iconUrl: blueCircleSvg,
            iconSize: [32, 32],
            iconAnchor: [16, 16]
        }});

        // Add restaurant markers
        var markers = {markersJson};
        markers.forEach(function(m) {{
            L.marker([m.lat, m.lng], {{ icon: redIcon }})
                .bindPopup('<b>' + m.name + '</b><br>' + m.description + '<br>⭐ ' + m.rating)
                .addTo(map);
        }});

        // User location marker (will be updated from C#)
        var userMarker = null;

        // Function to update user location (called from C#)
        function updateUserLocation(lat, lng) {{
            if (userMarker) {{
                userMarker.setLatLng([lat, lng]);
            }} else {{
                userMarker = L.marker([lat, lng], {{ icon: userIcon }}).addTo(map);
            }}
            // Auto-center map on user location
            map.setView([lat, lng], map.getZoom());
        }}
    </script>
</body>
</html>";

            webView.Source = new HtmlWebViewSource { Html = html };
        }

        private async void StartLocationTracking()
        {
            while (true)
            {
                try
                {
                    var location = await Geolocation.GetLocationAsync(new GeolocationRequest
                    {
                        DesiredAccuracy = GeolocationAccuracy.High,
                        Timeout = TimeSpan.FromSeconds(10)
                    });

                    if (location != null && isPageLoaded)
                    {
                        var lat = location.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        var lng = location.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);

                        await webView.EvaluateJavaScriptAsync($"updateUserLocation({lat}, {lng});");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Location error: {ex.Message}");
                }

                await Task.Delay(3000); // Update every 3 seconds
            }
        }
    }
}