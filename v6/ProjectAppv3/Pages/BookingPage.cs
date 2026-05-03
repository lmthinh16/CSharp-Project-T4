using Microsoft.Maui.Controls.Shapes;
using ProjectApp.Models;
using ProjectApp.Services;

namespace ProjectApp.Pages
{
    /// <summary>
    /// Trang đặt chỗ + chọn phương thức thanh toán.
    ///
    /// LUỒNG:
    ///   - Cash       : Xác nhận ngay → BookingConfirmationPage
    ///   - Ví điện tử : Xác nhận → EWalletPaymentPage (QR + đếm ngược 15 phút)
    ///   - Offline    : Khoá ví điện tử, chỉ cho phép Cash
    /// </summary>
    public class BookingPage : ContentPage
    {
        private readonly Restaurant _restaurant;
        private readonly string _lang;

        // Form fields
        private Entry _nameEntry = null!;
        private Entry _phoneEntry = null!;
        private Stepper _guestStepper = null!;
        private Label _guestLabel = null!;
        private DatePicker _datePicker = null!;
        private TimePicker _timePicker = null!;
        private Editor _noteEditor = null!;
        private string _selectedPayment = "cash";
        private Label _paymentInfoLabel = null!;

        private readonly Dictionary<string, Border> _paymentCards = [];

        private const double DEPOSIT_AMOUNT = 50_000; // 50,000đ cọc cố định

        public BookingPage(Restaurant restaurant)
        {
            _restaurant = restaurant;
            _lang = UserSession.Language;
            BackgroundColor = Color.FromArgb("#F8FAFC");
            NavigationPage.SetHasNavigationBar(this, false);
            BuildUI();
        }

        // ── Build UI ──────────────────────────────────────────────────────────

