using Microsoft.Maui.Controls.Shapes;
using ProjectApp.Models;
using ProjectApp.Services;

namespace ProjectApp.Pages
{
    public class VisitHistoryPage : ContentPage
    {
        private readonly string _lang;
        private VerticalStackLayout _listContainer = null!;

        public VisitHistoryPage()
        {
            _lang = UserSession.Language;
            BackgroundColor = Color.FromArgb("#F8FAFC");
            NavigationPage.SetHasNavigationBar(this, false);
            BuildUI();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadHistoryAsync();
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
            
            // Subtle blue gradient
            header.Add(new BoxView
            {
                Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0), EndPoint = new Point(1, 0),
                    GradientStops = new GradientStopCollection {
                        new GradientStop(Color.FromArgb("#1565C0"), 0),
                        new GradientStop(Color.FromArgb("#1E88E5"), 1)
                    }
                }
            });

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
                VerticalOptions   = LayoutOptions.Center,
                FontAttributes = FontAttributes.Bold
            };
            backBtn.GestureRecognizers.Add(new TapGestureRecognizer
                { Command = new Command(async () => await Navigation.PopAsync()) });
 
            header.Add(backBtn);

            header.Add(new Label
            {
                Text = L("🕒 Lịch sử ghé thăm", "🕒 Visit History", "🕒 游览历史"),
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

        private async Task LoadHistoryAsync()
        {
            _listContainer.Clear();
            var histories = await App.Database.GetVisitHistoryAsync();
            
            if (histories.Count == 0)
            {
                _listContainer.Add(new Label
                {
                    Text = L("Chưa có lịch sử ghé thăm nào.\nHãy bắt đầu khám phá!",
                             "No visit history yet.\nStart exploring!",
                             "暂无游览历史。\n开始探索吧！"),
                    FontSize = 15, TextColor = Color.FromArgb("#94A3B8"),
                    HorizontalOptions = LayoutOptions.Center,
                    HorizontalTextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 60)
                });
                return;
            }

            var restaurants = await App.Database.GetRestaurantsAsync();

            var currentDay = string.Empty;

            foreach (var h in histories)
            {
                var r = restaurants.FirstOrDefault(x => x.Id == h.RestaurantId);
                var dayStr = h.VisitedAt.ToString("dd/MM/yyyy");

                // Nếu là ngày mới, thêm header ngày
                if (dayStr != currentDay)
                {
                    _listContainer.Add(new Label
                    {
                        Text = dayStr == DateTime.Now.ToString("dd/MM/yyyy") ? L("Hôm nay", "Today", "今天") : dayStr,
                        FontSize = 14, FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#64748B"),
                        Margin = new Thickness(0, 10, 0, 4)
                    });
                    currentDay = dayStr;
                }

                _listContainer.Add(BuildHistoryCard(h, r));
            }
        }

        private View BuildHistoryCard(VisitHistory h, Restaurant? r)
        {
            var card = new Border
            {
                BackgroundColor = Colors.White,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 14 },
                Padding = new Thickness(16, 12),
                Shadow = new Shadow { Brush = Color.FromArgb("#000000"), Opacity = 0.04f, Radius = 6, Offset = new Point(0, 2) }
            };

            var row = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                },
                ColumnSpacing = 12
            };

            // Avatar / Icon
            var iconBox = new Border
            {
                BackgroundColor = Color.FromArgb("#F1F5F9"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                WidthRequest = 48, HeightRequest = 48,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            iconBox.Content = new Label
            {
                Text = r?.CategoryEmoji ?? "📍",
                FontSize = 24, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center
            };
            row.Add(iconBox, 0, 0);

            // Tên POI
            var infoStack = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Spacing = 2 };
            var nameLbl = new Label
            {
                Text = r?.DisplayName ?? L("Địa điểm không rõ", "Unknown place", "未知地点"),
                FontSize = 15, FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#0D2137"),
                LineBreakMode = LineBreakMode.TailTruncation
            };
            var catLbl = new Label
            {
                Text = r?.Category ?? "POI",
                FontSize = 12, TextColor = Color.FromArgb("#64748B")
            };
            infoStack.Add(nameLbl);
            infoStack.Add(catLbl);
            row.Add(infoStack, 1, 0);

            // Thời gian
            var timeLbl = new Label
            {
                Text = h.VisitedAt.ToString("HH:mm"),
                FontSize = 13, FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#1565C0"),
                VerticalOptions = LayoutOptions.Center
            };
            row.Add(timeLbl, 2, 0);

            card.Content = row;
            
            // Tương tác khi click -> Mở chi tiết
            if (r != null)
            {
                card.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    Command = new Command(async () =>
                    {
                        await Navigation.PushAsync(new RestaurantDetailPage(r));
                    })
                });
            }

            return card;
        }

        private string L(string vi, string en, string zh) => _lang switch { "en" => en, "zh" => zh, _ => vi };
    }
}
