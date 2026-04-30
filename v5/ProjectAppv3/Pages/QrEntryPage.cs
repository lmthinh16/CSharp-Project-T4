// Pages/QrEntryPage.cs
// Màn hình cổng vào app qua QR — hiện ra trước LoginPage
//
// LUỒNG:
//   App mở → QrEntryPage (màn hình này)
//     ├─ "QUÉT MÃ QR" → EntryQRScannerPage (camera ZXing)
//     │     └─ Quét vinhkhanhtour://open/guest → LoginAsGuest → AppShell
//     ├─ "Truy cập không cần QR"  → LoginAsGuest → AppShell
//     └─ "Đăng nhập"              → LoginPage (flow đăng nhập thường)
//
// YÊU CẦU:
//   NuGet: ZXing.Net.Maui.Controls 0.4.0
//   MauiProgram.cs: builder.UseBarcodeReader()
//   AndroidManifest.xml: thêm CAMERA permission
//
// ĐỂ BẬT CAMERA THẬT: bỏ comment region "#if USE_CAMERA" bên dưới
// Khi chưa có ZXing: EntryQRScannerPage sẽ dùng nút "DÙNG MÃ MẪU" thay camera.

using Microsoft.Maui.Controls.Shapes;
using ProjectApp.Services;

// Uncomment dòng dưới sau khi thêm package ZXing.Net.Maui.Control

#if USE_CAMERA
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
#endif

namespace ProjectApp.Pages
{
    // ══════════════════════════════════════════════════════════════
    //  MÀN HÌNH CHÍNH — Landing page, chưa dùng camera
    // ══════════════════════════════════════════════════════════════
    public class QrEntryPage : ContentPage
    {
        public QrEntryPage()
        {
            BackgroundColor = Color.FromArgb("#080D14");
            NavigationPage.SetHasNavigationBar(this, false);
            BuildUI();
        }

        private void BuildUI()
        {
            var root = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition(GridLength.Star),
                    new RowDefinition(GridLength.Auto)
                }
            };

