using AppProjectT4.Models;

namespace AppProjectT4
{
    public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        LoadRestaurants();
    }

    private async void LoadRestaurants()
    {
        try
        {
            await Task.Delay(500);
            var restaurants = await App.Database.GetRestaurantsAsync();

            if (restaurants.Count == 0)
            {
                await DisplayAlert("Thông báo", "Chưa có dữ liệu nhà hàng. Đang khởi tạo...", "OK");
            }
            else
            {
                RestaurantsCollection.ItemsSource = restaurants;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", $"Không thể tải danh sách: {ex.Message}", "OK");
        }
    }

    private async void OnRestaurantSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Restaurant restaurant)
        {
            await DisplayAlert(
                restaurant.Name,
                $"{restaurant.Description}\n\n" +
                $"📍 {restaurant.Address}\n" +
                $"⭐ Rating: {restaurant.Rating}\n" +
                $"🕐 {restaurant.OpenHours}\n\n" +
                $"📌 Tọa độ: {restaurant.Latitude}, {restaurant.Longitude}",
                "OK"
            );

            ((CollectionView)sender).SelectedItem = null;
        }
    }

   
}
}