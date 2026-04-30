using Microsoft.Maui.Controls.Shapes;
using ProjectApp.Models;
using ProjectApp.Services;

namespace ProjectApp.Pages
{
    public class AnalyticsPage : ContentPage
    {
        private VerticalStackLayout _content = null!;

        // ── Palette (Clean BLUE & WHITE Tone) ───────────────────
        static readonly Color BgPage = Color.FromArgb("#F8FAFC"); // Light Background
        static readonly Color BgCard = Colors.White;
        static readonly Color PrimaryBlue = Color.FromArgb("#1565C0"); // Core App Blue
        static readonly Color AccentBlue = Color.FromArgb("#42A5F5"); // Light Blue Accent
        static readonly Color TextPrimary = Color.FromArgb("#0D2137"); // Deep Dark Navy for text
        static readonly Color TextSecond = Color.FromArgb("#475569"); // Darker Slate for High Contrast
        static readonly Color Divider = Color.FromArgb("#E2E8F0");
        static readonly Color AccentGold = Color.FromArgb("#F59E0B");

        private string _currentLang = Preferences.Default.Get("app_lang", "vi");

        public AnalyticsPage()
        {
            Title = "Thống kê";
            BackgroundColor = BgPage;
            NavigationPage.SetHasNavigationBar(this, false);
            BuildUI();
            UpdateLanguage(_currentLang);
        }

        public void UpdateLanguage(string lang)
        {
            _currentLang = lang;
            Title = lang switch { "en" => "Analytics", "zh" => "统计分析", "ja" => "統計", "ko" => "통계 분석", _ => "Thống kê hành trình" };
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadAsync();
        }

        private void BuildUI()
        {
            _content = new VerticalStackLayout { Spacing = 0, Padding = new Thickness(0, 0, 0, 40) };
            Content = new ScrollView { Content = _content, BackgroundColor = BgPage };
        }

        private async Task LoadAsync()
        {
            _content.Children.Clear();

            // ── Hero Header ───────────────────────────────────────
            _content.Add(BuildHeroHeader(Navigation));

            try
            {
                var summary = await AnalyticsService.GetSummaryAsync();
                var topPois = await AnalyticsService.GetTopPoisAsync(6);

                // ── Stat cards row ────────────────────────────────
                _content.Add(BuildStatCardsRow(summary));

                // ── Insight strip ─────────────────────────────────
                _content.Add(BuildInsightStrip(summary.TotalListens, topPois));

                // ── Top POI ranking ───────────────────────────────
                var rankingTitle = _currentLang switch { "en" => "🏆  Popular Spots", "zh" => "🏆  热门地点", "ja" => "🏆  人気のスポット", "ko" => "🏆  인기 장소", _ => "🏆  Địa điểm nổi bật" };
                _content.Add(BuildSectionHeader(rankingTitle));

                if (topPois.Count > 0)
                    _content.Add(BuildRankingList(topPois));
                else
                {
                    var emptyMsg = _currentLang switch 
                    { 
                        "en" => "No narration listens yet.\nStart exploring Vinh Khanh! 🍜", 
                        "zh" => "尚未听取导览。\n开始探索永庆吧！ 🍜",
                        "ja" => "音声ガイドをまだ聴いていません。\nヴィンカンを探索しましょう！ 🍜",
                        "ko" => "음성 안내를 아직 듣지 않았습니다.\n지금 바로 빈칸을 탐색해보세요! 🍜",
                        _ => "Chưa có lượt nghe thuyết minh nào.\nHãy bắt đầu khám phá Vĩnh Khánh! 🍜" 
                    };
                    _content.Add(BuildEmptyState(emptyMsg));
                }

                // ── Avg listen time chart ─────────────────────────
                var withTime = topPois.Where(p => p.AvgSeconds > 1).ToList();
                if (withTime.Count > 0)
                {
                    var chartTitle = _currentLang switch { "en" => "⏱  Avg Listening Time (sec)", "zh" => "⏱  平均收听时间（秒）", "ja" => "⏱  平均視聴時間（秒）", "ko" => "⏱  평균 청취 시간 (초)", _ => "⏱  Thời gian nghe trung bình (giây)" };
                    _content.Add(BuildSectionHeader(chartTitle));
                    _content.Add(BuildAvgTimeChart(withTime));
                }

                _content.Add(BuildClearButton());
            }
            catch (Exception ex)
            {
                _content.Add(BuildEmptyState("Lỗi: " + ex.Message));
            }
        }

