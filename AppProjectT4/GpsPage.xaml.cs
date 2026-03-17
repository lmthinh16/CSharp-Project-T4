using System;
using System.Threading;
using System.Threading.Tasks;
using AppProjectT4.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Graphics;

namespace AppProjectT4
{
    public partial class GpsPage : ContentPage
    {
        private bool _isTracking = false;
        private CancellationTokenSource? _cancelTokenSource;
        private GeofencingService _geofencing = new GeofencingService();
        private string? _lastAlertedName;
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
                // Start the tracking loop in background so StartTracking returns immediately
                _ = TrackLocationLoop(_cancelTokenSource.Token);
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
            try
            {
                _cancelTokenSource?.Dispose();
            }
            catch { }
            _cancelTokenSource = null;
            BtnStartStop.Text = "Bắt đầu theo dõi";
            BtnStartStop.BackgroundColor = Colors.Green;
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
                    }, token);

                    if (location != null)
                    {
                        // Do work (including checking nearby restaurants) off the UI thread,
                        // then update UI on the main thread.
                        var nearest = await _geofencing.CheckNearbyRestaurant(
                            location.Latitude, location.Longitude);

                        double? distance = null;
                        if (nearest != null)
                        {
                            distance = _geofencing.CalculateDistance(
                                location.Latitude, location.Longitude,
                                nearest.Latitude, nearest.Longitude);
                        }

                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            LblLatitude.Text = $"Latitude: {location.Latitude:F6}";
                            LblLongitude.Text = $"Longitude: {location.Longitude:F6}";
                            LblAccuracy.Text = $"Độ chính xác: {location.Accuracy:F1}m";

                            if (nearest != null && distance.HasValue)
                            {
                                LblNearestName.Text = nearest.Name;
                                LblDistance.Text = $"Khoảng cách: {distance.Value:F1}m";

                                AddLog($"🎯 {nearest.Name} ({distance.Value:F1}m)");

                                // Avoid repeated alerts for the same restaurant
                                if (nearest.Name != _lastAlertedName)
                                {
                                    _lastAlertedName = nearest.Name;
                                    await DisplayAlert("🔔 Đã đến gần!",
                                        $"{nearest.Name}\n{nearest.Description}", "OK");
                                }
                            }
                            else
                            {
                                _lastAlertedName = null;
                                LblNearestName.Text = "Không có nhà hàng gần";
                                LblDistance.Text = "---";
                            }
                        });
                    }

                    await Task.Delay(3000, token);
                }
                catch (OperationCanceledException)
                {
                    AddLog("🔁 Tracking canceled");
                    break;
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
            try
            {
                MainThread.BeginInvokeOnMainThread(() => LblLog.Text = _logText);
            }
            catch
            {
                // Ignore UI update failures
            }
        }
    }
}