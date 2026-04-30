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

    // ✅ FIX: Tránh gọi LoadPois nhiều lần khi vị trí cập nhật liên tục
    private bool _poisLoaded = false;

    private CancellationTokenSource? _routeCts;

    private const string OSRM_BASE = "https://router.project-osrm.org/route/v1/driving";
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(12) };

    private const string MAP_HTML = """
     <!DOCTYPE html>
     <html>
     <head>
       <meta name="viewport" content="width=device-width,initial-scale=1,maximum-scale=1,user-scalable=no"/>
       <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css"/>
       <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
       <style>
         *{margin:0;padding:0;box-sizing:border-box}
         html,body,#map{width:100%;height:100%;background:#0F172A}
         .poi-label{background:rgba(255,255,255,.93);border:none;border-radius:8px;
                    padding:3px 8px;font-size:11px;font-weight:700;
                    box-shadow:0 2px 8px rgba(0,0,0,.22);white-space:nowrap;color:#0F172A}
         .user-dot{width:16px;height:16px;border-radius:50%;background:#2563EB;
                   box-shadow:0 0 0 0 rgba(37,99,235,.6);animation:pulse 1.8s infinite}
         @keyframes pulse{
           0%  {box-shadow:0 0 0 0 rgba(37,99,235,.6)}
           70% {box-shadow:0 0 0 12px rgba(37,99,235,0)}
           100%{box-shadow:0 0 0 0 rgba(37,99,235,0)}
         }
       </style>
     </head>
     <body>
       <div id="map"></div>
       <script>
         var map = L.map('map',{zoomControl:false,attributionControl:false});
         L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',{maxZoom:19}).addTo(map);
         L.control.zoom({position:'bottomright'}).addTo(map);

         var userMarker=null, poiMarkers={}, routeLayer=null;

         function setUserLocation(lat,lng,accurate){
           var ll=[lat,lng];
           if(userMarker){ userMarker.setLatLng(ll); }
           else{
             var icon=L.divIcon({className:'',html:'<div class="user-dot"></div>',
                                 iconSize:[16,16],iconAnchor:[8,8]});
             userMarker=L.marker(ll,{icon,zIndexOffset:1000}).addTo(map);
           }
           // Chỉ center map khi GPS thật (accurate=1), tránh nhảy do vị trí cache cũ
           if(accurate) map.setView(ll,16,{animate:true,duration:0.5});
         }

         function loadPois(json){
           var pois=JSON.parse(json);
           pois.forEach(function(p){
             var icon=L.divIcon({
               className:'',
               html:'<div style="font-size:26px;text-align:center;line-height:1;filter:drop-shadow(0 2px 4px rgba(0,0,0,.3))">'+(p.emoji||'📍')+'</div>',
               iconSize:[32,32],iconAnchor:[16,16]
             });
             var m=L.marker([p.lat,p.lng],{icon})
                    .bindTooltip(p.name,{permanent:false,direction:'top',className:'poi-label'})
                    .addTo(map);
             m.on('click',function(){ window.location.href='maui://poi?id='+p.id; });
             poiMarkers[p.id]=m;
             L.circle([p.lat,p.lng],{
               radius:p.radius||80,color:'#2563EB',fillColor:'#3B82F6',
               fillOpacity:.07,weight:1,dashArray:'4'
             }).addTo(map);
           });
         }

         function drawRoute(geojson){
           if(routeLayer) map.removeLayer(routeLayer);
           routeLayer=L.geoJSON(JSON.parse(geojson),{
             style:{color:'#2563EB',weight:5,opacity:.85,lineJoin:'round',lineCap:'round'}
           }).addTo(map);
           map.fitBounds(routeLayer.getBounds(),{padding:[48,48]});
         }

         function clearRoute(){
           if(routeLayer){map.removeLayer(routeLayer);routeLayer=null;}
         }

         function highlightPoi(id){
           Object.values(poiMarkers).forEach(function(m){
             if(m.getElement()) m.getElement().style.filter='';
           });
           if(poiMarkers[id]&&poiMarkers[id].getElement())
             poiMarkers[id].getElement().style.filter='drop-shadow(0 0 8px rgba(37,99,235,.9))';
         }

         function panTo(lat,lng,zoom){
           map.setView([lat,lng],zoom||17,{animate:true,duration:.5});
         }
       </script>
     </body>
     </html>
     """;

    public MapPage()
    {
        InitializeComponent();

        _geofencing = Application.Current!.Handler!.MauiContext!.Services.GetRequiredService<GeofencingService>();
        _audio = Application.Current!.Handler!.MauiContext!.Services.GetRequiredService<IAudioService>();
        _db = Application.Current!.Handler!.MauiContext!.Services.GetRequiredService<DatabaseService>();
        _api = Application.Current!.Handler!.MauiContext!.Services.GetRequiredService<ApiService>();

        _geofencing.PoiEntered += OnPoiEntered;
        _geofencing.PoiExited += OnPoiExited;

        mapWebView.Source = new HtmlWebViewSource { Html = MAP_HTML };
        mapWebView.Navigating += OnMapWebViewNavigating;
        mapWebView.Navigated += OnWebViewLoaded;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
        {
            await DisplayAlert("Lỗi", "Bạn chưa cấp quyền vị trí", "OK");
            return;
        }

        _currentLang = UserSession.Language ?? "vi";
        _poisLoaded = false; // Reset để load lại khi quay lại trang

        StartLocationTracking();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopLocationTracking();
        _geofencing.PoiEntered -= OnPoiEntered;
        _geofencing.PoiExited -= OnPoiExited;
    }

    // ── WebView events ───────────────────────────────────────────────

    private async void OnWebViewLoaded(object sender, WebNavigatedEventArgs e)
    {
        _mapReady = true;
        // ✅ FIX: Map đã load → thử load POI từ DB (nếu đã có data)
        await LoadPoisAsync();
    }

    // ── Load POI ─────────────────────────────────────────────────────

    private async Task LoadPoisAsync()
    {
        if (!_mapReady) return;

        try
        {
            // Bước 1: Thử lấy từ SQLite local
            _pois = await _db.GetRestaurantsAsync() ?? new();

            // ✅ FIX: Nếu DB rỗng → sync từ API luôn (không cần đợi vị trí)
            if (_pois.Count == 0)
            {
                var apiPois = await _api.GetRestaurantsAsync();
                if (apiPois.Count > 0)
                {
                    await _db.SyncRestaurantsAsync(apiPois);
                    _pois = await _db.GetRestaurantsAsync() ?? new();
                }
            }

            if (_pois.Count == 0) return;

            // FIX: Latitude/Longitude là double? -> dùng HasValue để lọc null
            // (Trước đây dùng != 0, sẽ fail khi type là nullable)
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
                id = p.Id,
                lat = p.Latitude!.Value,
                lng = p.Longitude!.Value,
                name = p.DisplayName,
                emoji = string.IsNullOrEmpty(p.Emoji) ? "🍽️" : p.Emoji,
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
        _currentLocation = e.Location;

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await EvalJsAsync($"setUserLocation({e.Location.Latitude},{e.Location.Longitude})");

            // ✅ FIX: Nếu POI chưa được load (lần đầu nhận vị trí) → thử load lại
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
        var doc = JsonDocument.Parse(json);
        var geo = doc.RootElement.GetProperty("routes")[0].GetProperty("geometry");
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