        private Grid BuildHeroHeader(INavigation nav)
        {
            var grid = new Grid { HeightRequest = 180, BackgroundColor = PrimaryBlue };
            
            // Subtle blue gradient
            grid.Add(new BoxView
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

            // Back button (Left)
            var backBtn = new Border
            {
                BackgroundColor = Color.FromArgb("#20FFFFFF"),
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                StrokeThickness = 0,
                Padding = new Thickness(12, 4),
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(20, 20, 0, 0)
            };
            backBtn.Content = new Label { Text = "←", FontSize = 22, TextColor = Colors.White, FontAttributes = FontAttributes.Bold };
            var tb = new TapGestureRecognizer();
            tb.Tapped += async (s, e) => { if (nav.ModalStack.Count > 0) await nav.PopModalAsync(); else await nav.PopAsync(); };
            backBtn.GestureRecognizers.Add(tb);
            grid.Add(backBtn);

            // Refresh button (Right)
            var refreshBtn = new Border
            {
                BackgroundColor = Color.FromArgb("#20FFFFFF"),
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                StrokeThickness = 0,
                Padding = new Thickness(10, 8),
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(0, 20, 20, 0)
            };
            refreshBtn.Content = new Label { Text = "🔄", FontSize = 16, TextColor = Colors.White };
            var tr = new TapGestureRecognizer();
            tr.Tapped += async (s, e) => {
                refreshBtn.Opacity = 0.5;
                await LoadAsync();
                refreshBtn.Opacity = 1.0;
            };
            refreshBtn.GestureRecognizers.Add(tr);
            grid.Add(refreshBtn);

            var textStack = new VerticalStackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(24, 40, 24, 0),
                Spacing = 2
            };
            var heroTitle = _currentLang switch { "en" => "Journey Analytics", "zh" => "行程统计", "ja" => "統計", "ko" => "여정 통계", _ => "Thống kê hành trình" };
            var heroSub = _currentLang switch { "en" => "Your exploration dashboard", "zh" => "您的探险面板", "ja" => "あなたの探検ダッシュボード", "ko" => "나의 탐험 대시보드", _ => "Hành trình khám phá Quận 4 của bạn" };
            
            textStack.Add(new Label { Text = heroTitle, FontSize = 24, FontAttributes = FontAttributes.Bold, TextColor = Colors.White });
            textStack.Add(new Label { Text = heroSub, FontSize = 13, TextColor = Color.FromArgb("#BBDEFB") });
            grid.Add(textStack);

            return grid;
        }

        private Grid BuildStatCardsRow(AnalyticsSummary s)
        {
            var grid = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(), new ColumnDefinition() },
                RowDefinitions = { new RowDefinition(), new RowDefinition() },
                ColumnSpacing = 12, RowSpacing = 12,
                Margin = new Thickness(16, 16, 16, 0)
            };

            var lblListens = _currentLang switch { "en" => "Total Listens", "zh" => "总收听量", "ja" => "総視聴数", "ko" => "총 청취 수", _ => "Tổng lượt nghe" };
            var lblSpots = _currentLang switch { "en" => "Visited Spots", "zh" => "已游览地点", "ja" => "訪問スポット", "ko" => "방문 장소", _ => "Địa điểm ghé qua" };
            var lblAvg = _currentLang switch { "en" => "Avg Time", "zh" => "平均时间", "ja" => "平均時間", "ko" => "평균 시간", _ => "Thời gian nghe TB" };
            var lblTour = _currentLang switch { "en" => "Tours Done", "zh" => "已完成行程", "ja" => "完了したツアー", "ko" => "완료된 투어", _ => "Tour hoàn thành" };

            grid.Add(MakeStatCard("🎧", s.TotalListens.ToString(), lblListens, PrimaryBlue), 0, 0);
            grid.Add(MakeStatCard("📍", s.UniquePois.ToString(), lblSpots, AccentBlue), 1, 0);
            grid.Add(MakeStatCard("⏱", $"{s.AvgListenSec:F0}s", lblAvg, Color.FromArgb("#2E7D32")), 0, 1);
            grid.Add(MakeStatCard("🗺️", s.TourCompletes.ToString(), lblTour, AccentGold), 1, 1);

