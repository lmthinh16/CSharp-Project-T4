using ProjectApp.Models;

namespace ProjectApp.Services
{
    /// <summary>
    /// Quản lý trạng thái mạng và đồng bộ dữ liệu offline → online.
    /// Khi online trở lại: tự sync bookings + làm mới danh sách nhà hàng.
    /// </summary>
    public class OfflineService
    {
        private static OfflineService? _instance;
        public static OfflineService Instance => _instance ??= new OfflineService();

        // ── Trạng thái mạng ───────────────────────────────────────────────────

        /// <summary>Có kết nối internet (theo hệ thống)</summary>
        public bool IsOnline { get; private set; }

        /// <summary>Phát ra khi trạng thái kết nối thay đổi</summary>
        public event Action<bool>? StatusChanged;

        private OfflineService()
        {
            IsOnline = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
            Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
        }

        // ── Kết nối thay đổi ─────────────────────────────────────────────────

        private async void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
        {
            bool wasOnline = IsOnline;
            IsOnline = e.NetworkAccess == NetworkAccess.Internet;

            System.Diagnostics.Debug.WriteLine(
                $"[OfflineService] Network: {(IsOnline ? "ONLINE" : "OFFLINE")}");

            StatusChanged?.Invoke(IsOnline);

            if (!wasOnline && IsOnline)
            {
                // Vừa có mạng lại → sync pending bookings
                await SyncPendingBookingsAsync();
            }
        }

        // ── Đồng bộ Bookings ─────────────────────────────────────────────────

        public async Task SyncPendingBookingsAsync()
        {
            if (!IsOnline) return;

            try
            {
                var pending = await App.Database.GetPendingBookingsAsync();
                if (pending.Count == 0) return;

                System.Diagnostics.Debug.WriteLine(
                    $"[OfflineService] Syncing {pending.Count} pending bookings...");

                foreach (var booking in pending)
                {
                    bool ok = await App.Api.PostBookingAsync(booking);
                    if (ok)
                    {
                        booking.SyncStatus = "synced";
                        await App.Database.UpdateBookingAsync(booking);
                        System.Diagnostics.Debug.WriteLine(
                            $"[OfflineService] ✅ Synced booking {booking.BookingCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OfflineService] SyncBookings: {ex.Message}");
            }
        }

        public void Dispose()
        {
            Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;
        }
    }
}
