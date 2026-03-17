using Microsoft.Maui.Controls.Shapes;
using ProjectApp.Models;

namespace ProjectApp
{
    public class TourDetailPage : ContentPage
    {
        private Tour _tour;

        public TourDetailPage(Tour tour)
        {
            _tour = tour;
            Title = tour.Name;
            BackgroundColor = Color.FromArgb("#F8F9FA");

            CreateUI();
        }

        private async void CreateUI()
        {
            var allRestaurants = await App.Database.GetRestaurantsAsync();
            var tourRestaurants = allRestaurants.Where(r => _tour.RestaurantIds.Contains(r.Id)).ToList();

            var scrollView = new ScrollView();
            var mainLayout = new VerticalStackLayout { Spacing = 0 };

            // Header
            var header = new Border
            {
                BackgroundColor = Color.FromArgb("#FF6B6B"),
                Padding = new Thickness(20, 30, 20, 20),
                Margin = new Thickness(0, 0, 0, 10),
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(0, 0, 30, 30) },
                StrokeThickness = 0
            };

            var headerContent = new VerticalStackLayout { Spacing = 5 };
            headerContent.Add(new Label
            {
                Text = $"{_tour.Emoji} {_tour.Name}",
                FontSize = 22,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HorizontalTextAlignment = TextAlignment.Center
            });
            headerContent.Add(new Label
            {
                Text = _tour.Description,
                FontSize = 13,
                TextColor = Color.FromArgb("#FFE5E5"),
                HorizontalTextAlignment = TextAlignment.Center
            });
            headerContent.Add(new Label
            {
                Text = $"⭐ {_tour.Rating} • {_tour.Duration} • {tourRestaurants.Count} địa điểm",
                FontSize = 13,
                TextColor = Color.FromArgb("#FFE5E5"),
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 5, 0, 0)
            });

            header.Content = headerContent;
            mainLayout.Add(header);

            // Restaurant list
            var listLayout = new VerticalStackLayout
            {
                Padding = new Thickness(15, 0),
                Spacing = 8
            };

            foreach (var restaurant in tourRestaurants)
            {
                listLayout.Add(CreateRestaurantCard(restaurant));
            }

            mainLayout.Add(listLayout);
            scrollView.Content = mainLayout;
            Content = scrollView;
        }

        private Border CreateRestaurantCard(Restaurant restaurant)
        {
            var border = new Border
            {
                BackgroundColor = Colors.White,
                StrokeThickness = 0,
                Padding = 0,
                Margin = new Thickness(0, 8),
                StrokeShape = new RoundRectangle { CornerRadius = 20 }
            };

            var grid = new Grid { RowDefinitions = { new RowDefinition { Height = 140 }, new RowDefinition { Height = GridLength.Auto } } };

            // Image area
            var imageArea = new Border
            {
                BackgroundColor = Color.FromArgb("#FFE5E5"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(20, 20, 0, 0) }
            };

            var imageGrid = new Grid();
            imageGrid.Add(new Label
            {
                Text = "🍜",
                FontSize = 70,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Opacity = 0.3
            });

            var ratingBadge = new Border
            {
                BackgroundColor = Color.FromArgb("#FF6B6B"),
                StrokeThickness = 0,
                Padding = new Thickness(10, 6),
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Start,
                Margin = 12,
                StrokeShape = new RoundRectangle { CornerRadius = 15 }
            };

            var ratingStack = new HorizontalStackLayout { Spacing = 4 };
            ratingStack.Add(new Label { Text = "⭐", FontSize = 14 });
            ratingStack.Add(new Label { Text = restaurant.Rating.ToString(), FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Colors.White });
            ratingBadge.Content = ratingStack;
            imageGrid.Add(ratingBadge);

            imageArea.Content = imageGrid;
            grid.Add(imageArea, 0, 0);

            // Info area
            var info = new VerticalStackLayout { Padding = 15, Spacing = 8 };
            info.Add(new Label { Text = restaurant.Name, FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#2C3E50") });
            info.Add(new Label { Text = restaurant.Description, FontSize = 13, TextColor = Color.FromArgb("#7F8C8D"), LineBreakMode = LineBreakMode.TailTruncation, MaxLines = 2 });

            var timeStack = new HorizontalStackLayout { Spacing = 5 };
            timeStack.Add(new Label { Text = "🕐", FontSize = 13 });
            timeStack.Add(new Label { Text = restaurant.OpenHours, FontSize = 13, TextColor = Color.FromArgb("#27AE60"), FontAttributes = FontAttributes.Bold });
            info.Add(timeStack);

            grid.Add(info, 0, 1);
            border.Content = grid;

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += async (s, e) =>
            {
                await DisplayAlert(restaurant.Name,
                    $"{restaurant.Description}\n\n📍 {restaurant.Address}\n⭐ {restaurant.Rating}\n🕐 {restaurant.OpenHours}",
                    "OK");
            };
            border.GestureRecognizers.Add(tapGesture);

            return border;
        }
    }
}