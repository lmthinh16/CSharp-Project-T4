using Microsoft.Maui.Controls;
using ProjectApp.Services;

namespace ProjectApp.Pages;

/// <summary>
/// WelcomePage — màn hình onboarding, hiện ra 1 lần sau lần đăng nhập đầu tiên.
/// 3 slide giới thiệu tính năng chính, có thể swipe hoặc nhấn nút.
/// Sau khi xong → navigate vào AppShell.
/// </summary>
public partial class WelcomePage : ContentPage
{
    private int _slide = 0;
    private const int SLIDES = 3;

    // Nội dung từng slide — tuỳ chỉnh theo app của bạn
    private readonly (string Emoji, string Title, string Body)[] _content =
    [
        ("🗺️",
         "Khám phá Vĩnh Khánh",
         "Bản đồ tương tác dẫn đường đến hàng chục quán ngon.\nĐến gần là tự động giới thiệu."),

        ("🔉",
         "Nghe thuyết minh tự động",
         "Khi bạn bước vào vùng geofence của một điểm ăn,\nứng dụng tự kể chuyện bằng audio."),

        ("🌏",
         "3 ngôn ngữ",
         "Tiếng Việt · English · 中文\nChuyển ngôn ngữ bất kỳ lúc nào trong Cài đặt.")
    ];

    public WelcomePage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        ApplySlide(0, animate: false);
        await AnimateEntranceAsync();
    }

    // ── Entrance animation ────────────────────────────────────────────
    private async Task AnimateEntranceAsync()
    {
        logoArea.Opacity       = 0;
        logoArea.TranslationY  = -24;
        slideArea.Opacity      = 0;
        dotsRow.Opacity        = 0;
        btnArea.Opacity        = 0;
        btnArea.TranslationY   = 32;

        await Task.Delay(150);

        await Task.WhenAll(
            logoArea.FadeTo(1, 500, Easing.CubicOut),
            logoArea.TranslateTo(0, 0, 500, Easing.CubicOut));

        await Task.WhenAll(
            slideArea.FadeTo(1, 400, Easing.CubicOut),
            dotsRow.FadeTo(1, 400, Easing.CubicOut));

        await Task.WhenAll(
            btnArea.FadeTo(1, 350, Easing.CubicOut),
            btnArea.TranslateTo(0, 0, 350, Easing.CubicOut));
    }

    // ── Apply slide content ───────────────────────────────────────────
    private async void ApplySlide(int index, bool animate = true)
    {
        _slide = index;
        var (emoji, title, body) = _content[index];

        if (animate) await slideArea.FadeTo(0, 130);

        emojiLabel.Text   = emoji;
        titleLabel.Text   = title;
        bodyLabel.Text    = body;

        // Dots
        dot0.WidthRequest = index == 0 ? 24 : 8;
        dot1.WidthRequest = index == 1 ? 24 : 8;
        dot2.WidthRequest = index == 2 ? 24 : 8;
        dot0.Color = index == 0 ? Color.FromArgb("#2563EB") : Color.FromArgb("#CBD5E1");
        dot1.Color = index == 1 ? Color.FromArgb("#2563EB") : Color.FromArgb("#CBD5E1");
        dot2.Color = index == 2 ? Color.FromArgb("#2563EB") : Color.FromArgb("#CBD5E1");

        // Buttons
        nextBtn.Text      = index < SLIDES - 1 ? "Tiếp tục →" : "🎉 Bắt đầu khám phá!";
        skipBtn.IsVisible = index < SLIDES - 1;

        if (animate) await slideArea.FadeTo(1, 180, Easing.CubicOut);
    }

    // ── Swipe gestures ────────────────────────────────────────────────
    private void OnSwipeLeft(object sender, SwipedEventArgs e)
    {
        if (_slide < SLIDES - 1) ApplySlide(_slide + 1);
    }

    private void OnSwipeRight(object sender, SwipedEventArgs e)
    {
        if (_slide > 0) ApplySlide(_slide - 1);
    }

    // ── Buttons ───────────────────────────────────────────────────────
    private async void OnNextClicked(object sender, EventArgs e)
    {
        if (_slide < SLIDES - 1)
            ApplySlide(_slide + 1);
        else
            await FinishAsync();
    }

    private async void OnSkipClicked(object sender, EventArgs e)
        => await FinishAsync();

    private async Task FinishAsync()
    {
        Preferences.Set("onboarding_done", true);

        await this.FadeTo(0, 300, Easing.CubicIn);

        // Navigate vào AppShell (shell chứa tab bar chính)
        Application.Current!.MainPage = new AppShell();
    }
}
