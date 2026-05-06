using ProjectApp.Models;
using ProjectApp.ViewModels;

namespace ProjectApp.Pages;

public partial class FavoritePage : ContentPage
{
    private readonly FavoritesViewModel _vm;

    public FavoritePage()
    {
        InitializeComponent();
        BindingContext = _vm = new FavoritesViewModel();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _vm.LoadAsync();
    }
}
