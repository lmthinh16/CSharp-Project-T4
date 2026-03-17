using ProjectApp.Services;

namespace ProjectApp
{
    public partial class GpsPage : ContentPage
    {
        private bool _isTracking = false;
        private CancellationTokenSource? _cancelTokenSource;
        private GeofencingService _geofencing = new GeofencingService();
        private string _logText = "";

        public GpsPage()
        {
            InitializeComponent();
        }

        private async void OnStartStopClicked(object sender, EventArgs e)
        {
            if (_isTracking)
            {
                StopTracking();
            }
            else
            {
                await StartTracking();
            }
        }

        private async Task StartTracking()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }

                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlert("Lỗi", "Cần cấp quyền GPS", "OK");
                    return;
                }

                _isTracking = true;
                BtnStartStop.Text = "Dừng theo dõi";
                BtnStartStop.BackgroundColor = Colors.Red;

                AddLog("✅ Bắt đầu tracking...");

                _cancelTokenSource = new CancellationTokenSource();
                await TrackLocationLoop(_cancelTokenSource.Token);
            }
            catch (Exception ex)
            {
                AddLog($"❌ Lỗi: {ex.Message}");
            }
        }

        private void StopTracking()
        {
            _isTracking = false;
            _cancelTokenSource?.Cancel();
            BtnStartStop.Text = "Bắt đầu theo dõi";
            BtnStartStop.BackgroundColor = Color.FromArgb("#4CAF50");
            AddLog("⏸️ Đã dừng");
        }

        private async Task TrackLocationLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested && _isTracking)
            {
                try
                {
                    var location = await Geolocation.GetLocationAsync(new GeolocationRequest
                    {
                        DesiredAccuracy = GeolocationAccuracy.Best,
                        Timeout = TimeSpan.FromSeconds(10)
                    });

                    if (location != null)
                    {
                        LblLatitude.Text = $"Latitude: {location.Latitude:F6}";
                        LblLongitude.Text = $"Longitude: {location.Longitude:F6}";
                        LblAccuracy.Text = $"Độ chính xác: {location.Accuracy:F1}m";

                        var nearest = await _geofencing.CheckNearbyRestaurant(
                            location.Latitude, location.Longitude);

                        if (nearest != null)
                        {
                            double distance = _geofencing.CalculateDistance(
                                location.Latitude, location.Longitude,
                                nearest.Latitude, nearest.Longitude);

                            LblNearestName.Text = nearest.Name;
                            LblDistance.Text = $"Khoảng cách: {distance:F1}m";

                            AddLog($"🎯 {nearest.Name} ({distance:F1}m)");

                            await DisplayAlert("🔔 Đã đến gần!",
                                $"{nearest.Name}\n{nearest.Description}", "OK");
                        }
                        else
                        {
                            LblNearestName.Text = "Không có nhà hàng gần";
                            LblDistance.Text = "---";
                        }
                    }

                    await Task.Delay(3000, token);
                }
                catch (Exception ex)
                {
                    AddLog($"⚠️ {ex.Message}");
                }
            }
        }

        private void AddLog(string message)
        {
            _logText = $"[{DateTime.Now:HH:mm:ss}] {message}\n{_logText}";
            LblLog.Text = _logText;
        }
    }
}