using Microsoft.Maui.Controls.Shapes;
using ProjectApp.Models;

namespace ProjectApp
{
    public partial class RestaurantDetails : ContentPage
    {
        private Restaurant _restaurant;

        public RestaurantDetails(Restaurant restaurant)
        {
            InitializeComponent();
            _restaurant = restaurant;
            BindingContext = _restaurant;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            var audiosVI = await App.Database.GetAudiosForRestaurantAsync(_restaurant.Id, "vi-VN");
            LabelTextVI.Text = audiosVI.FirstOrDefault()?.TextContent ?? "Chưa có kịch bản tiếng Việt.";

            var audiosEN = await App.Database.GetAudiosForRestaurantAsync(_restaurant.Id, "en-US");
            LabelTextEN.Text = audiosEN.FirstOrDefault()?.TextContent ?? "No English script available.";

            var audiosZH = await App.Database.GetAudiosForRestaurantAsync(_restaurant.Id, "zh-CN");
            LabelTextZH.Text = audiosZH.FirstOrDefault()?.TextContent ?? "暂无中文剧本。";
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void OnPlayVI_Clicked(object sender, EventArgs e)
        {
            App.Audio.CurrentLanguageCode = "vi-VN";
            await App.Audio.PlayPoiAudioAsync(_restaurant);
        }

        private async void OnPlayEN_Clicked(object sender, EventArgs e)
        {
            App.Audio.CurrentLanguageCode = "en-US";
            await App.Audio.PlayPoiAudioAsync(_restaurant);
        }

        private async void OnPlayZH_Clicked(object sender, EventArgs e)
        {
            App.Audio.CurrentLanguageCode = "zh-CN";
            await App.Audio.PlayPoiAudioAsync(_restaurant);
        }
        
        private void OnStopAudio_Clicked(object sender, EventArgs e)
        {
            App.Audio.StopAudio();
        }
    }
}