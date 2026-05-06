using Microsoft.Maui.Controls.Shapes;
using ProjectApp.Models;

namespace ProjectApp.Pages
{
    /// <summary>
    /// Trang xác nhận đặt chỗ thành công — hiển thị mã booking, thông tin chi tiết.
    /// </summary>
    public class BookingConfirmationPage : ContentPage
    {
        private readonly Booking _booking;
        private readonly string _lang;

        public BookingConfirmationPage(Booking booking)
        {
            _booking = booking;
            _lang    = Services.UserSession.Language;
            BackgroundColor = Color.FromArgb("#F8FAFC");
            NavigationPage.SetHasNavigationBar(this, false);
            BuildUI();
        }

        private void BuildUI()
        {
            var scroll = new ScrollView { VerticalScrollBarVisibility = ScrollBarVisibility.Never };
            var root = new VerticalStackLayout { Padding = new Thickness(24, 60, 24, 40), Spacing = 20 };

            // Success icon
            root.Add(new Border
            {
                BackgroundColor = Color.FromArgb("#E8F5E9"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 50 },
                HeightRequest = 100, WidthRequest = 100,
                HorizontalOptions = LayoutOptions.Center,
                Content = new Label { Text = "✅", FontSize = 48, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
            });

            root.Add(new Label
            {
                Text = L("Đặt chỗ thành công!", "Booking Confirmed!", "预约成功！"),
                FontSize = 26, FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#0D2137"),
                HorizontalOptions = LayoutOptions.Center
            });

            root.Add(new Label
            {
                Text = L("Chúng tôi sẽ liên hệ xác nhận qua điện thoại của bạn.",
                         "We will confirm your booking via phone.", "我们将通过电话确认您的预约。"),
                FontSize = 14, TextColor = Color.FromArgb("#64748B"),
                HorizontalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center
            });

            // Ticket card
            var ticket = new Border
            {
                BackgroundColor = Colors.White,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 20 },
                Padding = new Thickness(24),
                Shadow = new Shadow { Brush = Color.FromArgb("#000000"), Opacity = 0.08f, Radius = 15, Offset = new Point(0, 4) }
            };
            var ticketContent = new VerticalStackLayout { Spacing = 0 };

            // Header ticket
            var ticketHeader = new Grid
            {
                BackgroundColor = Color.FromArgb("#EFF6FF"),
                Padding = new Thickness(24, 20),
                Margin = new Thickness(-24, -24, -24, 0)
            };
            ticketHeader.Add(new VerticalStackLayout
            {
                Spacing = 2,
                Children =
                {
                    new Label { Text = _booking.RestaurantName, FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1565C0") },
                    new Label { Text = L("Vĩnh Khánh, Quận 4, TP.HCM", "Vinh Khanh, District 4, HCMC", "第4郡永庆区"), FontSize = 12, TextColor = Color.FromArgb("#64748B") }
                }
            });
            ticketContent.Add(ticketHeader);

            // Divider
            ticketContent.Add(new BoxView { HeightRequest = 1, BackgroundColor = Color.FromArgb("#E2E8F0"), Margin = new Thickness(0, 20) });

            // Booking code (nổi bật)
            ticketContent.Add(new Border
            {
                BackgroundColor = Color.FromArgb("#1565C0"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 10 },
                Padding = new Thickness(16, 10),
                Margin = new Thickness(0, 0, 0, 20),
                HorizontalOptions = LayoutOptions.Center,
                Content = new Label
                {
                    Text = _booking.BookingCode, FontSize = 22, FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White, CharacterSpacing = 2,
                    HorizontalOptions = LayoutOptions.Center
                }
            });

            // Info rows
            ticketContent.Add(InfoRow("👤", L("Khách", "Guest", "顾客"), _booking.CustomerName));
            ticketContent.Add(InfoRow("📱", L("Điện thoại", "Phone", "电话"), _booking.CustomerPhone));
            ticketContent.Add(InfoRow("👥", L("Số khách", "Guests", "人数"), $"{_booking.GuestCount} {L("người", "guests", "位")}"));
            ticketContent.Add(InfoRow("📅", L("Ngày", "Date", "日期"), _booking.BookingDate));
            ticketContent.Add(InfoRow("⏰", L("Giờ", "Time", "时间"), _booking.BookingTime));
            ticketContent.Add(InfoRow("💳", L("Thanh toán", "Payment", "付款"), _booking.PaymentDisplay));

            if (_booking.DepositAmount > 0)
                ticketContent.Add(InfoRow("💰", L("Đã cọc", "Deposit", "押金"), $"{_booking.DepositAmount:N0}đ"));

            if (!string.IsNullOrWhiteSpace(_booking.Note))
                ticketContent.Add(InfoRow("📝", L("Ghi chú", "Note", "备注"), _booking.Note));

            // Offline badge
            if (_booking.SyncStatus == "pending")
            {
                ticketContent.Add(new BoxView { HeightRequest = 1, BackgroundColor = Color.FromArgb("#E2E8F0"), Margin = new Thickness(0, 16, 0, 12) });
                ticketContent.Add(new Border
                {
                    BackgroundColor = Color.FromArgb("#FFF8E1"),
                    StrokeThickness = 0,
                    StrokeShape = new RoundRectangle { CornerRadius = 8 },
                    Padding = new Thickness(12, 8),
                    Content = new Label
                    {
                        Text = L("📡 Chưa đồng bộ (offline) — sẽ tự động gửi khi có mạng",
                                 "📡 Not synced (offline) — will send when connected", "📡 未同步（离线）— 联网后自动发送"),
                        FontSize = 12, TextColor = Color.FromArgb("#E65100")
                    }
                });
            }

            ticket.Content = ticketContent;
            root.Add(ticket);

            // Buttons
            root.Add(CreateBtn(
                L("🏠  VỀ TRANG CHỦ", "🏠  HOME", "🏠  主页"),
                Color.FromArgb("#1565C0"), Colors.White,
                async () => await Navigation.PopToRootAsync()));

            root.Add(CreateBtn(
                L("📋  XEM ĐẶT CHỖ CỦA TÔI", "📋  MY BOOKINGS", "📋  我的预约"),
                Color.FromArgb("#F1F5F9"), Color.FromArgb("#1565C0"),
                async () => await Navigation.PushAsync(new BookingHistoryPage())));

            scroll.Content = root;
            Content = scroll;
        }

        private View InfoRow(string icon, string label, string value)
        {
            var row = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                },
                Margin = new Thickness(0, 5)
            };
            row.Add(new Label { Text = icon, FontSize = 16, VerticalOptions = LayoutOptions.Center, Margin = new Thickness(0, 0, 8, 0) }, 0, 0);
            row.Add(new Label { Text = label, FontSize = 13, TextColor = Color.FromArgb("#64748B"), VerticalOptions = LayoutOptions.Center }, 1, 0);
            row.Add(new Label { Text = value, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0D2137"), HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.Center }, 2, 0);
            return row;
        }

        private Border CreateBtn(string text, Color bg, Color fg, Func<Task> action)
        {
            var btn = new Border
            {
                BackgroundColor = bg, HeightRequest = 52,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 16 }
            };
            btn.Content = new Label
            {
                Text = text, TextColor = fg,
                FontAttributes = FontAttributes.Bold, FontSize = 14, CharacterSpacing = 0.5,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions   = LayoutOptions.Center
            };
            btn.GestureRecognizers.Add(new TapGestureRecognizer
                { Command = new Command(async () => await action()) });
            return btn;
        }

        private string L(string vi, string en, string zh) => _lang switch { "en" => en, "zh" => zh, _ => vi };
    }
}
