using Microsoft.Maui.Controls;
using System.Text;

namespace AppProjectT4
{
    public class MapPreviewControl : WebView
    {
        private bool isPageLoaded = false;

        public MapPreviewControl()
        {
            HeightRequest = 300;
            HorizontalOptions = LayoutOptions.Fill;
            VerticalOptions = LayoutOptions.Fill;

            Navigated += OnNavigated;

            LoadMap();
            StartLocationTracking();
        }

        private void OnNavigated(object sender, WebNavigatedEventArgs e)
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
                    ""name"": ""{r.Name}""
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
        #map {{ width: 100%; height: 300px; }}
    </style>
</head>
<body>
    <div id='map'></div>
    <script>
        var map = L.map('map', {{
            dragging: false,
            touchZoom: false,
            scrollWheelZoom: false,
            doubleClickZoom: false,
            boxZoom: false,
            keyboard: false,
            zoomControl: false
        }}).setView([10.761500, 106.704500], 15);

        L.tileLayer('https://{{s}}.basemaps.cartocdn.com/light_all/{{z}}/{{x}}/{{y}}{{r}}.png', {{
            maxZoom: 19,
            attribution: '© OpenStreetMap contributors, © CARTO'
        }}).addTo(map);

        var redCircleSvg = 'data:image/svg+xml;base64,' + btoa(`
            <svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24'>
                <circle cx='12' cy='12' r='10' fill='#FF6B6B' stroke='white' stroke-width='2'/>
            </svg>
        `);

        var redIcon = L.icon({{
            iconUrl: redCircleSvg,
            iconSize: [24, 24],
            iconAnchor: [12, 12]
        }});

        var blueCircleSvg = 'data:image/svg+xml;base64,' + btoa(`
            <svg xmlns='http://www.w3.org/2000/svg' width='28' height='28' viewBox='0 0 28 28'>
                <circle cx='14' cy='14' r='6' fill='#4285F4' stroke='white' stroke-width='2'/>
                <circle cx='14' cy='14' r='12' fill='none' stroke='#4285F4' stroke-width='1.5' opacity='0.3'/>
            </svg>
        `);

        var userIcon = L.icon({{
            iconUrl: blueCircleSvg,
            iconSize: [28, 28],
            iconAnchor: [14, 14]
        }});

        var markers = {markersJson};
        markers.forEach(function(m) {{
            L.marker([m.lat, m.lng], {{ icon: redIcon }}).addTo(map);
        }});

        var userMarker = null;

        function updateUserLocation(lat, lng) {{
            if (userMarker) {{
                userMarker.setLatLng([lat, lng]);
            }} else {{
                userMarker = L.marker([lat, lng], {{ icon: userIcon }}).addTo(map);
            }}
        }}
    </script>
</body>
</html>";

            Source = new HtmlWebViewSource { Html = html };
        }

        private async void StartLocationTracking()
        {
            while (true)
            {
                try
                {
                    var location = await Geolocation.GetLocationAsync(new GeolocationRequest
                    {
                        DesiredAccuracy = GeolocationAccuracy.Medium,
                        Timeout = TimeSpan.FromSeconds(10)
                    });

                    if (location != null && isPageLoaded)
                    {
                        var lat = location.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        var lng = location.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);

                        await this.EvaluateJavaScriptAsync($"updateUserLocation({lat}, {lng});");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"MapPreview location error: {ex.Message}");
                }

                await Task.Delay(5000); // Update every 5 seconds
            }
        }
    }
}