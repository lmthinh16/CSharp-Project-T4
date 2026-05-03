using ProjectApp.Services;

namespace ProjectApp.Pages;

public partial class LanguageSelectionPage : ContentPage
{
    private static readonly Color NormalBg     = Color.FromArgb("#0D1B2A");
    private static readonly Color SelectedBg   = Color.FromArgb("#0A2A45");
    private static readonly Color NormalStroke  = Color.FromArgb("#1E3A5F");
    private static readonly Color SelectedStroke = Color.FromArgb("#4EA5D9");

    private readonly bool _isOnboarding;
    private string _selectedLang;

    private Dictionary<string, (Border card, Label check)> _items = null!;

    public LanguageSelectionPage(bool isOnboarding = true)
    {
        InitializeComponent();
        _isOnboarding = isOnboarding;
        _selectedLang = UserSession.Language ?? "vi";

        BtnConfirm.Text = isOnboarding ? "Tiếp tục" : "Lưu";
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _items = new()
        {
            ["vi"] = (CardVi, CheckVi),
            ["en"] = (CardEn, CheckEn),
            ["zh"] = (CardZh, CheckZh),
            ["ja"] = (CardJa, CheckJa),
            ["ko"] = (CardKo, CheckKo),
        };

        ApplySelection(_selectedLang);
    }

    private void OnLanguageTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is string lang)
            ApplySelection(lang);
    }

    private void ApplySelection(string lang)
    {
        _selectedLang = lang;
        foreach (var (key, (card, check)) in _items)
        {
            bool selected = key == lang;
            card.BackgroundColor = selected ? SelectedBg : NormalBg;
            card.Stroke = new SolidColorBrush(selected ? SelectedStroke : NormalStroke);
            check.IsVisible = selected;
        }
    }

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        BtnConfirm.IsEnabled = false;
        LoadingOverlay.IsVisible = true;

        // Nhường 1 frame để overlay render xong trước khi rebuild
        await Task.Delay(80);

        UserSession.Language = _selectedLang;
        Preferences.Set("lang_selected", true);
        Services.LocalizationManager.Instance.SetLanguage(_selectedLang);

        if (!_isOnboarding)
        {
            var shell = new AppShell();
            Application.Current!.MainPage = shell;
            await shell.GoToAsync("//Profile");
            return;
        }

        // Onboarding: tiếp tục flow bình thường
        Page next;
        if (UserSession.Current.IsLoggedIn)
        {
            Application.Current!.MainPage = new AppShell();
            return;
        }
        else if (!Preferences.Get("onboarding_done", false))
        {
            next = new WelcomePage();
        }
        else
        {
            next = new QrEntryPage();
        }

        await Navigation.PushAsync(next);
    }
}