            // ── Phần trên: logo + QR frame minh hoạ ──────────────────────
            var top = new VerticalStackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                Padding         = new Thickness(40, 0),
                Spacing         = 0
            };

            // Accent line vàng
            top.Add(new BoxView
            {
                Color              = Color.FromArgb("#C9A84C"),
                HeightRequest      = 2,
                WidthRequest       = 40,
                HorizontalOptions  = LayoutOptions.Start,
                Margin             = new Thickness(0, 0, 0, 28)
            });

            top.Add(new Label
            {
                Text             = "VĨNH KHÁNH",
                FontSize         = 34,
                FontAttributes   = FontAttributes.Bold,
                TextColor        = Colors.White,
                CharacterSpacing = 6,
                Margin           = new Thickness(0, 0, 0, 8)
            });

            top.Add(new Label
            {
                Text             = "Audio Guide · Quận 4",
                FontSize         = 14,
                TextColor        = Color.FromArgb("#C9A84C"),
                CharacterSpacing = 2,
                Margin           = new Thickness(0, 0, 0, 52)
            });

            // Khung QR minh hoạ (animation đường quét)
            top.Add(new Border
            {
                WidthRequest      = 180,
                HeightRequest     = 180,
                HorizontalOptions = LayoutOptions.Start,
                StrokeShape       = new RoundRectangle { CornerRadius = 4 },
                StrokeThickness   = 0,
                BackgroundColor   = Colors.Transparent,
                Margin            = new Thickness(0, 0, 0, 40),
                Content           = BuildQRFramePreview()
            });

            top.Add(new Label
            {
                Text      = "Đưa camera vào mã QR\ntại lối vào để bắt đầu",
                FontSize  = 16,
                TextColor = Color.FromArgb("#8899AA"),
                LineHeight = 1.7
            });

            Grid.SetRow(top, 0);
            root.Add(top);

            // ── Phần dưới: các nút hành động ─────────────────────────────
            var btns = new VerticalStackLayout
            {
                Padding = new Thickness(40, 0, 40, 52),
                Spacing = 0
            };

            // Nút chính — mở camera quét QR
            var scanBtn = CreateBtn(
                text:    "QUÉT MÃ QR ĐỂ VÀO",
                bgColor: "#C9A84C",
                fgColor: "#080D14",
                margin:  new Thickness(0, 0, 0, 14),
                action:  async () => await Navigation.PushAsync(new EntryQRScannerPage()));
            btns.Add(scanBtn);

            // Nút phụ — vào thẳng không cần QR (guest)
            var guestBtn = new Border
            {
                BackgroundColor = Colors.Transparent,
                StrokeShape     = new RoundRectangle { CornerRadius = 3 },
                StrokeThickness = 1,
                Stroke          = Color.FromArgb("#1E2D3F"),
                HeightRequest   = 52,
                HorizontalOptions = LayoutOptions.Fill,
                Margin          = new Thickness(0, 0, 0, 24),
                Content         = new Label
                {
                    Text              = "Truy cập không cần QR",
                    FontSize          = 13,
                    TextColor         = Color.FromArgb("#4A6280"),
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions   = LayoutOptions.Center
                }
            };
            guestBtn.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(EnterAsGuest)
            });
            btns.Add(guestBtn);

            // Link đăng nhập
            var loginRow = new HorizontalStackLayout
            {
                Spacing           = 8,
                HorizontalOptions = LayoutOptions.Center
            };
            loginRow.Add(new Label
            {
                Text            = "Đã có tài khoản?",
                FontSize        = 13,
                TextColor       = Color.FromArgb("#2E3D4D"),
                VerticalOptions = LayoutOptions.Center
            });
            var loginLink = new Label
            {
                Text            = "Đăng nhập",
                FontSize        = 13,
                FontAttributes  = FontAttributes.Bold,
                TextColor       = Color.FromArgb("#C9A84C"),
                VerticalOptions = LayoutOptions.Center
            };
            loginLink.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () =>
                    await Navigation.PushAsync(new LoginPage()))
            });
            loginRow.Add(loginLink);
            btns.Add(loginRow);

            Grid.SetRow(btns, 1);
            root.Add(btns);
            Content = root;
        }

        // ── QR frame tĩnh có animation đường quét ────────────────────────
        private static View BuildQRFramePreview()
        {
            var g    = new Grid { WidthRequest = 180, HeightRequest = 180 };
            var gold = Color.FromArgb("#C9A84C");
            var dim  = Color.FromArgb("#1A2535");

            // Vùng tối bên trong
            g.Add(new Border
            {
                Margin          = new Thickness(24),
                BackgroundColor = dim,
                StrokeThickness = 0,
                StrokeShape     = new RoundRectangle { CornerRadius = 2 }
            });

            // 4 góc vàng
            const int cs = 20, t = 2;
            foreach (var (h, v) in new[]
            {
                (LayoutOptions.Start, LayoutOptions.Start),
                (LayoutOptions.End,   LayoutOptions.Start),
                (LayoutOptions.Start, LayoutOptions.End),
                (LayoutOptions.End,   LayoutOptions.End)
            })
            {
                g.Add(new BoxView { Color = gold, WidthRequest = cs, HeightRequest = t, HorizontalOptions = h, VerticalOptions = v });
                g.Add(new BoxView { Color = gold, WidthRequest = t, HeightRequest = cs, HorizontalOptions = h, VerticalOptions = v });
            }

            // Đường quét animation
            var line = new BoxView
            {
                Color             = gold,
                HeightRequest     = 1.5,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions   = LayoutOptions.Start,
                Margin            = new Thickness(8, 0),
                Opacity           = 0.7
            };
            g.Add(line);
            AnimateScanLine(line, 180);
            return g;
        }

        private static async void AnimateScanLine(BoxView line, double maxY)
        {
            while (true)
            {
                try
                {
                    await line.TranslateTo(0, maxY - 16, 2000, Easing.SinInOut);
                    await line.TranslateTo(0, 8, 2000, Easing.SinInOut);
                }
                catch { break; }
            }
        }

        // ── Helper tạo nút ────────────────────────────────────────────────
        private static Border CreateBtn(string text, string bgColor, string fgColor,
            Thickness margin, Func<Task> action)
        {
            var btn = new Border
            {
                BackgroundColor   = Color.FromArgb(bgColor),
                StrokeThickness   = 0,
                StrokeShape       = new RoundRectangle { CornerRadius = 3 },
                HeightRequest     = 56,
                HorizontalOptions = LayoutOptions.Fill,
                Margin            = margin,
                Content           = new Label
                {
                    Text             = text,
                    FontSize         = 13,
                    FontAttributes   = FontAttributes.Bold,
                    TextColor        = Color.FromArgb(fgColor),
                    CharacterSpacing = 2,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions   = LayoutOptions.Center
                }
            };
            btn.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () => await action())
            });
            return btn;
        }

        // ── Vào app với tư cách guest ─────────────────────────────────────
        private static void EnterAsGuest()
        {
            UserSession.Current.LoginAsGuest();
            Application.Current!.MainPage = new AppShell();
        }
    }


    // ══════════════════════════════════════════════════════════════
    //  MÀN HÌNH CAMERA QUÉT QR
    //  Hai chế độ:
    //    - USE_CAMERA defined  → dùng CameraBarcodeReaderView (ZXing thật)
    //    - USE_CAMERA không có → hiện nút "DÙNG MÃ MẪU" thay camera
    // ══════════════════════════════════════════════════════════════
    public class EntryQRScannerPage : ContentPage
    {
        // Scheme hợp lệ để mở app (chỉ nhận đúng scheme này)
        private const string VALID_SCHEME = "vinhkhanhtour://open/guest";

        // QR mẫu để test — quét ảnh này bằng camera hoặc nhấn nút bên dưới
        private const string SEED_QR_URL =
            "https://api.qrserver.com/v1/create-qr-code/?size=120x120&margin=4" +
            "&data=vinhkhanhtour%3A%2F%2Fopen%2Fguest";

        private bool _isProcessing = false;
        private Grid? _rootGrid;

#if USE_CAMERA
        private ZXing.Net.Maui.Controls.CameraBarcodeReaderView? _cameraView;
#endif

        public EntryQRScannerPage()
        {
            BackgroundColor = Color.FromArgb("#080D14");
            NavigationPage.SetHasNavigationBar(this, false);
            BuildUI();
        }

        private void BuildUI()
        {
            _rootGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition(GridLength.Auto),   // header
                    new RowDefinition(new GridLength(320)), // camera/placeholder
                    new RowDefinition(GridLength.Auto)    // bottom panel
                }
            };

            _rootGrid.Add(BuildHeader(),        0, 0);
            _rootGrid.Add(BuildCameraArea(),    0, 1);
            _rootGrid.Add(BuildBottomPanel(),   0, 2);

            Content = _rootGrid;
        }

        // ── Header ───────────────────────────────────────────────────────
        private View BuildHeader()
        {
            var header = new Grid
            {
                BackgroundColor = Color.FromArgb("#080D14"),
                Padding = new Thickness(24, 54, 24, 20),
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star)
                }
            };

            var backLbl = new Label
            {
                Text            = "←",
                FontSize        = 22,
                TextColor       = Colors.White,
                Padding         = new Thickness(0, 0, 20, 0),
                VerticalOptions = LayoutOptions.Center
            };
            backLbl.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () => await Navigation.PopAsync())
            });
            header.Add(backLbl, 0, 0);

            var titles = new VerticalStackLayout { Spacing = 3, VerticalOptions = LayoutOptions.Center };
            titles.Add(new Label
            {
                Text             = "QUÉT MÃ VÀO APP",
                FontSize         = 15,
                FontAttributes   = FontAttributes.Bold,
                TextColor        = Colors.White,
                CharacterSpacing = 1.5
            });
            titles.Add(new Label
            {
                Text      = "Đặt mã QR tại lối vào vào khung",
                FontSize  = 12,
                TextColor = Color.FromArgb("#3D5268")
            });
            header.Add(titles, 1, 0);
            return header;
        }

        // ── Vùng camera (hoặc placeholder nếu chưa cài ZXing) ────────────
        private View BuildCameraArea()
        {
            var camOuter = new Grid { BackgroundColor = Color.FromArgb("#050A10") };

#if USE_CAMERA
            _cameraView = new ZXing.Net.Maui.Controls.CameraBarcodeReaderView
            {
                Options = new ZXing.Net.Maui.BarcodeReaderOptions
                {
                    Formats    = ZXing.Net.Maui.BarcodeFormat.QrCode,
                    AutoRotate = true,
                    Multiple   = false
                },
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions   = LayoutOptions.Fill,
                IsDetecting       = true
            };
            _cameraView.BarcodesDetected += OnQRDetected;
            camOuter.Add(_cameraView);
#else
            // ── Placeholder khi chưa cài ZXing ──────────────────────
            var placeholder = new VerticalStackLayout
            {
                VerticalOptions   = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                Spacing           = 16
            };
            placeholder.Add(new Label
            {
                Text              = "📷",
                FontSize          = 56,
                HorizontalOptions = LayoutOptions.Center
            });
            placeholder.Add(new Label
            {
                Text                    = "Camera chưa được kích hoạt\nDùng nút bên dưới để test",
                FontSize                = 14,
                TextColor               = Color.FromArgb("#3D5268"),
                HorizontalTextAlignment = TextAlignment.Center,
                LineHeight              = 1.6
            });
            camOuter.Add(placeholder);
#endif
            // Overlay khung quét (4 góc vàng + đường quét)
            camOuter.Add(BuildScanOverlay(Color.FromArgb("#C9A84C")));
            return camOuter;
        }

        // ── Panel dưới: QR mẫu + nút ─────────────────────────────────────
        private View BuildBottomPanel()
        {
            var panel = new VerticalStackLayout
            {
                Padding         = new Thickness(24, 20, 24, 40),
                Spacing         = 16,
                BackgroundColor = Color.FromArgb("#080D14")
            };

            // Card hiển thị QR mẫu
            var seedCard = new Border
            {
                BackgroundColor = Color.FromArgb("#0D1520"),
                StrokeShape     = new RoundRectangle { CornerRadius = 3 },
                StrokeThickness = 1,
                Stroke          = Color.FromArgb("#1A2535"),
                Padding         = new Thickness(16, 14)
            };

            var row = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(new GridLength(80)),
                    new ColumnDefinition(GridLength.Star)
                },
                ColumnSpacing = 16
            };

            // Ảnh QR mẫu
            row.Add(new Border
            {
                BackgroundColor = Colors.White,
                StrokeThickness = 0,
                StrokeShape     = new RoundRectangle { CornerRadius = 2 },
                Padding         = 3,
                Content         = new Image
                {
                    Source        = new UriImageSource { Uri = new Uri(SEED_QR_URL) },
                    WidthRequest  = 74,
                    HeightRequest = 74,
                    Aspect        = Aspect.AspectFit
                }
            }, 0, 0);

            // Text giải thích
            var info = new VerticalStackLayout { Spacing = 4, VerticalOptions = LayoutOptions.Center };
            info.Add(new Label
            {
                Text           = "Mã QR mẫu",
                FontSize       = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor      = Colors.White
            });
            info.Add(new Label
            {
                Text       = "Quét mã này để vào app\nmà không cần in QR thật",
                FontSize   = 12,
                TextColor  = Color.FromArgb("#3D5268"),
                LineHeight = 1.5
            });
            row.Add(info, 1, 0);
            seedCard.Content = row;
            panel.Add(seedCard);

            // Nút dùng QR mẫu (simulate quét thành công)
            var seedBtn = new Border
            {
                BackgroundColor   = Color.FromArgb("#C9A84C"),
                StrokeThickness   = 0,
                StrokeShape       = new RoundRectangle { CornerRadius = 3 },
                HeightRequest     = 52,
                HorizontalOptions = LayoutOptions.Fill,
                Content           = new Label
                {
                    Text             = "DÙNG MÃ QR MẪU",
                    FontSize         = 13,
                    FontAttributes   = FontAttributes.Bold,
                    TextColor        = Color.FromArgb("#080D14"),
                    CharacterSpacing = 1.5,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions   = LayoutOptions.Center
                }
            };
            seedBtn.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () => await HandleValidQRAsync())
            });
            panel.Add(seedBtn);

            return panel;
        }

        // ── Overlay khung quét (4 góc vàng + scan line) ──────────────────
        private static View BuildScanOverlay(Color accent)
        {
            var overlay = new Grid
            {
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions   = LayoutOptions.Fill
            };

            var frame = new Grid
            {
                WidthRequest      = 220,
                HeightRequest     = 220,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions   = LayoutOptions.Center
            };

            foreach (var (h, v) in new[]
            {
                (LayoutOptions.Start, LayoutOptions.Start),
                (LayoutOptions.End,   LayoutOptions.Start),
                (LayoutOptions.Start, LayoutOptions.End),
                (LayoutOptions.End,   LayoutOptions.End)
            })
            {
                frame.Add(new BoxView { Color = accent, WidthRequest = 28, HeightRequest = 2, HorizontalOptions = h, VerticalOptions = v });
                frame.Add(new BoxView { Color = accent, WidthRequest = 2,  HeightRequest = 28, HorizontalOptions = h, VerticalOptions = v });
            }

            var scanLine = new BoxView
            {
                Color             = accent,
                HeightRequest     = 1.5,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions   = LayoutOptions.Start,
                Margin            = new Thickness(4, 0),
                Opacity           = 0.8
            };
            frame.Add(scanLine);
            AnimateScanLine(scanLine);
            overlay.Add(frame);
            return overlay;
        }

        private static async void AnimateScanLine(BoxView line)
        {
            while (true)
            {
                try
                {
                    await line.TranslateTo(0, 218, 1800, Easing.SinInOut);
                    await line.TranslateTo(0, 0,   1800, Easing.SinInOut);
                    await Task.Delay(80);
                }
                catch { break; }
            }
        }