            return grid;
        }

        private Border MakeStatCard(string icon, string value, string label, Color accent)
        {
            var valLbl = new Label { Text = value, FontSize = 24, FontAttributes = FontAttributes.Bold, TextColor = TextPrimary };
            var subLbl = new Label { Text = label, FontSize = 11, TextColor = TextSecond };
            var iconLbl = new Label { Text = icon, FontSize = 22, HorizontalOptions = LayoutOptions.End, Opacity = 0.8 };

            var stack = new VerticalStackLayout { Spacing = 2 };
            stack.Add(iconLbl);
            stack.Add(valLbl);
            stack.Add(subLbl);

            return new Border
            {
                BackgroundColor = BgCard,
                StrokeShape = new RoundRectangle { CornerRadius = 18 },
                StrokeThickness = 1,
                Stroke = new SolidColorBrush(Color.FromArgb("#F1F5F9")),
                Padding = new Thickness(16),
                Shadow = new Shadow { Brush = Colors.Black, Opacity = 0.05f, Radius = 8, Offset = new Point(0, 4) },
                Content = stack
            };
        }

        private Border BuildInsightStrip(int listens, List<PoiStats> topPois)
        {
            string msg = listens == 0 
                ? (_currentLang switch { "en" => "✨ Start exploring to see your personal stats!", "zh" => "✨ 即刻开启探索，查看个人统计！", "ja" => "✨ 探険を始めて、自分の統計を確認しましょう！", "ko" => "✨ 탐험을 시작하고 개인 통계를 확인해 보세요!", _ => "✨ Bắt đầu khám phá ngay để xem thống kê của riêng bạn!" })
                : (_currentLang switch 
                    { 
                        "en" => $"🏆 You've listened {listens} times. " + (topPois.Count>0?$"Most loved: {topPois[0].PoiName}!":""),
                        "zh" => $"🏆 您已收听 {listens} 次。 " + (topPois.Count>0?$"最爱: {topPois[0].PoiName}!":""),
                        "ja" => $"🏆 {listens} 回視聴しました。 " + (topPois.Count>0?$"お気に入り: {topPois[0].PoiName}!":""),
                        "ko" => $"🏆 {listens} 회 청취했습니다. " + (topPois.Count>0?$"가장 좋아하는 곳: {topPois[0].PoiName}!":""),
                        _ => $"🏆 Bạn đã nghe thuyết minh {listens} lần. " + (topPois.Count > 0 ? $"Thích nhất là {topPois[0].PoiName}!" : "")
                    });

            return new Border
            {
                BackgroundColor = Color.FromArgb("#1565C01A"),
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                StrokeThickness = 0,
                Padding = new Thickness(16, 12),
                Margin = new Thickness(16, 16, 16, 0),
                Content = new Label { Text = msg, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = PrimaryBlue, HorizontalTextAlignment = TextAlignment.Center }
            };
        }

        private Grid BuildSectionHeader(string title)
        {
            var grid = new Grid { Margin = new Thickness(20, 24, 20, 12) };
            grid.Add(new Label { Text = title, FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = TextPrimary });
            return grid;
        }

        private Border BuildRankingList(List<PoiStats> pois)
        {
            var stack = new VerticalStackLayout { Spacing = 12, Padding = new Thickness(16) };
            int max = Math.Max(pois.Max(x => x.ListenCount), 1);

            for (int i = 0; i < pois.Count; i++)
            {
                var p = pois[i];
                double ratio = (double)p.ListenCount / max;
                
                var row = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = GridLength.Auto } } };
                
                var nameStack = new VerticalStackLayout { Spacing = 4 };
                nameStack.Add(new Label { Text = $"{(i < 3 ? "⭐ " : "")}{p.PoiName}", FontSize = 14, TextColor = TextPrimary });
                
                var barCont = new Grid { HeightRequest = 6 };
                barCont.Add(new BoxView { BackgroundColor = Color.FromArgb("#2F3E4F"), CornerRadius = 3 });
                barCont.Add(new BoxView { BackgroundColor = PrimaryBlue, CornerRadius = 3, HorizontalOptions = LayoutOptions.Start, WidthRequest = 200 * ratio });
                nameStack.Add(barCont);
                
                row.Add(nameStack, 0, 0);
                row.Add(new Label { Text = $"{p.ListenCount}x", FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = AccentBlue, VerticalOptions = LayoutOptions.Center }, 1, 0);
                
                stack.Add(row);
                if (i < pois.Count - 1) stack.Add(new BoxView { HeightRequest = 1, BackgroundColor = Divider });
            }

            return new Border { BackgroundColor = BgCard, StrokeShape = new RoundRectangle { CornerRadius = 20 }, Padding = 0, Margin = new Thickness(16, 0), Content = stack };
        }

        private Border BuildAvgTimeChart(List<PoiStats> pois)
        {
            var stack = new VerticalStackLayout { Spacing = 14, Padding = new Thickness(16) };
            double max = Math.Max(pois.Max(x => x.AvgSeconds), 1);

            foreach (var p in pois)
            {
                // Increase label column width and add more spacing
                var row = new Grid { ColumnDefinitions = { 
                    new ColumnDefinition { Width = 110 }, 
                    new ColumnDefinition { Width = GridLength.Star }, 
                    new ColumnDefinition { Width = 45 } 
                } };
                
                row.Add(new Label { 
                    Text = p.PoiName, FontSize = 12, TextColor = TextSecond, 
                    VerticalOptions = LayoutOptions.Center, MaxLines = 1, 
                    LineBreakMode = LineBreakMode.TailTruncation 
                }, 0, 0);
                
                var barCont = new Grid { HeightRequest = 6, VerticalOptions = LayoutOptions.Center };
                barCont.Add(new BoxView { BackgroundColor = Color.FromArgb("#F1F5F9"), CornerRadius = 3 }); // Lighter track background
                
                double barWidth = 130 * (p.AvgSeconds / max);
                barCont.Add(new BoxView { 
                    BackgroundColor = Color.FromArgb("#10B981"), // More vibrant Green
                    CornerRadius = 3, HorizontalOptions = LayoutOptions.Start, 
                    WidthRequest = Math.Max(barWidth, 2) 
                });
                row.Add(barCont, 1, 0);
                
                row.Add(new Label { 
                    Text = $"{(int)p.AvgSeconds}s", FontSize = 12, FontAttributes = FontAttributes.Bold, 
                    TextColor = PrimaryBlue, HorizontalOptions = LayoutOptions.End, 
                    VerticalOptions = LayoutOptions.Center 
                }, 2, 0);
                
                stack.Add(row);
            }
            return new Border { BackgroundColor = BgCard, StrokeShape = new RoundRectangle { CornerRadius = 20 }, Margin = new Thickness(16, 0), Content = stack };
        }

        private Button BuildClearButton()
        {
            var btnText = _currentLang switch { "en" => "Clear Analytics", "zh" => "清除统计", "ja" => "統計を消去", "ko" => "통계 데이터 초기화", _ => "Xóa thống kê" };
            var btn = new Button
            {
                Text = btnText, BackgroundColor = Colors.Transparent, BorderColor = Color.FromArgb("#FF5252"), TextColor = Color.FromArgb("#FF5252"),
                BorderWidth = 1, CornerRadius = 12, Margin = new Thickness(24, 32, 24, 0), FontSize = 13, HeightRequest = 44
            };
            btn.Clicked += async (s, e) => {
                var title = _currentLang switch { "en" => "Confirm", "zh" => "确认", "ja" => "確認", "ko" => "확인", _ => "Xác nhận" };
                var body = _currentLang switch { "en" => "Do you want to clear all history from both device and server?", "ko" => "기기와 서버에서 모든 기록을 삭제하시겠습니까?", _ => "Bạn có muốn xóa toàn bộ lịch sử thống kê trên thiết bị và server?" };
                var ok = _currentLang switch { "en" => "Clear All", "ko" => "모두 삭제", _ => "Xóa sạch" };
                var cancel = _currentLang switch { "en" => "Cancel", "ko" => "취소", _ => "Hủy" };
                
                if (await DisplayAlert(title, body, ok, cancel)) {
                    await AnalyticsService.ClearAllAsync();
                    await LoadAsync();
                }
            };
            return btn;
        }

        private Border BuildEmptyState(string msg) => new Border
        {
            BackgroundColor = BgCard, StrokeShape = new RoundRectangle { CornerRadius = 16 }, Padding = 30, Margin = 16,
            Content = new Label { Text = msg, TextColor = TextSecond, FontSize = 14, HorizontalTextAlignment = TextAlignment.Center }
        };
    }
}
