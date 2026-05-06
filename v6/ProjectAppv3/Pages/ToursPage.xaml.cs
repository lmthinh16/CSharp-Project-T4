using ProjectApp.Models;

namespace ProjectApp.Pages;

public partial class ToursPage : ContentPage
{
    public ToursPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadToursAsync();
    }

    private async Task LoadToursAsync()
    {
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        ToursCollection.IsVisible  = false;
        EmptyState.IsVisible       = false;

        try
        {
            var tours = await App.Database.GetToursAsync();

            if (tours.Count == 0)
            {
                EmptyState.IsVisible = true;
            }
            else
            {
                ToursCollection.ItemsSource = tours.Where(t => t.IsActive).ToList();
                ToursCollection.IsVisible   = true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ToursPage] {ex.Message}");
            EmptyState.IsVisible = true;
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }

    private async void OnTourTapped(object sender, TappedEventArgs e)
    {
        if (sender is VisualElement el && el.BindingContext is Tour tour)
            await Shell.Current.GoToAsync("TourDetail", new Dictionary<string, object> { ["Tour"] = tour });
    }
}