#if USE_CAMERA
        // ── ZXing callback khi camera phát hiện QR ───────────────────────
        private void OnQRDetected(object? sender, ZXing.Net.Maui.BarcodeDetectionEventArgs e)
        {
            if (_isProcessing) return;
            _isProcessing = true;

            var value = e.Results?.FirstOrDefault()?.Value;
            if (string.IsNullOrWhiteSpace(value)) { _isProcessing = false; return; }

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (_cameraView != null) _cameraView.IsDetecting = false;

                if (value.StartsWith("vinhkhanhtour://", StringComparison.OrdinalIgnoreCase))
                {
                    await FlashSuccessAsync();
                    await HandleValidQRAsync();
                }
                else
                {
                    await DisplayAlert(
                        "Mã không hợp lệ",
                        "Đây không phải mã QR của VinhKhanhTour.\nVui lòng thử lại.",
                        "Thử lại");
                    _isProcessing = false;
                    if (_cameraView != null) _cameraView.IsDetecting = true;
                }
            });
        }
#endif

        // ── Xử lý QR hợp lệ → vào app ───────────────────────────────────
        private async Task HandleValidQRAsync()
        {
            if (_isProcessing && _rootGrid != null)
            {
                // Đã đang xử lý (camera callback) — chỉ navigate
            }
            _isProcessing = true;

            await FlashSuccessAsync();

            UserSession.Current.LoginAsGuest();
            Application.Current!.MainPage = new AppShell();
        }

        // ── Flash vàng xác nhận thành công ───────────────────────────────
        private async Task FlashSuccessAsync()
        {
            if (_rootGrid == null) return;

            var flash = new BoxView
            {
                Color   = Color.FromArgb("#C9A84C"),
                Opacity = 0,
                ZIndex  = 99
            };
            Grid.SetRowSpan(flash, 3);
            _rootGrid.Add(flash);

            await flash.FadeTo(0.25, 80);
            await flash.FadeTo(0,    300);
            _rootGrid.Remove(flash);
        }

        // ── Lifecycle ─────────────────────────────────────────────────────
        protected override void OnAppearing()
        {
            base.OnAppearing();
            _isProcessing = false;
#if USE_CAMERA
            if (_cameraView != null) _cameraView.IsDetecting = true;
#endif
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
#if USE_CAMERA
            if (_cameraView != null) _cameraView.IsDetecting = false;
#endif
        }
    }
}