        private void BuildUI()
        {
            var scroll = new ScrollView { VerticalScrollBarVisibility = ScrollBarVisibility.Never };
            var root = new VerticalStackLayout { Spacing = 0 };

            root.Add(BuildHeader());

            var form = new VerticalStackLayout
            {
                Padding = new Thickness(20, 24, 20, 40),
                Spacing = 16,
                TranslationY = -20
            };

            // Banner offline
            if (!OfflineService.Instance.IsOnline)
                form.Add(BuildOfflineBanner());

            form.Add(SectionCard(L("👤 Thông tin khách", "👤 Guest Info", "👤 顾客信息"), BuildGuestInfoSection()));
            form.Add(SectionCard(L("👥 Số lượng khách", "👥 Guest Count", "👥 人数"), BuildGuestCountSection()));
            form.Add(SectionCard(L("📅 Ngày & Giờ", "📅 Date & Time", "📅 日期时间"), BuildDateTimeSection()));

            _noteEditor = new Editor
            {
                Placeholder = L("Yêu cầu đặc biệt (tuỳ chọn)", "Special requests (optional)", "特殊要求（可选）"),
                HeightRequest = 80,
                BackgroundColor = Colors.Transparent,
                TextColor = Color.FromArgb("#0D2137")
            };
            form.Add(SectionCard(L("📝 Ghi chú", "📝 Notes", "📝 备注"), _noteEditor));
            form.Add(SectionCard(L("💳 Phương thức thanh toán", "💳 Payment Method", "💳 付款方式"), BuildPaymentSection()));

            root.Add(form);
            scroll.Content = root;

            var mainGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition(GridLength.Star),
                    new RowDefinition(GridLength.Auto)
                }
            };
            mainGrid.Add(scroll, 0, 0);
            mainGrid.Add(BuildFooter(), 0, 1);
            Content = mainGrid;
        }

        private View BuildHeader()
        {
            var header = new Grid { HeightRequest = 160 };
            header.Add(new BoxView
            {
                Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint   = new Point(1, 1),
                    GradientStops =
                    [
                        new GradientStop(Color.FromArgb("#1565C0"), 0),
                        new GradientStop(Color.FromArgb("#42A5F5"), 1)
                    ]
                }
            });

            var backBtn = new Border
            {
                BackgroundColor = Color.FromArgb("#30FFFFFF"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 24 },
                Margin = new Thickness(20, 50, 0, 0),
                HeightRequest = 44, WidthRequest = 44,
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions   = LayoutOptions.Start,
                Content = new Label
                {
                    Text = "←", TextColor = Colors.White, FontSize = 22,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions   = LayoutOptions.Center
                }
            };
            backBtn.GestureRecognizers.Add(new TapGestureRecognizer
                { Command = new Command(async () => await Navigation.PopAsync()) });
            header.Add(backBtn);

            var titleStack = new VerticalStackLayout
            {
                VerticalOptions   = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                Spacing = 4
            };
            titleStack.Add(new Label
            {
                Text = L("🍽️  ĐẶT CHỖ", "🍽️  RESERVE", "🍽️  预约"),
                TextColor = Colors.White, FontSize = 20, FontAttributes = FontAttributes.Bold,
                HorizontalOptions = LayoutOptions.Center
            });
            titleStack.Add(new Label
            {
                Text = _restaurant.Name,
                TextColor = Color.FromArgb("#B3E5FC"), FontSize = 13,
                HorizontalOptions = LayoutOptions.Center
            });
            header.Add(titleStack);
            return header;
        }

        private View BuildOfflineBanner()
        {
            return new Border
            {
                BackgroundColor = Color.FromArgb("#FFF3E0"),
                Stroke = Color.FromArgb("#FF9800"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                Padding = new Thickness(14, 12),
                Content = new VerticalStackLayout
                {
                    Spacing = 4,
                    Children =
                    {
                        new Label { Text = L("📡 Bạn đang offline", "📡 You're offline", "📡 您已离线"),
                            FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#E65100") },
                        new Label { Text = L("Chỉ có thể đặt bằng Tiền mặt khi offline. Booking sẽ tự đồng bộ khi có mạng.",
                            "Cash only when offline. Booking will sync when connected.", "离线时仅支持现金。联网后自动同步。"),
                            FontSize = 12, TextColor = Color.FromArgb("#BF360C") }
                    }
                }
            };
        }

        private View BuildGuestInfoSection()
        {
            return new VerticalStackLayout
            {
                Spacing = 14,
                Children =
                {
                    BuildField(L("Họ và tên *", "Full Name *", "姓名 *"), out _nameEntry, Keyboard.Text),
                    BuildField(L("Số điện thoại *", "Phone *", "电话 *"), out _phoneEntry, Keyboard.Telephone)
                }
            };
        }

        private View BuildGuestCountSection()
        {
            _guestLabel = new Label
            {
                Text = $"2 {L("khách", "guests", "位")}",
                FontSize = 15, FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#1565C0"),
                VerticalOptions = LayoutOptions.Center
            };
            _guestStepper = new Stepper { Minimum = 1, Maximum = 20, Increment = 1, Value = 2 };
            _guestStepper.ValueChanged += (s, e) =>
                _guestLabel.Text = $"{(int)e.NewValue} {L("khách", "guests", "位")}";

            var grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                },
                Children = { _guestLabel, _guestStepper }
            };
            Grid.SetColumn(_guestStepper, 1);
            return grid;
        }

        private View BuildDateTimeSection()
        {
            _datePicker = new DatePicker
            {
                MinimumDate = DateTime.Today,
                Date = DateTime.Today.AddDays(1),
                Format = "dd/MM/yyyy",
                TextColor = Color.FromArgb("#0D2137")
            };
            _timePicker = new TimePicker
            {
                Time = new TimeSpan(19, 0, 0),
                TextColor = Color.FromArgb("#0D2137")
            };

            var grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Star)
                },
                ColumnSpacing = 12,
                Children = { _datePicker, _timePicker }
            };
            Grid.SetColumn(_timePicker, 1);
            return grid;
        }

        private View BuildPaymentSection()
        {
            var isOffline = !OfflineService.Instance.IsOnline;

            _paymentInfoLabel = new Label
            {
                Text = L("Thanh toán tại quán, không cần cọc trước",
                         "Pay at venue, no deposit needed", "到店付款，无需预付款"),
                FontSize = 13, TextColor = Color.FromArgb("#64748B")
            };

            var paymentGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Star)
                },
                RowDefinitions =
                {
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Auto)
                },
                ColumnSpacing = 10, RowSpacing = 10
            };

            var paymentInfo = new (string key, string icon, string label)[]
            {
                ("cash",    "💵", L("Tiền mặt", "Cash",    "现金")),
                ("vnpay",   "💳", "VNPay"),
                ("momo",    "🟣", "MoMo"),
                ("zalopay", "🔵", "ZaloPay"),
            };

            for (int i = 0; i < paymentInfo.Length; i++)
            {
                var (key, icon, label) = paymentInfo[i];
                var isEwallet  = key != "cash";
                var isDisabled = isOffline && isEwallet;

                var card = new Border
                {
                    BackgroundColor = key == "cash" ? Color.FromArgb("#EFF6FF") : Colors.White,
                    Stroke          = key == "cash" ? Color.FromArgb("#1565C0") : Color.FromArgb("#E2E8F0"),
                    StrokeThickness = 1.5,
                    StrokeShape = new RoundRectangle { CornerRadius = 12 },
                    Padding = new Thickness(12, 10),
                    Opacity = isDisabled ? 0.4 : 1.0
                };

                var inner = new VerticalStackLayout { Spacing = 2 };
                inner.Add(new Label { Text = icon, FontSize = 22, HorizontalOptions = LayoutOptions.Center });
                inner.Add(new Label
                {
                    Text = label, FontSize = 12, FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#0D2137"), HorizontalOptions = LayoutOptions.Center
                });
                if (isEwallet)
                    inner.Add(new Label
                    {
                        Text = L($"Cọc {DEPOSIT_AMOUNT:N0}đ", $"{DEPOSIT_AMOUNT:N0}đ deposit", $"押金{DEPOSIT_AMOUNT:N0}đ"),
                        FontSize = 10, TextColor = Color.FromArgb("#64748B"),
                        HorizontalOptions = LayoutOptions.Center
                    });
                card.Content = inner;

                if (!isDisabled)
                {
                    var capturedKey = key;
                    card.GestureRecognizers.Add(new TapGestureRecognizer
                        { Command = new Command(() => SelectPayment(capturedKey)) });
                }

                _paymentCards[key] = card;
                paymentGrid.Add(card, i % 2, i / 2);
            }

            var stack = new VerticalStackLayout { Spacing = 10 };
            stack.Add(paymentGrid);
            stack.Add(_paymentInfoLabel);
            if (isOffline)
                stack.Add(new Label
                {
                    Text = L("⚠️ Ví điện tử không khả dụng khi offline",
                             "⚠️ E-wallets require internet connection", "⚠️ 电子钱包需要网络连接"),
                    FontSize = 12, TextColor = Color.FromArgb("#E65100")
                });
            return stack;
        }

        private void SelectPayment(string method)
        {
            if (!OfflineService.Instance.IsOnline && method != "cash") return;

            _selectedPayment = method;

            foreach (var kv in _paymentCards)
            {
                kv.Value.BackgroundColor = kv.Key == method ? Color.FromArgb("#EFF6FF") : Colors.White;
                kv.Value.Stroke          = kv.Key == method ? Color.FromArgb("#1565C0") : Color.FromArgb("#E2E8F0");
            }

            _paymentInfoLabel.Text = method switch
            {
                "vnpay"   => L($"Cọc {DEPOSIT_AMOUNT:N0}đ qua VNPay — quét QR bước tiếp theo",
                               $"{DEPOSIT_AMOUNT:N0}đ deposit via VNPay — scan QR next", "通过VNPay缴纳押金，下一步扫码"),
                "momo"    => L($"Cọc {DEPOSIT_AMOUNT:N0}đ qua MoMo — quét QR bước tiếp theo",
                               $"{DEPOSIT_AMOUNT:N0}đ deposit via MoMo — scan QR next", "通过MoMo缴纳押金，下一步扫码"),
                "zalopay" => L($"Cọc {DEPOSIT_AMOUNT:N0}đ qua ZaloPay — quét QR bước tiếp theo",
                               $"{DEPOSIT_AMOUNT:N0}đ deposit via ZaloPay — scan QR next", "通过ZaloPay缴纳押金，下一步扫码"),
                _ => L("Thanh toán tại quán, không cần cọc trước",
                       "Pay at venue, no deposit needed", "到店付款，无需预付款")
            };
        }

        private View BuildFooter()
        {
            var confirmBtn = new Border
            {
                HeightRequest = 56,
                Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint   = new Point(1, 0),
                    GradientStops =
                    [
                        new GradientStop(Color.FromArgb("#1565C0"), 0),
                        new GradientStop(Color.FromArgb("#42A5F5"), 1)
                    ]
                },
                StrokeShape = new RoundRectangle { CornerRadius = 18 },
                StrokeThickness = 0,
                Shadow = new Shadow { Brush = Color.FromArgb("#1565C0"), Opacity = 0.35f, Radius = 12, Offset = new Point(0, 5) },
                Content = new Label
                {
                    Text = L("✅  XÁC NHẬN ĐẶT CHỖ", "✅  CONFIRM BOOKING", "✅  确认预约"),
                    TextColor = Colors.White, FontAttributes = FontAttributes.Bold,
                    FontSize = 15, CharacterSpacing = 1,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions   = LayoutOptions.Center
                }
            };
            confirmBtn.GestureRecognizers.Add(new TapGestureRecognizer
                { Command = new Command(async () => await OnConfirmAsync()) });

            return new Border
            {
                BackgroundColor = Colors.White,
                Padding = new Thickness(20, 12, 20, 36),
                StrokeThickness = 0,
                Content = confirmBtn
            };
        }

        // ── Confirm Logic ─────────────────────────────────────────────────────

        private async Task OnConfirmAsync()
        {
            if (string.IsNullOrWhiteSpace(_nameEntry.Text))
            {
                await DisplayAlert(
                    L("Thiếu thông tin", "Missing info", "信息缺失"),
                    L("Vui lòng nhập họ tên.", "Please enter your name.", "请输入姓名。"), "OK");
                return;
            }
            if (string.IsNullOrWhiteSpace(_phoneEntry.Text) || _phoneEntry.Text.Length < 9)
            {
                await DisplayAlert(
                    L("Thiếu thông tin", "Missing info", "信息缺失"),
                    L("Vui lòng nhập số điện thoại hợp lệ.", "Please enter a valid phone number.", "请输入有效电话号码。"), "OK");
                return;
            }

            DateTime bookingDateTime = (DateTime)(_datePicker.Date + _timePicker.Time);
            if (bookingDateTime < DateTime.Now.AddMinutes(30))
            {
                await DisplayAlert(
                    L("Thời gian không hợp lệ", "Invalid time", "时间无效"),
                    L("Vui lòng chọn thời gian ít nhất 30 phút từ bây giờ.",
                      "Please choose a time at least 30 minutes from now.", "请选择至少30分钟后的时间。"), "OK");
                return;
            }

            if (!OfflineService.Instance.IsOnline && _selectedPayment != "cash")
            {
                await DisplayAlert(
                    L("Không có mạng", "No internet", "无网络"),
                    L("Ví điện tử cần kết nối internet. Vui lòng chọn Tiền mặt khi offline.",
                      "E-wallets require internet. Please use Cash when offline.", "电子钱包需要网络，请在离线时选择现金。"), "OK");
                return;
            }

            var depositAmount = _selectedPayment == "cash" ? 0 : DEPOSIT_AMOUNT;
            var booking = await PaymentService.Instance.CreateBookingAsync(
                restaurant: _restaurant,
                customerName: _nameEntry.Text.Trim(),
                customerPhone: _phoneEntry.Text.Trim(),
                guestCount: (int)_guestStepper.Value,
                bookingDateTime: bookingDateTime,
                note: _noteEditor.Text?.Trim() ?? "",
                paymentMethod: _selectedPayment,
                depositAmount: depositAmount);

            if (_selectedPayment == "cash")
            {
                await PaymentService.Instance.FinalizeCashBookingAsync(booking);
                await Navigation.PushAsync(new BookingConfirmationPage(booking));
            }
            else
            {
                var ewalletInfo = PaymentService.Instance.GetEWalletInfo(
                    _selectedPayment, DEPOSIT_AMOUNT, booking.BookingCode);
                await Navigation.PushAsync(new EWalletPaymentPage(booking, ewalletInfo));
            }

            // Xoá BookingPage khỏi navigation stack sau khi đã push trang mới
            var stack = Navigation.NavigationStack.ToList();
            if (stack.Count >= 2)
                Navigation.RemovePage(stack[^2]);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private string L(string vi, string en, string zh) => _lang switch
        {
            "en" => en, "zh" => zh, _ => vi
        };

        private Border SectionCard(string title, View content)
        {
            var inner = new VerticalStackLayout { Spacing = 12 };
            inner.Add(new Label
            {
                Text = title, FontSize = 14, FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#475569")
            });
            inner.Add(content);

            return new Border
            {
                BackgroundColor = Colors.White,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Padding = new Thickness(18, 14),
                Shadow = new Shadow { Brush = Color.FromArgb("#000000"), Opacity = 0.05f, Radius = 8, Offset = new Point(0, 2) },
                Content = inner
            };
        }

        private View BuildField(string placeholder, out Entry entry, Keyboard? keyboard = null)
        {
            entry = new Entry
            {
                Placeholder = placeholder,
                Keyboard = keyboard ?? Keyboard.Default,
                BackgroundColor = Color.FromArgb("#F1F5F9"),
                TextColor = Color.FromArgb("#0D2137"),
                HeightRequest = 48
            };
            return new Border
            {
                BackgroundColor = Color.FromArgb("#F1F5F9"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 10 },
                Padding = new Thickness(4, 0),
                Content = entry
            };
        }
    }
}
