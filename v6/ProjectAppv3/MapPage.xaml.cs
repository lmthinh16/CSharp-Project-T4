using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices.Sensors;
using System.Text.Json;
using ProjectApp.Models;
using ProjectApp.Services;

namespace ProjectApp;

public partial class MapPage : ContentPage
{
    private readonly GeofencingService _geofencing;
    private readonly IAudioService _audio;
    private readonly DatabaseService _db;
    private readonly ApiService _api;

    private List<Restaurant> _pois = new();
    private Restaurant? _selectedPoi;
    private Location? _currentLocation;

    private bool _isRouting = false;
    private string _currentLang = "vi";
    private bool _mapReady = false;
    private bool _poisLoaded = false;

    private CancellationTokenSource? _routeCts;

    private const string OSRM_BASE = "https://router.project-osrm.org/route/v1/driving";
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(12) };

    // MAP_HTML_TEMPLATE dùng {{API_KEY}} làm placeholder, được thay bằng key thật lúc khởi tạo
    private static readonly string MAP_HTML =
        MAP_HTML_TEMPLATE.Replace("{{API_KEY}}", AppSecrets.GoogleMapsApiKey);

    private const string MAP_HTML_TEMPLATE = """
     <!DOCTYPE html>
     <html>
     <head>
       <meta name="viewport" content="width=device-width,initial-scale=1,maximum-scale=1,user-scalable=no"/>
       <style>
         *{margin:0;padding:0;box-sizing:border-box}
         html,body,#map{width:100%;height:100%}
       </style>
     </head>
     <body>
       <div id="map"></div>
       <script>
         var map, userMarker=null, poiMarkers={}, routePolyline=null;

         function initMap(){
           map=new google.maps.Map(document.getElementById('map'),{
             zoom:15,
             center:{lat:16.047,lng:108.206},
             disableDefaultUI:true,
             zoomControl:true,
             gestureHandling:'greedy'
           });
           // Báo C# map đã sẵn sàng nhận lệnh JS
           window.location.href='maui://map-ready';
         }

         function svgUserDot(){
           return 'data:image/svg+xml;charset=UTF-8,'+encodeURIComponent(
             '<svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 32 32">'+
             '<circle cx="16" cy="16" r="8" fill="#2563EB" stroke="white" stroke-width="2"/>'+
             '<circle cx="16" cy="16" r="8" fill="none" stroke="#2563EB" stroke-width="2">'+
               '<animate attributeName="r" from="8" to="18" dur="1.8s" repeatCount="indefinite"/>'+
               '<animate attributeName="opacity" from="0.6" to="0" dur="1.8s" repeatCount="indefinite"/>'+
             '</circle></svg>');
         }

         function svgEmojiIcon(emoji){
           return 'data:image/svg+xml;charset=UTF-8,'+encodeURIComponent(
             '<svg xmlns="http://www.w3.org/2000/svg" width="36" height="40" viewBox="0 0 36 40">'+
             '<text y="32" font-size="30" font-family="Segoe UI Emoji,Apple Color Emoji,Noto Color Emoji,sans-serif">'+
             emoji+'</text></svg>');
         }

         function setUserLocation(lat,lng,accurate){
           var pos={lat:lat,lng:lng};
           if(userMarker){
             userMarker.setPosition(pos);
           }else{
             userMarker=new google.maps.Marker({
               position:pos,map:map,zIndex:1000,optimized:false,
               icon:{url:svgUserDot(),scaledSize:new google.maps.Size(32,32),anchor:new google.maps.Point(16,16)}
             });
           }
           if(accurate) map.panTo(pos);
         }

         function loadPois(json){
           // Xoá markers cũ trước khi load lại
           Object.values(poiMarkers).forEach(function(m){m.setMap(null);});
           poiMarkers={};

           var pois=JSON.parse(json);
           pois.forEach(function(p){
             var marker=new google.maps.Marker({
               position:{lat:p.lat,lng:p.lng},map:map,
               title:p.name,zIndex:100,optimized:false,
               icon:{url:svgEmojiIcon(p.emoji||'📍'),
                     scaledSize:new google.maps.Size(36,40),
                     anchor:new google.maps.Point(18,20)}
             });
             marker.addListener('click',function(){
               window.location.href='maui://poi?id='+p.id;
             });
             poiMarkers[p.id]=marker;

             new google.maps.Circle({
               map:map,center:{lat:p.lat,lng:p.lng},radius:p.radius||80,
               fillColor:'#3B82F6',fillOpacity:0.07,
               strokeColor:'#2563EB',strokeWeight:1,strokeOpacity:0.5
             });
           });
         }

         function drawRoute(geojson){
           if(routePolyline) routePolyline.setMap(null);
           var data=JSON.parse(geojson);
           var coords=data.geometry.coordinates;
           var path=coords.map(function(c){return{lat:c[1],lng:c[0]};});
           routePolyline=new google.maps.Polyline({
             path:path,map:map,
             strokeColor:'#2563EB',strokeWeight:5,strokeOpacity:0.85
           });
           var bounds=new google.maps.LatLngBounds();
           path.forEach(function(p){bounds.extend(p);});
           map.fitBounds(bounds,48);
         }

         function clearRoute(){
           if(routePolyline){routePolyline.setMap(null);routePolyline=null;}
         }

         function highlightPoi(id){
           Object.keys(poiMarkers).forEach(function(k){poiMarkers[k].setAnimation(null);});
           if(poiMarkers[id]){
             poiMarkers[id].setAnimation(google.maps.Animation.BOUNCE);
             setTimeout(function(){if(poiMarkers[id])poiMarkers[id].setAnimation(null);},1400);
           }
         }

         function panTo(lat,lng,zoom){
           map.panTo({lat:lat,lng:lng});
           if(zoom) map.setZoom(zoom);
         }
       </script>
       <script src="https://maps.googleapis.com/maps/api/js?key={{API_KEY}}&callback=initMap&v=weekly" async defer></script>
     </body>
     </html>
     """;

    public MapPage()
    {
        InitializeComponent();

        _geofencing = Application.Current!.Handler!.MauiContext!.Services.GetRequiredService<GeofencingService>();
        _audio      = Application.Current!.Handler!.MauiContext!.Services.GetRequiredService<IAudioService>();
        _db         = Application.Current!.Handler!.MauiContext!.Services.GetRequiredService<DatabaseService>();
        _api        = Application.Current!.Handler!.MauiContext!.Services.GetRequiredService<ApiService>();

        mapWebView.Source = new HtmlWebViewSource { Html = MAP_HTML };
        mapWebView.Navigating += OnMapWebViewNavigating;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _geofencing.PoiEntered += OnPoiEntered;
        _geofencing.PoiExited  += OnPoiExited;

        var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
        {
            var L = Services.LocalizationManager.Instance;
            await DisplayAlert(L["err_title"], L["map_err_perm"], L["ok"]);
            return;
        }

        _currentLang = UserSession.Language ?? "vi";
        _poisLoaded = false;

        StartLocationTracking();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopLocationTracking();
        _geofencing.PoiEntered -= OnPoiEntered;
        _geofencing.PoiExited  -= OnPoiExited;
    }

    // ── Load POI ─────────────────────────────────────────────────────

    private async Task LoadPoisAsync()
    {
        if (!_mapReady) return;

        try
        {
            _pois = await _db.GetRestaurantsAsync() ?? new();

            if (_pois.Count == 0)
            {
                var apiPois = await _api.GetRestaurantsAsync();
                if (apiPois.Count > 0)
                {
                    await _db.SyncRestaurantsAsync(apiPois, _api.BaseUrl);
                    _pois = await _db.GetRestaurantsAsync() ?? new();
                }
            }

            if (_pois.Count == 0) return;

            var validPois = _pois
                .Where(p => p.Latitude.HasValue && p.Longitude.HasValue
                         && p.Latitude.Value != 0 && p.Longitude.Value != 0)
                .ToList();

            if (validPois.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[MapPage] Tất cả POI đều thiếu tọa độ");
                return;
            }

            var json = JsonSerializer.Serialize(validPois.Select(p => new
            {
                id     = p.Id,
                lat    = p.Latitude!.Value,
                lng    = p.Longitude!.Value,
                name   = p.DisplayName,
                emoji  = string.IsNullOrEmpty(p.Emoji) ? "🍽️" : p.Emoji,
                radius = p.Radius > 0 ? p.Radius : 80
            }));

            await EvalJsAsync($"loadPois({JsonSerializer.Serialize(json)})");
            _poisLoaded = true;

            System.Diagnostics.Debug.WriteLine($"[MapPage] Đã load {validPois.Count} markers lên map");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MapPage] LoadPoisAsync lỗi: {ex.Message}");
        }
    }

    // ── Location tracking ────────────────────────────────────────────

    private void StartLocationTracking()
    {
        Geolocation.Default.LocationChanged += OnLocationChanged;
        _ = Geolocation.Default.StartListeningForegroundAsync(
            new GeolocationListeningRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(3)));
    }

    private void StopLocationTracking()
    {
        Geolocation.Default.LocationChanged -= OnLocationChanged;
        Geolocation.Default.StopListeningForeground();
    }

    private async void OnLocationChanged(object? sender, GeolocationLocationChangedEventArgs e)
    {
        // Lớp 1: Bỏ qua tọa độ (0,0) — Android trả về trước khi có GPS fix
        if (e.Location.Latitude == 0 && e.Location.Longitude == 0) return;

        _currentLocation = e.Location;

        // Lớp 2: Chỉ auto-center khi accuracy đủ tốt (< 100m)
        // Marker vẫn được đặt nhưng map không nhảy khi GPS chưa ổn định
        var isAccurate = e.Location.Accuracy is null or < 100;

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await EvalJsAsync($"setUserLocation({e.Location.Latitude},{e.Location.Longitude},{(isAccurate ? 1 : 0)})");

            if (!_poisLoaded)
                await LoadPoisAsync();
        });

        _geofencing.UpdateDistances(e.Location.Latitude, e.Location.Longitude,
            _pois.Where(p => p.Latitude.HasValue && p.Longitude.HasValue).ToList());
    }

    // ── JS bridge ────────────────────────────────────────────────────

    private Task EvalJsAsync(string js)
    {
        if (!_mapReady || mapWebView == null)
            return Task.CompletedTask;
        return mapWebView.EvaluateJavaScriptAsync(js);
    }

    private void OnMapWebViewNavigating(object? sender, WebNavigatingEventArgs e)
    {
        // Google Maps initMap() callback báo map đã sẵn sàng
        if (e.Url == "maui://map-ready")
        {
            e.Cancel = true;
            _mapReady = true;
            _ = LoadPoisAsync();

            // Khôi phục vị trí người dùng nếu GPS đã có trước khi map load xong
            if (_currentLocation != null)
            {
                var loc = _currentLocation;
                var isAccurate = loc.Accuracy is null or < 100;
                _ = EvalJsAsync($"setUserLocation({loc.Latitude},{loc.Longitude},{(isAccurate ? 1 : 0)})");
            }
            return;
        }

        if (e.Url.StartsWith("maui://poi?id="))
        {
            e.Cancel = true;
            if (int.TryParse(e.Url.Replace("maui://poi?id=", ""), out int id))
            {
                var poi = _pois.FirstOrDefault(p => p.Id == id);
                if (poi != null)
                {
                    _selectedPoi = poi;
                    _ = EvalJsAsync($"highlightPoi({id})");
                    ShowBottomSheet(poi);
                }
            }
        }
    }

    // ── Geofence ─────────────────────────────────────────────────────

    private async void OnPoiEntered(object? sender, Restaurant poi)
    {
        _selectedPoi = poi;
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await EvalJsAsync($"highlightPoi({poi.Id})");
            ShowBottomSheet(poi);
        });
        try { await _audio.PlayAudioAsync(poi, _currentLang); }
        catch { }
    }

    private void OnPoiExited(object? sender, Restaurant poi)
    {
        if (_selectedPoi?.Id == poi.Id)
            MainThread.BeginInvokeOnMainThread(HideBottomSheet);
    }

    // ── UI helpers ───────────────────────────────────────────────────

    private void ShowBottomSheet(Restaurant poi)
    {
        poiNameLabel.Text = poi.Name;
        poiDescLabel.Text = poi.Description;
        bottomSheet.IsVisible = true;
    }

    private void HideBottomSheet()
    {
        bottomSheet.IsVisible = false;
    }

    // ── XAML event handlers ──────────────────────────────────────────

    private async void OnAudioBtnClicked(object sender, EventArgs e)
    {
        if (_selectedPoi == null) return;
        await _audio.PlayAudioAsync(_selectedPoi, _currentLang);
    }

    private async void OnRouteBtnClicked(object sender, EventArgs e)
    {
        if (_selectedPoi == null || _currentLocation == null) return;

        var url = $"{OSRM_BASE}/{_currentLocation.Longitude},{_currentLocation.Latitude}" +
                  $";{_selectedPoi.Longitude},{_selectedPoi.Latitude}" +
                  "?overview=full&geometries=geojson";

        var json = await _http.GetStringAsync(url);
        var doc  = JsonDocument.Parse(json);
        var geo  = doc.RootElement.GetProperty("routes")[0].GetProperty("geometry");
        var geojson = JsonSerializer.Serialize(new { type = "Feature", geometry = geo });

        await EvalJsAsync($"drawRoute({JsonSerializer.Serialize(geojson)})");
    }

    private void OnRouteInfoClose(object sender, EventArgs e)
    {
        routeInfoBar.IsVisible = false;
        _ = EvalJsAsync("clearRoute()");
    }

    private void OnMyLocationClicked(object sender, EventArgs e)
    {
        if (_currentLocation == null) return;
        _ = EvalJsAsync($"panTo({_currentLocation.Latitude},{_currentLocation.Longitude},17)");
    }

    private async void OnPoiDetailClicked(object sender, EventArgs e)
    {
        if (_selectedPoi == null) return;
        await Navigation.PushAsync(new Pages.RestaurantDetailPage(_selectedPoi));
    }

    private void OnBottomSheetClose(object sender, EventArgs e)
    {
        HideBottomSheet();
    }
}
