using Microsoft.Maui.Controls.Shapes;
using ProjectApp.Models;
using ProjectApp.Services;

namespace ProjectApp.Pages
{
    /// <summary>
    /// Lịch sử đặt chỗ — hiển thị tất cả booking, cho phép hủy.
    /// </summary>
    public class BookingHistoryPage : ContentPage
    {
        private readonly string _lang;
        private VerticalStackLayout _listContainer = null!;

        public BookingHistoryPage()
        {
            _lang = UserSession.Language;
            BackgroundColor = Color.FromArgb("#F8FAFC");
            NavigationPage.SetHasNavigationBar(this, false);
            BuildUI();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadBookingsAsync();
        }

        private void BuildUI()
        {
            var root = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Star)
                }
            };

            // Header
            var header = new Grid { HeightRequest = 100, BackgroundColor = Color.FromArgb("#1565C0") };
            var backBtn = new Border
            {
                BackgroundColor = Color.FromArgb("#30FFFFFF"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 22 },
                HeightRequest = 44, WidthRequest = 44,
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions   = LayoutOptions.Center,
                Margin = new Thickness(20, 28, 0, 0)
            };
            backBtn.Content = new Label
            {
                Text = "←", TextColor = Colors.White, FontSize = 22,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions   = LayoutOptions.Center
            };
            backBtn.GestureRecognizers.Add(new TapGestureRecognizer
                { Command = new Command(async () => await Navigation.PopAsync()) });
 
            header.Add(new Label
            {
                Text = L("📋 Đặt chỗ của tôi", "📋 My Bookings", "📋 我的预约"),
                FontSize = 18, FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions   = LayoutOptions.Center,
                Margin = new Thickness(0, 28, 0, 0)
            });

            root.Add(header, 0, 0);

            var scroll = new ScrollView { VerticalScrollBarVisibility = ScrollBarVisibility.Never };
            _listContainer = new VerticalStackLayout { Padding = new Thickness(20, 16), Spacing = 14 };
            scroll.Content = _listContainer;
            root.Add(scroll, 0, 1);

            Content = root;
        }

        private async Task LoadBookingsAsync()
        {
            _listContainer.Clear();
            var bookings = await App.Database.GetAllBookingsAsync();

            if (bookings.Count == 0)
            {
                _listContainer.Add(new Label
                {
                    Text = L("Chưa có đặt chỗ nào.\nHãy ghé thăm nhà hàng và đặt chỗ!",
                             "No bookings yet.\nVisit a restaurant and book a table!",
                             "暂无预约。\n前往餐厅进行预约！"),
                    FontSize = 15, TextColor = Color.FromArgb("#94A3B8"),
                    HorizontalOptions = LayoutOptions.Center,
                    HorizontalTextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 60)
                });
                return;
            }

            foreach (var b in bookings)
                _listContainer.Add(BuildBookingCard(b));
        }

        private View BuildBookingCard(Booking b)
        {
            var card = new Border
            {
                BackgroundColor = Colors.White,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Padding = new Thickness(18, 16),
                Shadow = new Shadow { Brush = Color.FromArgb("#000000"), Opacity = 0.06f, Radius = 8, Offset = new Point(0, 2) }
            };
            var content = new VerticalStackLayout { Spacing = 10 };

            // Top row: tên + badge trạng thái
            var topRow = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                }
            };
            topRow.Add(new Label
            {
                Text = b.RestaurantName, FontSize = 16, FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#0D2137"), VerticalOptions = LayoutOptions.Center
            }, 0, 0);

            var statusBadge = new Border
            {
                BackgroundColor = b.Status switch
                {
                    "confirmed" => Color.FromArgb("#E8F5E9"),
                    "cancelled" => Color.FromArgb("#FFEBEE"),
                    "completed" => Color.FromArgb("#E3F2FD"),
                    _           => Color.FromArgb("#FFF9E6")
                },
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 8 },
                Padding = new Thickness(8, 4)
            };
            statusBadge.Content = new Label
            {
                Text = b.StatusDisplay, FontSize = 11, FontAttributes = FontAttributes.Bold,
                TextColor = b.Status switch
                {
                    "confirmed" => Color.FromArgb("#2E7D32"),
                    "cancelled" => Color.FromArgb("#C62828"),
                    "completed" => Color.FromArgb("#1565C0"),
                    _           => Color.FromArgb("#E65100")
                }
            };
            topRow.Add(statusBadge, 1, 0);
            content.Add(topRow);

            // Booking code
            content.Add(new Label
            {
                Text = b.BookingCode, FontSize = 13, FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#1565C0"), CharacterSpacing = 1
            });

            // Info
            content.Add(new Label
            {
                Text = $"📅 {b.BookingDate}  ⏰ {b.BookingTime}  👥 {b.GuestCount} {L("khách", "guests", "位")}",
                FontSize = 13, TextColor = Color.FromArgb("#475569")
            });
            content.Add(new Label
            {
                Text = b.PaymentDisplay + (b.DepositAmount > 0 ? $"  •  {b.DepositAmount:N0}đ" : ""),
                FontSize = 13, TextColor = Color.FromArgb("#64748B")
            });

            // Sync badge
            if (b.SyncStatus == "pending")
                content.Add(new Label
                {
                    Text = L("📡 Chưa đồng bộ", "📡 Not synced", "📡 未同步"),
                    FontSize = 11, TextColor = Color.FromArgb("#E65100")
                });

            // Nút hủy (chỉ nếu đang confirmed)
            if (b.Status == "confirmed")
            {
                var cancelBtn = new Border
                {
                    BackgroundColor = Color.FromArgb("#FFEBEE"),
                    StrokeThickness = 0,
                    StrokeShape = new RoundRectangle { CornerRadius = 10 },
                    Padding = new Thickness(12, 8),
                    HorizontalOptions = LayoutOptions.Start
                };
                cancelBtn.Content = new Label
                {
                    Text = L("❌ Hủy đặt chỗ", "❌ Cancel", "❌ 取消预约"),
                    FontSize = 13, TextColor = Color.FromArgb("#C62828"), FontAttributes = FontAttributes.Bold
                };
                var capturedId = b.Id;
                cancelBtn.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    Command = new Command(async () =>
                    {
                        bool confirm = await DisplayAlert(
                            L("Xác nhận hủy", "Confirm Cancel", "确认取消"),
                            L("Bạn có chắc muốn hủy đặt chỗ này?", "Are you sure you want to cancel?", "确定要取消此预约吗？"),
                            L("Hủy đặt chỗ", "Cancel", "取消"),
                            L("Giữ lại", "Keep", "保留"));
                        if (confirm)
                        {
                            await PaymentService.Instance.CancelBookingAsync(capturedId);
                            await LoadBookingsAsync();
                        }
                    })
                });
                content.Add(cancelBtn);
            }

            card.Content = content;
            return card;
        }

        private string L(string vi, string en, string zh) => _lang switch { "en" => en, "zh" => zh, _ => vi };
    }
}
