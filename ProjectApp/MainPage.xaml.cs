using ProjectApp.ViewModels;

namespace ProjectApp
{
    public partial class MainPage : ContentPage
    {
        private readonly MainViewModel _vm;

        public MainPage()
        {
            InitializeComponent();
            BindingContext = _vm = new MainViewModel();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _vm.LoadAsync();
        }
    }
}
