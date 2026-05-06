using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices.Sensors;
using ProjectApp.Models;
using ProjectApp.Services;

namespace ProjectApp;

public partial class MapPage : ContentPage
{
    // ── Services ──────────────────────────────────────────────────────────
    private static IAudioService Audio => App.Audio;

    // ── UI ────────────────────────────────────────────────────────────────
    private readonly WebView _webView;
    private readonly Label _statusLabel;
    private readonly Grid _audioBarContainer;
    private readonly Label _audioBarLabel;
    private Button _pauseAudioBtn = null!;
    private Grid _offlineBanner = null!;

    // ── State ─────────────────────────────────────────────────────────────
    private List<Restaurant> _pois = new();
    private List<AppLanguage> _langs = new();
    private string _currentLang = UserSession.Language;
    private bool _htmlLoaded = false;
    private bool _mapReady = false;
    private bool _audioPaused = false;
    private Location? _userLocation;
    private Location? _pendingLocation;
    private readonly Dictionary<int, string> _imgCache = new();
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };

    // ─────────────────────────────────────────────────────────────────────

    public MapPage()
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Color.FromArgb("#0D1B2A");

        App.Geofencing.PoiEntered += OnPoiEntered;

        // WebView
        _webView = new WebView
        {
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            BackgroundColor = Colors.Transparent
        };
        _webView.Navigating += OnNavigating;

        // Status label
        _statusLabel = new Label
        {
            Text = "Đang tải bản đồ...",
            BackgroundColor = Color.FromArgb("#F0F6FF"),
            TextColor = Color.FromArgb("#0D2137"),
            Padding = new Thickness(16, 12, 16, 24),
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center
        };

        // Audio bar
        _audioBarContainer = new Grid
        {
            BackgroundColor = Color.FromArgb("#1565C0"),
            IsVisible = false,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };
        _audioBarLabel = new Label
        {
            Text = string.Empty,
            TextColor = Colors.White,
            Padding = new Thickness(16, 10, 0, 10),
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            VerticalTextAlignment = TextAlignment.Center
        };
        var stopBtn = new Button
        {
            Text = "⏹ Dừng",
            TextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#d32f2f"),
            FontAttributes = FontAttributes.Bold,
            FontSize = 12,
            CornerRadius = 14,
            HeightRequest = 30,
            Padding = new Thickness(12, 0),
            Margin = new Thickness(0, 0, 12, 0),
            VerticalOptions = LayoutOptions.Center
        };
        stopBtn.Clicked += (_, _) =>
        {
            Audio.Stop();
            _audioPaused = false;
        };
        _pauseAudioBtn = new Button
        {
            Text = "⏸ Tạm dừng",
            TextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#f57c00"),
            FontAttributes = FontAttributes.Bold,
            FontSize = 12,
            CornerRadius = 14,
            HeightRequest = 30,
            Padding = new Thickness(12, 0),
            Margin = new Thickness(8, 0),
            VerticalOptions = LayoutOptions.Center
        };
        _pauseAudioBtn.Clicked += (_, _) =>
        {
            if (_audioPaused) { Audio.Resume(); _audioPaused = false; _pauseAudioBtn.Text = "⏸ Tạm dừng"; _pauseAudioBtn.BackgroundColor = Color.FromArgb("#f57c00"); }
            else { Audio.Pause(); _audioPaused = true; _pauseAudioBtn.Text = "▶ Tiếp tục"; _pauseAudioBtn.BackgroundColor = Color.FromArgb("#2e7d32"); }
        };
        _audioBarContainer.Add(_audioBarLabel, 0, 0);
        _audioBarContainer.Add(_pauseAudioBtn, 1, 0);
        _audioBarContainer.Add(stopBtn, 2, 0);

        Audio.OnPlaybackStateChanged += isPlaying => MainThread.BeginInvokeOnMainThread(() =>
        {
            _audioBarContainer.IsVisible = isPlaying;
            _audioBarLabel.Text = isPlaying ? $"🎧 Đang phát: {Audio.CurrentTrack?.Title ?? "..."}" : string.Empty;
            if (!isPlaying) _audioPaused = false;
        });

        // Offline banner
        _offlineBanner = new Grid
        {
            BackgroundColor = Color.FromArgb("#B71C1C"),
            IsVisible = !OfflineService.Instance.IsOnline,
            Padding = new Thickness(16, 6),
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star }
            }
        };
        _offlineBanner.Add(new Label { Text = "📡", FontSize = 14, VerticalOptions = LayoutOptions.Center }, 0, 0);
        _offlineBanner.Add(new Label
        {
            Text = "Đang offline – Bản đồ từ bộ nhớ cache",
            TextColor = Colors.White, FontSize = 12, FontAttributes = FontAttributes.Bold,
            VerticalOptions = LayoutOptions.Center, Margin = new Thickness(8, 0)
        }, 1, 0);
        OfflineService.Instance.StatusChanged += isOnline =>
            MainThread.BeginInvokeOnMainThread(() => _offlineBanner.IsVisible = !isOnline);

        var grid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto }
            }
        };
        grid.Add(_offlineBanner, 0, 0);
        grid.Add(_webView, 0, 1);
        grid.Add(_audioBarContainer, 0, 2);
        grid.Add(_statusLabel, 0, 3);
        Content = grid;

        _ = InitAsync();
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _currentLang = UserSession.Language;
        if (_mapReady)
            _ = EvalJsAsync($"cL='{_currentLang}';if(typeof applyLang==='function')applyLang();");
        else if (!_htmlLoaded)
            _ = InitAsync();
        _ = RequestLocationPermissionAsync();
    }

    // ── Init ──────────────────────────────────────────────────────────────

    private async Task InitAsync()
    {
        try
        {
            if (_pois.Count == 0)
            {
                _pois = await App.Database.GetRestaurantsAsync();
                if (_pois.Count == 0)
                {
                    var apiPois = await App.Api.GetRestaurantsAsync();
                    if (apiPois.Count > 0)
                    {
                        _pois = apiPois;
                        await App.Database.SyncRestaurantsAsync(apiPois, App.Api.BaseUrl);
                        _pois = await App.Database.GetRestaurantsAsync();
                    }
                }
            }

            if (_langs.Count == 0)
                _langs = await App.Api.GetLanguagesAsync();

            if (!_htmlLoaded)
            {
                // Hiện map ngay với URL ảnh trực tiếp, không chờ prefetch
                var json = BuildJson();
                _htmlLoaded = true;
                _webView.Source = new HtmlWebViewSource
                {
                    Html    = GetHtml(json),
                    BaseUrl = "file:///android_asset/"
                };
                // Fetch ảnh trong background; khi xong inject lại data vào JS
                _ = FetchImagesAsync().ContinueWith(_ =>
                {
                    var updated = BuildJson();
                    MainThread.BeginInvokeOnMainThread(() =>
                        _ = EvalJsAsync($"ALL={updated};renderMenu&&renderMenu();"));
                });
            }
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Lỗi: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[MapPage] InitAsync: {ex}");
        }
    }

    // ── Data ──────────────────────────────────────────────────────────────

    private string GetImgUrl(Restaurant r)
    {
        if (string.IsNullOrWhiteSpace(r.ImageUrl)) return "";
        if (r.ImageUrl.StartsWith("http://") || r.ImageUrl.StartsWith("https://")) return r.ImageUrl;
        return App.Api.BaseUrl.TrimEnd('/') + "/" + r.ImageUrl.TrimStart('/');
    }

    private async Task FetchImagesAsync()
    {
        foreach (var r in _pois)
        {
            if (_imgCache.ContainsKey(r.Id) || string.IsNullOrWhiteSpace(r.ImageUrl)) continue;
            try
            {
                var url = GetImgUrl(r);
                if (string.IsNullOrEmpty(url)) continue;
                var bytes = await _http.GetByteArrayAsync(url);
                var ext = url.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "png" : "jpeg";
                _imgCache[r.Id] = $"data:image/{ext};base64," + Convert.ToBase64String(bytes);
            }
            catch { _imgCache[r.Id] = ""; }
        }
    }

    private string BuildJson()
    {
        var ic = System.Globalization.CultureInfo.InvariantCulture;
        var valid = _pois.Where(p => p.Latitude.HasValue && p.Longitude.HasValue
                                  && p.Latitude.Value != 0 && p.Longitude.Value != 0).ToList();
        var sb = new StringBuilder("[");
        for (int i = 0; i < valid.Count; i++)
        {
            var r = valid[i];
            var img = _imgCache.TryGetValue(r.Id, out var b64) && !string.IsNullOrEmpty(b64) ? b64 : GetImgUrl(r);
            if (i > 0) sb.Append(',');
            sb.Append($"{{\"id\":{r.Id}");
            sb.Append($",\"lat\":{r.Latitude!.Value.ToString(ic)}");
            sb.Append($",\"lng\":{r.Longitude!.Value.ToString(ic)}");
            sb.Append($",\"name\":\"{Esc(r.DisplayName)}\"");
            sb.Append($",\"nameEn\":\"{Esc(r.NameEn)}\"");
            sb.Append($",\"nameZh\":\"{Esc(r.NameZh)}\"");
            sb.Append($",\"desc\":\"{Esc(r.DisplayDescription)}\"");
            sb.Append($",\"descEn\":\"{Esc(r.DescriptionEn)}\"");
            sb.Append($",\"descZh\":\"{Esc(r.DescriptionZh)}\"");
            sb.Append($",\"addr\":\"{Esc(r.Address)}\"");
            sb.Append($",\"rating\":{r.Rating.ToString(ic)}");
            sb.Append($",\"hours\":\"{Esc(r.OpenHours)}\"");
            sb.Append($",\"fav\":{r.IsFavorite.ToString().ToLower()}");
            sb.Append($",\"img\":\"{img}\"}}");
        }
        sb.Append("]");
        return sb.ToString();
    }

    private static string Esc(string? s) =>
        (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("'", "\\'")
                 .Replace("\n", "\\n").Replace("\r", "");

    // ── JS Bridge ─────────────────────────────────────────────────────────

    private void OnNavigating(object? sender, WebNavigatingEventArgs e)
    {
        if (!e.Url.StartsWith("maui://")) return;
        e.Cancel = true;
        var uri = new Uri(e.Url);
        var q = System.Web.HttpUtility.ParseQueryString(uri.Query);
        var ic = System.Globalization.CultureInfo.InvariantCulture;

        switch (uri.Host.ToLower())
        {
            case "mapready":
                _mapReady = true;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _statusLabel.Text = L("Map ready", "地图已就绪", "Bản đồ sẵn sàng");
                    _ = EvalJsAsync($"cL='{_currentLang}';if(typeof applyLang==='function')applyLang();");
                    if (_pendingLocation != null)
                    {
                        var ic = System.Globalization.CultureInfo.InvariantCulture;
                        _ = EvalJsAsync($"sPos({_pendingLocation.Latitude.ToString(ic)},{_pendingLocation.Longitude.ToString(ic)});");
                        _pendingLocation = null;
                    }
                });
                break;

            case "locationupdated":
                if (double.TryParse(q["lat"], System.Globalization.NumberStyles.Float, ic, out double lat) &&
                    double.TryParse(q["lng"], System.Globalization.NumberStyles.Float, ic, out double lng))
                {
                    _userLocation = new Location(lat, lng);
                    App.Geofencing.UpdateDistances(lat, lng, _pois);
                }
                break;

            case "routerequested":
                var data = Uri.UnescapeDataString(q["data"] ?? "");
                MainThread.BeginInvokeOnMainThread(() => _ = DrawRouteAsync(data));
                break;

            case "statusupdate":
                var msg = Uri.UnescapeDataString(q["msg"] ?? "");
                MainThread.BeginInvokeOnMainThread(() => _statusLabel.Text = msg);
                break;

            case "stopaudio":
                MainThread.BeginInvokeOnMainThread(() => { Audio.Stop(); _audioPaused = false; });
                break;

            case "changelang":
                var lang = q["lang"] ?? "vi";
                _currentLang = lang;
                UserSession.Language = lang;
                Services.LocalizationManager.Instance.SetLanguage(lang);
                // KHÔNG rebuild HTML — JS applyLang() đã xử lý cập nhật UI
                // JSON đã có nameEn/nameZh/descEn/descZh nên JS tự switch được
                MainThread.BeginInvokeOnMainThread(() =>
                    _statusLabel.Text = L("Map ready", "地图已就绪", "Bản đồ sẵn sàng"));
                break;

            case "speakpoi":
                if (int.TryParse(q["id"], out int speakId))
                {
                    var poi = _pois.FirstOrDefault(r => r.Id == speakId);
                    if (poi != null)
                        MainThread.BeginInvokeOnMainThread(() => _ = PlayAudioAsync(poi));
                }
                break;

            case "togglefav":
                if (int.TryParse(q["id"], out int favId))
                {
                    var poi = _pois.FirstOrDefault(r => r.Id == favId);
                    if (poi != null)
                    {
                        poi.IsFavorite = !poi.IsFavorite;
                        _ = App.Database.SaveRestaurantAsync(poi);
                        ViewModels.FavoritesViewModel.NotifyFavoritesChanged();
                        var favStr = poi.IsFavorite.ToString().ToLower();
                        MainThread.BeginInvokeOnMainThread(() =>
                            _ = EvalJsAsync($"if(cur&&cur.id==={poi.Id}){{cur.fav={favStr};document.getElementById('txFavBtn').innerHTML=cur.fav?'❤️':'🤍';}}"));
                    }
                }
                break;

        }
    }

    private Task EvalJsAsync(string js)
    {
        if (!_mapReady || _webView == null) return Task.CompletedTask;
        return _webView.EvaluateJavaScriptAsync(js);
    }

    private string L(string en, string zh, string vi) =>
        _currentLang switch { "en" => en, "zh" => zh, _ => vi };

    // ── Location ──────────────────────────────────────────────────────────

    private async Task RequestLocationPermissionAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (status != PermissionStatus.Granted)
            {
                _statusLabel.Text = L("GPS permission required", "需要GPS权限", "Cần cấp quyền GPS");
                return;
            }

            _statusLabel.Text = L("Getting location...", "获取位置中...", "Đang lấy vị trí...");
            _ = StartLocationAsync();
        }
        catch { }
    }

    private async Task StartLocationAsync()
    {
        try
        {
            var loc = await Geolocation.Default.GetLocationAsync(new GeolocationRequest
            {
                DesiredAccuracy = GeolocationAccuracy.Medium,
                Timeout         = TimeSpan.FromSeconds(15)
            });
            if (loc == null) return;

            _userLocation = loc;
            var ic = System.Globalization.CultureInfo.InvariantCulture;
            var js = $"sPos({loc.Latitude.ToString(ic)},{loc.Longitude.ToString(ic)});";

            if (_mapReady)
                await EvalJsAsync(js);
            else
                _pendingLocation = loc;

            App.Geofencing.UpdateDistances(loc.Latitude, loc.Longitude, _pois);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MapPage] Location: {ex.Message}");
        }
    }

    // ── Audio ─────────────────────────────────────────────────────────────

    private async Task PlayAudioAsync(Restaurant poi)
    {
        try { await Audio.PlayAudioAsync(poi, _currentLang); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[MapPage] Audio: {ex.Message}"); }
    }

    // ── Geofencing ────────────────────────────────────────────────────────

    private void OnPoiEntered(object? sender, Restaurant poi)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Phát audio hướng dẫn
            _ = PlayAudioAsync(poi);

            // Hiện proximity alert card trên map
            var img   = _imgCache.TryGetValue(poi.Id, out var b64) && !string.IsNullOrEmpty(b64) ? b64 : "";
            var name  = _currentLang switch { "en" => poi.NameEn ?? poi.Name, "zh" => poi.NameZh ?? poi.Name, _ => poi.Name };
            var desc  = _currentLang switch { "en" => poi.DescriptionEn ?? poi.DisplayDescription, "zh" => poi.DescriptionZh ?? poi.DisplayDescription, _ => poi.DisplayDescription };
            _ = EvalJsAsync($"showProximity({poi.Id},'{Esc(name)}','{Esc(desc)}','{img}');");
        });
    }

    // ── Route ─────────────────────────────────────────────────────────────

    private async Task<bool> DrawRouteAsync(string json)
    {
        var loc = _userLocation ?? new Location(10.7615, 106.7045);
        try
        {
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var info = JsonSerializer.Deserialize<RouteRequest>(json, opts);
            if (info == null) return false;

            MainThread.BeginInvokeOnMainThread(() =>
                _statusLabel.Text = L("Calculating route...", "计算路线中...", "Đang tính đường đi..."));

            var ic  = System.Globalization.CultureInfo.InvariantCulture;
            // routing.openstreetmap.de — OSRM chính thức của OSM, ổn định hơn demo server
            var url = "https://routing.openstreetmap.de/routed-foot/route/v1/foot/"
                    + $"{loc.Longitude.ToString(ic)},{loc.Latitude.ToString(ic)};"
                    + $"{info.Lng.ToString(ic)},{info.Lat.ToString(ic)}"
                    + "?overview=full&geometries=geojson";

            System.Diagnostics.Debug.WriteLine($"[MapPage] OSRM request: {url}");

            var resp = await _http.GetStringAsync(url);
            using var doc = JsonDocument.Parse(resp);
            var root = doc.RootElement;
            var code = root.GetProperty("code").GetString();

            System.Diagnostics.Debug.WriteLine($"[MapPage] OSRM response code: {code}");

            if (code != "Ok")
            {
                MainThread.BeginInvokeOnMainThread(() =>
                    _statusLabel.Text = L("No route found", "无法找到路线", "Không tìm thấy đường đi"));
                return false;
            }

            var route    = root.GetProperty("routes")[0];
            var distM    = route.GetProperty("distance").GetDouble();
            var durS     = route.GetProperty("duration").GetDouble();
            var distText = distM >= 1000 ? $"{distM / 1000.0:F1} km" : $"{(int)distM} m";
            var durText  = durS  >= 3600
                ? $"{(int)(durS / 3600)}h {(int)(durS % 3600 / 60)}min"
                : $"{(int)(durS / 60)} min";

            // OSRM trả [lng, lat] — Leaflet cần [lat, lng]
            var coords = route.GetProperty("geometry").GetProperty("coordinates");
            var sb = new StringBuilder("[");
            bool first = true;
            foreach (var pt in coords.EnumerateArray())
            {
                if (!first) sb.Append(',');
                first = false;
                var lng = pt[0].GetDouble();
                var lat = pt[1].GetDouble();
                sb.Append($"[{lat.ToString(ic)},{lng.ToString(ic)}]");
            }
            sb.Append("]");

            var img = info.Id > 0 && _imgCache.TryGetValue(info.Id, out var b64) && !string.IsNullOrEmpty(b64) ? b64 : "";
            await MainThread.InvokeOnMainThreadAsync(async () =>
                await _webView.EvaluateJavaScriptAsync(
                    $"drawPremiumRoute({sb},'{Esc(info.Name)}','{Esc(distText)}','{Esc(durText)}','{img}');"));
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MapPage] DrawRoute error: {ex.Message}");
            MainThread.BeginInvokeOnMainThread(() =>
                _statusLabel.Text = L("Routing error — check connection", "路线错误，请检查网络", "Lỗi chỉ đường — kiểm tra mạng"));
            return false;
        }
    }

    // ── HTML ──────────────────────────────────────────────────────────────

    private string GetHtml(string data)
    {
        var lines = new List<string>();
        var initialFlag = _langs.FirstOrDefault(l => l.Code == _currentLang)?.Flag
                       ?? (_currentLang == "en" ? "🇺🇸" : _currentLang == "zh" ? "🇨🇳" : "🇻🇳");

        lines.Add("<!DOCTYPE html>");
        lines.Add("<html><head><meta charset='utf-8'>");
        lines.Add("<meta name='viewport' content='width=device-width,initial-scale=1.0,maximum-scale=1.0,user-scalable=no'>");
        lines.Add("<link rel='stylesheet' href='leaflet.css'/>");
        lines.Add("<style>");
        lines.Add("*{box-sizing:border-box;margin:0;padding:0;font-family:'Segoe UI',Roboto,sans-serif;-webkit-tap-highlight-color:transparent}");
        lines.Add("body,html{height:100%;width:100%;overflow:hidden;background:#F0F6FF}");
        lines.Add("#map{position:fixed;top:0;left:0;width:100vw;height:100vh;z-index:1}");
        // Header
        lines.Add("#headerRow{position:fixed;top:16px;left:16px;right:16px;z-index:1000;display:flex;gap:12px;align-items:center}");
        lines.Add("#searchBar{flex:1;display:flex;align-items:center;background:rgba(255,255,255,0.95);backdrop-filter:blur(12px);border:1px solid rgba(21,101,192,0.3);border-radius:24px;box-shadow:0 4px 20px rgba(21,101,192,0.15);padding:0 16px;height:48px}");
        lines.Add("#searchBar input{flex:1;border:none;outline:none;font-size:15px;color:#0D2137;background:transparent;margin-left:8px}");
        lines.Add("#searchBar input::placeholder{color:#5A7A9A}");
        lines.Add("#btnSearch{background:none;border:none;font-size:18px;color:#42A5F5}");
        lines.Add("#btnClearSearch{background:none;border:none;font-size:18px;cursor:pointer;color:#8ba0b2;display:none}");
        lines.Add("#langBtn{width:48px;height:48px;border-radius:24px;background:rgba(255,255,255,0.95);backdrop-filter:blur(12px);border:1px solid rgba(21,101,192,0.3);display:flex;align-items:center;justify-content:center;font-size:20px;box-shadow:0 4px 20px rgba(21,101,192,0.15);cursor:pointer}");
        lines.Add("#langDropdown{position:fixed;top:76px;right:16px;z-index:2000;background:rgba(255,255,255,0.97);border:1px solid rgba(21,101,192,0.3);border-radius:16px;box-shadow:0 10px 40px rgba(21,101,192,0.2);display:none;flex-direction:column;overflow:hidden;backdrop-filter:blur(10px);min-width:160px}");
        lines.Add(".ldItem{padding:14px 20px;color:#0D2137;font-size:14px;font-weight:600;display:flex;gap:10px;align-items:center;border-bottom:1px solid rgba(21,101,192,0.1);white-space:nowrap}");
        lines.Add(".ldItem:active{background:rgba(21,101,192,0.1)}");
        // Search results
        lines.Add("#searchResults{position:fixed;top:72px;left:16px;right:16px;z-index:1001;background:rgba(255,255,255,0.97);backdrop-filter:blur(16px);border:1px solid rgba(21,101,192,0.2);border-radius:20px;box-shadow:0 8px 32px rgba(21,101,192,0.15);display:none;max-height:280px;overflow-y:auto}");
        lines.Add(".srItem{display:flex;align-items:center;gap:14px;padding:14px 16px;border-bottom:1px solid rgba(21,101,192,0.08);cursor:pointer}");
        lines.Add(".srImg{width:48px;height:48px;border-radius:12px;object-fit:cover;background:#152535;flex-shrink:0;display:flex;align-items:center;justify-content:center;font-size:24px}");
        lines.Add(".srImg img{width:100%;height:100%;object-fit:cover;border-radius:12px}");
        lines.Add(".srName{font-size:15px;font-weight:600;color:#0D2137}");
        lines.Add(".srAddr{font-size:12px;color:#5A7A9A;margin-top:4px}");
        // Floating buttons
        lines.Add(".fBtn{width:52px;height:52px;border-radius:50%;background:rgba(13,27,42,0.9);backdrop-filter:blur(8px);border:1px solid rgba(66,165,245,0.4);color:#64B5F6;box-shadow:0 8px 24px rgba(0,0,0,0.5);display:flex;align-items:center;justify-content:center;font-size:22px;transition:all 0.2s}");
        lines.Add(".fBtn:active{transform:scale(0.9);background:rgba(30,136,229,0.9)}");
        lines.Add("#btnMenu{position:fixed;right:16px;bottom:272px;z-index:1000}");
        lines.Add("#btnG{position:fixed;right:16px;bottom:208px;z-index:1000;background:rgba(255,255,255,0.95);border:1px solid rgba(21,101,192,0.3);color:#1565C0;box-shadow:0 4px 20px rgba(21,101,192,0.2)}");
        lines.Add("#btnStop{position:fixed;right:16px;bottom:336px;z-index:1000;background:linear-gradient(135deg,#d32f2f,#E91E63);color:white;border:none;display:none}");
        // Bottom sheets
        lines.Add(".botSheet{position:fixed;bottom:0;left:0;right:0;z-index:9500;background:rgba(255,255,255,0.98);backdrop-filter:blur(20px);border-radius:28px 28px 0 0;box-shadow:0 -8px 30px rgba(21,101,192,0.15);transform:translateY(100%);transition:transform 0.4s cubic-bezier(0.2,0.8,0.2,1);display:flex;flex-direction:column;border-top:1px solid rgba(21,101,192,0.15)}");
        lines.Add(".botSheet.open{transform:translateY(0)}");
        lines.Add(".botSheet.open.minim{transform:translateY(calc(100% - 90px))}");
        lines.Add(".dragBar{width:40px;height:5px;background:rgba(21,101,192,0.25);border-radius:4px;margin:12px auto}");
        // Menu sheet
        lines.Add("#menuSheet{max-height:80vh}");
        lines.Add(".mhd{padding:4px 24px 16px;font-size:18px;font-weight:700;color:#0D2137;border-bottom:1px solid rgba(21,101,192,0.1)}");
        lines.Add(".mlist{overflow-y:auto;flex:1;padding-bottom:24px}");
        lines.Add(".mitem{display:flex;align-items:center;gap:14px;padding:16px 24px;border-bottom:1px solid rgba(21,101,192,0.08)}");
        lines.Add(".mimg{width:64px;height:64px;border-radius:14px;object-fit:cover;background:#1c2f42;flex-shrink:0;overflow:hidden}");
        lines.Add(".mimg img{width:100%;height:100%;object-fit:cover}");
        lines.Add(".minfo{flex:1;min-width:0}");
        lines.Add(".mnm{font-size:16px;font-weight:600;color:#0D2137;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}");
        lines.Add(".mrt{font-size:13px;color:#FFCA28;font-weight:700;margin-top:4px}");
        lines.Add(".mhr{font-size:12px;color:#5A7A9A;margin-top:2px}");
        lines.Add(".mbtns{display:flex;flex-direction:column;gap:8px;flex-shrink:0}");
        lines.Add(".mbtn{width:42px;height:42px;border-radius:14px;border:none;font-size:18px;display:flex;align-items:center;justify-content:center;color:#fff}");
        lines.Add(".mbtnNav{background:linear-gradient(135deg,#1565C0,#42A5F5)}");
        lines.Add(".mbtnAudio{background:rgba(21,101,192,0.1);color:#1565C0}");
        // Detail sheet
        lines.Add("#sheet{z-index:9600;max-height:75vh;overflow-y:auto;padding-bottom:32px}");
        lines.Add(".siw{margin:0;height:220px;border-radius:0;overflow:hidden;position:relative;background:#152535}");
        lines.Add(".simg{width:100%;height:100%;object-fit:cover}");
        lines.Add(".sph{width:100%;height:100%;display:flex;align-items:center;justify-content:center;font-size:80px}");
        lines.Add(".sOvl{position:absolute;bottom:0;left:0;right:0;height:120px;background:linear-gradient(0deg,rgba(5,15,30,0.95) 0%,rgba(5,15,30,0) 100%)}");
        lines.Add(".sOvlInfo{position:absolute;bottom:0;left:0;right:0;padding:16px 20px 14px}");
        lines.Add(".sbg{display:inline-block;background:rgba(21,101,192,0.85);color:#fff;font-size:10px;font-weight:700;padding:4px 10px;border-radius:20px;margin-bottom:6px;letter-spacing:0.5px}");
        lines.Add(".snm{font-size:22px;font-weight:800;color:#FFF;margin-bottom:4px}");
        lines.Add(".srt{font-size:15px;color:#FFCA28;font-weight:700}");
        lines.Add(".sdv{height:1px;background:rgba(21,101,192,0.1);margin:16px 24px}");
        lines.Add(".sac{display:flex;gap:12px;padding:16px 24px 0}");
        lines.Add(".bcl{width:56px;height:56px;background:rgba(21,101,192,0.1);color:#1565C0;border:none;border-radius:18px;font-size:20px}");
        lines.Add(".bfav{width:56px;height:56px;background:rgba(21,101,192,0.1);color:#1565C0;border:none;border-radius:18px;font-size:24px}");
        lines.Add(".bau{width:56px;height:56px;background:rgba(21,101,192,0.1);color:#1565C0;border:none;border-radius:18px;font-size:22px}");
        lines.Add(".bdr{flex:1;height:56px;background:linear-gradient(90deg,#1565C0,#42A5F5);color:white;border:none;border-radius:18px;font-size:16px;font-weight:700}");
        lines.Add(".sdt{padding:12px 24px 0}");
        lines.Add(".sro{display:flex;gap:16px;padding:10px 0;font-size:14px;color:#0D2137;line-height:1.5}");
        lines.Add(".sic{font-size:18px;width:24px;text-align:center;color:#1565C0}");
        // Nav sheet
        lines.Add("#rs{padding:0 24px 32px;z-index:9000}");
        lines.Add(".rrw{display:flex;align-items:center;gap:16px;margin:12px 0 20px}");
        lines.Add(".ric{width:68px;height:68px;border-radius:18px;overflow:hidden;background:#152535}");
        lines.Add(".ric img{width:100%;height:100%;object-fit:cover}");
        lines.Add(".rnm{font-size:18px;font-weight:700;color:#0D2137}");
        lines.Add(".rsb{font-size:13px;color:#5A7A9A;margin-top:4px}");
        lines.Add(".rst{display:flex;gap:16px;margin-bottom:24px}");
        lines.Add(".rsc{flex:1;background:rgba(21,101,192,0.06);border-radius:20px;padding:18px;text-align:center;border:1px solid rgba(21,101,192,0.12)}");
        lines.Add(".rvl{font-size:24px;font-weight:800;color:#64B5F6}");
        lines.Add(".rll{font-size:12px;color:#5A7A9A;margin-top:4px;text-transform:uppercase;letter-spacing:1px}");
        lines.Add(".ben{width:100%;height:56px;background:rgba(233,30,99,0.15);color:#F06292;border:1px solid rgba(233,30,99,0.3);border-radius:18px;font-size:16px;font-weight:700}");
        lines.Add(".bmin{position:absolute;top:12px;right:16px;background:rgba(21,101,192,0.1);color:#1565C0;border:none;border-radius:16px;padding:6px 14px;font-size:13px;font-weight:700;z-index:2;cursor:pointer}");
        // Proximity alert
        lines.Add("#proximityAlert{position:fixed;top:0;left:0;right:0;bottom:0;z-index:99999;display:none;align-items:flex-end;justify-content:center;padding:0 0 120px;pointer-events:none}");
        lines.Add("#proximityCard{background:rgba(255,255,255,0.98);backdrop-filter:blur(20px);border-radius:24px;margin:0 16px;box-shadow:0 20px 60px rgba(13,33,55,0.3);overflow:hidden;pointer-events:all;width:calc(100% - 32px);max-width:480px;transform:translateY(120px);opacity:0;transition:all 0.45s cubic-bezier(0.2,0.8,0.2,1)}");
        lines.Add("#proximityCard.show{transform:translateY(0);opacity:1}");
        lines.Add(".pcBanner{height:5px;background:linear-gradient(90deg,#1565C0,#42A5F5)}");
        lines.Add(".pcBody{padding:20px 22px 22px;display:flex;gap:16px;align-items:flex-start}");
        lines.Add(".pcIcon{width:56px;height:56px;border-radius:16px;background:linear-gradient(135deg,#E3F2FD,#BBDEFB);display:flex;align-items:center;justify-content:center;font-size:28px;flex-shrink:0}");
        lines.Add(".pcInfo{flex:1;min-width:0}");
        lines.Add(".pcBadge{display:inline-block;background:rgba(21,101,192,0.12);color:#1565C0;font-size:10px;font-weight:700;padding:3px 10px;border-radius:20px;letter-spacing:0.5px;margin-bottom:6px}");
        lines.Add(".pcName{font-size:17px;font-weight:700;color:#0D2137;margin-bottom:4px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}");
        lines.Add(".pcDesc{font-size:12px;color:#5A7A9A;line-height:1.5;display:-webkit-box;-webkit-line-clamp:2;-webkit-box-orient:vertical;overflow:hidden}");
        lines.Add(".pcActions{display:flex;gap:10px;padding:0 22px 20px}");
        lines.Add(".pcDismiss{flex:1;height:44px;background:rgba(21,101,192,0.08);color:#1565C0;border:none;border-radius:14px;font-size:14px;font-weight:600}");
        lines.Add(".pcNav{flex:1;height:44px;background:linear-gradient(90deg,#1565C0,#42A5F5);color:white;border:none;border-radius:14px;font-size:14px;font-weight:700}");
        // Toast
        lines.Add("#tst{position:fixed;top:80px;left:50%;transform:translateX(-50%);background:rgba(21,101,192,0.95);color:white;padding:10px 24px;border-radius:30px;font-size:14px;font-weight:600;display:none;z-index:9999;box-shadow:0 8px 30px rgba(21,101,192,0.4)}");
        lines.Add("</style></head><body>");

        lines.Add("<div id='map'></div>");
        lines.Add("<div id='tst'></div>");
        lines.Add("<div id='proximityAlert'><div id='proximityCard'><div class='pcBanner'></div><div class='pcBody'><div class='pcIcon' id='pcIconEl'>🍽️</div><div class='pcInfo'><div class='pcBadge' id='pcBadgeEl'>GẦN BẠN</div><div class='pcName' id='pcNameEl'></div><div class='pcDesc' id='pcDescEl'></div></div></div><div class='pcActions'><button class='pcDismiss' id='pcBtnClose' onclick='closeProximity()'>Đóng</button><button class='pcNav' id='pcBtnNav' onclick='navFromProximity()'>Chỉ đường</button></div></div></div>");

        // Header: search + language
        lines.Add("<div id='headerRow'>");
        lines.Add("  <div id='searchBar'><span id='btnSearch'>🔍</span><input id='searchInput' type='text' placeholder='Tìm quán ăn...' oninput='onSearch(this.value)' onfocus='showResults()'/><button id='btnClearSearch' onclick='clearSearch()'>✕</button></div>");
        lines.Add($"  <div id='langBtn' onclick='toggleLang()'><span id='currFlag'>{initialFlag}</span>");
        lines.Add("    <div id='langDropdown'>");
        if (_langs.Count > 0)
        {
            foreach (var l in _langs)
                lines.Add($"      <div class='ldItem' onclick='chgL(\"{l.Code}\",event)'>{l.Flag} {l.Name}</div>");
        }
        else
        {
            lines.Add("      <div class='ldItem' onclick='chgL(\"vi\",event)'>🇻🇳 Tiếng Việt</div>");
            lines.Add("      <div class='ldItem' onclick='chgL(\"en\",event)'>🇺🇸 English</div>");
            lines.Add("      <div class='ldItem' onclick='chgL(\"zh\",event)'>🇨🇳 中文</div>");
        }
        lines.Add("    </div>");
        lines.Add("  </div>");
        lines.Add("</div>");
        lines.Add("<div id='searchResults'></div>");

        // Floating buttons
        lines.Add("<button class='fBtn' id='btnMenu' onclick='toggleMenu()'>☰</button>");
        lines.Add("<button class='fBtn' id='btnG' onclick='toU()'>📍</button>");
        lines.Add("<button class='fBtn' id='btnStop' onclick='stopAudio()'>⏹</button>");

        // Menu sheet
        lines.Add("<div class='botSheet' id='menuSheet'><div class='dragBar'></div><div class='mhd' id='txPList'>Danh sách điểm đến</div><div class='mlist' id='menuList'></div></div>");

        // Detail sheet
        lines.Add("<div class='botSheet' id='sheet'>");
        lines.Add("  <div class='dragBar'></div>");
        lines.Add("  <div class='siw'><img id='simg' class='simg' src='' onerror=\"this.style.display='none';document.getElementById('sph').style.display='flex'\"><div id='sph' class='sph' style='display:none'></div><div class='sOvl'></div><div class='sOvlInfo'><div class='sbg' id='txCat'>QUÁN ĂN</div><div class='snm' id='snm'></div><div class='srt' id='srt'></div></div></div>");
        lines.Add("  <div class='sac'><button class='bcl' onclick='closeS()'>✕</button><button class='bfav' onclick='if(cur)toggleFav(cur.id,event)' id='txFavBtn'>🤍</button><button class='bau' onclick='if(cur)speakPoi(cur.id,event)'><svg width='22' height='22' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M3 18v-6a9 9 0 0 1 18 0v6'/><path d='M21 19a2 2 0 0 1-2 2h-1a2 2 0 0 1-2-2v-3a2 2 0 0 1 2-2h3zM3 19a2 2 0 0 0 2 2h1a2 2 0 0 0 2-2v-3a2 2 0 0 0-2-2H3z'/></svg></button><button class='bdr' onclick='reqR()' id='txDir'><svg width='16' height='16' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round' style='vertical-align:middle;margin-right:6px'><polygon points='3 11 22 2 13 21 11 13 3 11'/></svg><span id='txDirTxt'>Chỉ đường</span></button></div>");
        lines.Add("  <div class='sdv'></div>");
        lines.Add("  <div class='sdt'><div class='sro'><span class='sic'>💬</span><span id='sde'></span></div><div class='sro'><span class='sic'>📍</span><span id='sad'></span></div><div class='sro'><span class='sic'>⏰</span><span id='shr'></span></div></div>");
        lines.Add("</div>");

        // Nav sheet
        lines.Add("<div class='botSheet' id='rs'>");
        lines.Add("  <button class='bmin' onclick='toggleNavSheet()' id='txMin'>⬇ Ẩn bớt</button>");
        lines.Add("  <div class='dragBar' onclick='toggleNavSheet()'></div>");
        lines.Add("  <div class='rrw'><div class='ric' id='ricImg'>🍽️</div><div><div class='rnm' id='rnm'></div><div class='rsb' id='txWalk'>🚶 Chỉ đường đi bộ</div></div></div>");
        lines.Add("  <div class='rst'><div class='rsc'><div class='rvl' id='rdst'></div><div class='rll' id='txDist'>KHOẢNG CÁCH</div></div><div class='rsc'><div class='rvl' id='rtim'></div><div class='rll' id='txDur'>THỜI GIAN</div></div></div>");
        lines.Add("  <button class='ben' onclick='endR()' id='txEnd'>✕ Dừng chỉ đường</button>");
        lines.Add("</div>");

        // JavaScript
        lines.Add("<script>");
        lines.Add($"var map,uMk,_routeLayer=null,cur=null,uP=null,cL='{_currentLang}';");
        lines.Add("var ALL=" + data + ";");
        lines.Add("var CTR={lat:10.7615,lng:106.7045};");
        lines.Add("var L={");
        lines.Add("  vi:{search:'Tìm quán ăn...',plist:'Danh sách điểm đến',nodata:'Không tìm thấy quán',cat:'QUÁN ĂN',dir:'Chỉ đường',dist:'KHOẢNG CÁCH',dur:'THỜI GIAN',end:'✕ DỪNG CHỈ ĐƯỜNG',walk:'🚶 Chỉ đường đi bộ',near:'GẦN BẠN',close:'Đóng',min:'Ẩn bớt',exp:'Mở rộng'},");
        lines.Add("  en:{search:'Search spots...',plist:'Destinations',nodata:'No spots found',cat:'RESTAURANT',dir:'Get Directions',dist:'DISTANCE',dur:'DURATION',end:'✕ EXIT NAVIGATION',walk:'🚶 Walking directions',near:'NEAR YOU',close:'Close',min:'Hide',exp:'Expand'},");
        lines.Add("  zh:{search:'搜索餐厅...',plist:'目的地列表',nodata:'未找到餐厅',cat:'餐厅',dir:'获取路线',dist:'距离',dur:'持续时间',end:'✕ 退出导航',walk:'🚶 步行路线',near:'就在附近',close:'关闭',min:'隐藏',exp:'展开'}");
        lines.Add("};");

        // JS functions
        lines.Add("function toggleNavSheet(){var rs=document.getElementById('rs');rs.classList.toggle('minim');document.getElementById('txMin').textContent=(rs.classList.contains('minim')?'⬆ ':'⬇ ')+(rs.classList.contains('minim')?L[cL].exp:L[cL].min);}");
        lines.Add("function toggleLang(){var d=document.getElementById('langDropdown');d.style.display=d.style.display==='flex'?'none':'flex';}");
        lines.Add("function chgL(l,ev){if(ev)ev.stopPropagation();cL=l;var flags={'vi':'🇻🇳','en':'🇺🇸','zh':'🇨🇳'};document.getElementById('currFlag').textContent=flags[l]||'🌐';document.getElementById('langDropdown').style.display='none';applyLang();window.location.href='maui://changelang?lang='+l;}");
        lines.Add("function applyLang(){var t=L[cL]||L.vi;document.getElementById('searchInput').placeholder=t.search;document.getElementById('txPList').textContent=t.plist;document.getElementById('txCat').textContent=t.cat;document.getElementById('txDirTxt').textContent=t.dir;document.getElementById('txDist').textContent=t.dist;document.getElementById('txDur').textContent=t.dur;document.getElementById('txEnd').textContent=t.end;document.getElementById('txWalk').textContent=t.walk;document.getElementById('pcBadgeEl').textContent=t.near;document.getElementById('pcBtnClose').textContent=t.close;document.getElementById('pcBtnNav').textContent=t.dir;var rs=document.getElementById('rs');document.getElementById('txMin').textContent=(rs.classList.contains('minim')?'⬆ ':'⬇ ')+(rs.classList.contains('minim')?t.exp:t.min);}");
        lines.Add("function getPoiName(r){if(cL==='en'&&r.nameEn)return r.nameEn;if(cL==='zh'&&r.nameZh)return r.nameZh;return r.name;}");
        lines.Add("function getPoiDesc(r){if(cL==='en'&&r.descEn)return r.descEn;if(cL==='zh'&&r.descZh)return r.descZh;return r.desc;}");
        lines.Add("function st(r){return r.toFixed(1);}");
        lines.Add("function toast(m){var t=document.getElementById('tst');t.textContent=m;t.style.display='block';setTimeout(function(){t.style.display='none';},2500);}");
        lines.Add("function toU(){if(uP)map.panTo([uP.lat,uP.lng]);else toast((L[cL]||L.vi).search);}");
        lines.Add("function pick(r){cur=r;map.panTo([r.lat,r.lng]);opS(r);}");
        lines.Add("function opS(r){var si=document.getElementById('simg'),sp=document.getElementById('sph');if(r.img){si.src=r.img;si.style.display='block';sp.style.display='none';}else{si.style.display='none';sp.innerHTML='🍽️';sp.style.display='flex';}var nm=getPoiName(r);var de=getPoiDesc(r);document.getElementById('snm').textContent=nm;document.getElementById('srt').innerHTML='⭐ '+st(r.rating);document.getElementById('sde').textContent=de;document.getElementById('sad').textContent=r.addr;document.getElementById('shr').textContent=r.hours;document.getElementById('txFavBtn').innerHTML=r.fav?'❤️':'🤍';document.getElementById('sheet').classList.add('open');}");
        lines.Add("function toggleFav(id,ev){if(ev)ev.stopPropagation();window.location.href='maui://togglefav?id='+id;}");
        lines.Add("function closeS(){document.getElementById('sheet').classList.remove('open');document.getElementById('menuSheet').classList.remove('open');cur=null;}");
        lines.Add("function reqR(){if(!cur)return;if(!uP)toast('Đang lấy vị trí...');window.location.href='maui://routerequested?data='+encodeURIComponent(JSON.stringify({id:cur.id,lat:cur.lat,lng:cur.lng,name:getPoiName(cur)}));}");
        lines.Add("function shwR(n,d,t,img){document.getElementById('rnm').textContent=n;document.getElementById('rdst').textContent=d;document.getElementById('rtim').textContent=t;var ric=document.getElementById('ricImg');ric.innerHTML=img?'<img src=\"'+img+'\">':'🍽️';var rs=document.getElementById('rs');rs.classList.remove('minim');document.getElementById('txMin').textContent='⬇ '+(L[cL]||L.vi).min;rs.classList.add('open');document.getElementById('sheet').classList.remove('open');}");
        lines.Add("function endR(){if(_routeLayer){_routeLayer.remove();_routeLayer=null;}document.getElementById('rs').classList.remove('open');}");
        lines.Add("function drawPremiumRoute(coords,n,d,t,img){try{endR();_routeLayer=L.polyline(coords,{color:'#1A73E8',weight:6,opacity:0.85,lineCap:'round',lineJoin:'round'}).addTo(map);map.fitBounds(_routeLayer.getBounds(),{padding:[80,80]});shwR(n,d,t,img);}catch(e){console.error(e);}}");
        lines.Add("function sPos(lat,lng){uP={lat:lat,lng:lng};if(uMk){uMk.setLatLng([lat,lng]);}else{uMk=L.circleMarker([lat,lng],{radius:10,fillColor:'#42A5F5',fillOpacity:1,color:'white',weight:3}).addTo(map);map.panTo([lat,lng]);}window.location.href='maui://locationupdated?lat='+lat+'&lng='+lng;}");
        lines.Add("function gps(){if(navigator.geolocation){navigator.geolocation.watchPosition(function(p){sPos(p.coords.latitude,p.coords.longitude);},function(){},{enableHighAccuracy:true,timeout:10000,maximumAge:3000});}}");
        lines.Add("function stopAudio(){window.location.href='maui://stopaudio';}");
        lines.Add("function speakPoi(id,ev){if(ev)ev.stopPropagation();window.location.href='maui://speakpoi?id='+id;}");
        lines.Add("function setAudioPlaying(on){document.getElementById('btnStop').style.display=on?'flex':'none';}");
        // Search
        lines.Add("function onSearch(v){document.getElementById('btnClearSearch').style.display=v?'block':'none';if(!v){document.getElementById('searchResults').style.display='none';return;}var res=ALL.filter(function(r){var nm=getPoiName(r).toLowerCase();return nm.indexOf(v.toLowerCase())!==-1;});renderSearchResults(res);}");
        lines.Add("function renderSearchResults(res){var d=document.getElementById('searchResults');if(!res.length){d.innerHTML='<div style=\"padding:16px;text-align:center;color:#8ba0b2\">'+(L[cL]||L.vi).nodata+'</div>';d.style.display='block';return;}var h='';res.forEach(function(r){var nm=getPoiName(r);var imgH=r.img?'<img src=\"'+r.img+'\">':'<div>🍽️</div>';h+='<div class=\"srItem\" onclick=\"selectSR('+r.id+')\"><div class=\"srImg\">'+imgH+'</div><div><div class=\"srName\">'+nm+'</div><div class=\"srAddr\">'+r.addr+'</div></div></div>';});d.innerHTML=h;d.style.display='block';}");
        lines.Add("function selectSR(id){var r=ALL.find(function(x){return x.id===id;});if(!r)return;clearSearch();pick(r);}");
        lines.Add("function showResults(){if(document.getElementById('searchInput').value)document.getElementById('searchResults').style.display='block';}");
        lines.Add("function clearSearch(){document.getElementById('searchInput').value='';document.getElementById('searchResults').style.display='none';document.getElementById('btnClearSearch').style.display='none';}");
        // Menu
        lines.Add("function toggleMenu(){var m=document.getElementById('menuSheet');m.classList.toggle('open');if(m.classList.contains('open'))renderMenu();}");
        lines.Add("function renderMenu(){var h='';var sNav='<svg width=\"20\" height=\"20\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><polygon points=\"3 11 22 2 13 21 11 13 3 11\"></polygon></svg>';var sAud='<svg width=\"20\" height=\"20\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M3 18v-6a9 9 0 0 1 18 0v6\"></path><path d=\"M21 19a2 2 0 0 1-2 2h-1a2 2 0 0 1-2-2v-3a2 2 0 0 1 2-2h3zM3 19a2 2 0 0 0 2 2h1a2 2 0 0 0 2-2v-3a2 2 0 0 0-2-2H3z\"></path></svg>';ALL.forEach(function(r){var nm=getPoiName(r);var imgH=r.img?'<img src=\"'+r.img+'\">':'<div>🍽️</div>';h+='<div class=\"mitem\"><div class=\"mimg\">'+imgH+'</div><div class=\"minfo\"><div class=\"mnm\">'+nm+'</div><div class=\"mrt\">⭐ '+st(r.rating)+'</div><div class=\"mhr\">⏰ '+r.hours+'</div></div><div class=\"mbtns\"><button class=\"mbtn mbtnNav\" onclick=\"menuNav('+r.id+',event)\">'+sNav+'</button><button class=\"mbtn mbtnAudio\" onclick=\"speakPoi('+r.id+',event)\">'+sAud+'</button></div></div>';});document.getElementById('menuList').innerHTML=h;}");
        lines.Add("function menuNav(id,ev){ev.stopPropagation();var r=ALL.find(function(x){return x.id===id;});if(!r)return;cur=r;document.getElementById('menuSheet').classList.remove('open');reqR();}");
        // Proximity
        lines.Add("var _proxCur=null;");
        lines.Add("function showProximity(id,name,desc,img){_proxCur=ALL.find(function(r){return r.id===id;});document.getElementById('pcNameEl').textContent=name;document.getElementById('pcDescEl').textContent=desc;var ic=document.getElementById('pcIconEl');ic.innerHTML=img?'<img src=\"'+img+'\" style=\"width:100%;height:100%;object-fit:cover;border-radius:12px;\">':'🍽️';document.getElementById('proximityAlert').style.display='flex';setTimeout(function(){document.getElementById('proximityCard').classList.add('show');},10);setTimeout(function(){closeProximity();},8000);}");
        lines.Add("function closeProximity(){document.getElementById('proximityCard').classList.remove('show');setTimeout(function(){document.getElementById('proximityAlert').style.display='none';},450);}");
        lines.Add("function navFromProximity(){if(_proxCur){cur=_proxCur;closeProximity();reqR();}}");
        // Close dropdowns on outside click
        lines.Add("document.addEventListener('click',function(e){var sr=document.getElementById('searchResults');if(sr&&!sr.contains(e.target)&&e.target.id!=='searchInput')sr.style.display='none';var lb=document.getElementById('langBtn');var ld=document.getElementById('langDropdown');if(ld&&!lb.contains(e.target))ld.style.display='none';});");
        lines.Add("function initMap(){try{");
        lines.Add("  var mapDiv=document.getElementById('map');if(!mapDiv)return;");
        lines.Add("  map=L.map(mapDiv,{center:[CTR.lat,CTR.lng],zoom:16,zoomControl:false,attributionControl:false,doubleClickZoom:false});");
        lines.Add("  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',{maxZoom:19}).addTo(map);");
        lines.Add("  L.control.zoom({position:'bottomleft'}).addTo(map);");
        lines.Add("  ALL.forEach(function(r){var mk=L.circleMarker([r.lat,r.lng],{radius:10,fillColor:'#1565C0',fillOpacity:0.9,color:'white',weight:2});mk.addTo(map);mk.on('click',function(e){L.DomEvent.stopPropagation(e);pick(r);});});");
        lines.Add("  map.on('click',function(){closeS();});");
        lines.Add("  map.on('dblclick',function(e){sPos(e.latlng.lat,e.latlng.lng);toast('📍');});");
        lines.Add("  setTimeout(function(){map.invalidateSize();map.setView([CTR.lat,CTR.lng],16);},300);");
        lines.Add("  setTimeout(gps,800);");
        lines.Add("  applyLang();");
        lines.Add("  window.location.href='maui://mapready';");
        lines.Add("}catch(e){console.error('[Map]',e);window.location.href='maui://mapready';}}");
        lines.Add("</script>");

        lines.Add("<script src='leaflet.js'></script>");
        lines.Add("<script>window.addEventListener('load',function(){setTimeout(initMap,200);});</script>");
        lines.Add("</body></html>");

        return string.Join("\n", lines);
    }
}

// ── Route request DTO ─────────────────────────────────────────────────────
file class RouteRequest
{
    public int Id { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string Name { get; set; } = string.Empty;
}
