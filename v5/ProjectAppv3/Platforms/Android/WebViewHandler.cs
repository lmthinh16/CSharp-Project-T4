using Android.Webkit;
using Microsoft.Maui.Handlers;

namespace ProjectApp.Platforms.Android
{
    /// <summary>
    /// Custom WebView handler để bật JavaScript và cho phép load
    /// tài nguyên ngoài (Leaflet CDN) trên thiết bị thật Android.
    /// Không cần trên emulator vì emulator ít bị chặn hơn.
    /// </summary>
    public class CustomWebViewHandler : WebViewHandler
    {
        protected override global::Android.Webkit.WebView CreatePlatformView()
        {
            var webView = base.CreatePlatformView();

            var settings = webView.Settings;

            // Bật JavaScript (bắt buộc cho Leaflet)
            settings.JavaScriptEnabled = true;

            // Cho phép load tài nguyên HTTP lẫn HTTPS trong cùng trang
            // (Leaflet CDN dùng HTTPS nhưng tile OSM đôi khi mixed)
            settings.MixedContentMode = MixedContentHandling.AlwaysAllow;

            // Cho phép WebView truy cập DOM Storage (localStorage)
            settings.DomStorageEnabled = true;

            // Bật zoom bằng tay trong WebView
            settings.SetSupportZoom(true);
            settings.BuiltInZoomControls = true;
            settings.DisplayZoomControls = false;

            // Tắt cache để luôn tải tile mới nhất khi debug
            // (bỏ comment dòng dưới nếu muốn cache tile)
            // settings.CacheMode = CacheModes.NoCache;

            // Custom WebViewClient để intercept lỗi SSL/network trên thiết bị thật
            webView.SetWebViewClient(new CustomWebViewClient());

            return webView;
        }
    }

    /// <summary>
    /// WebViewClient tùy chỉnh: bỏ qua lỗi SSL không quan trọng
    /// và log lỗi để dễ debug trên thiết bị thật.
    /// </summary>
    public class CustomWebViewClient : WebViewClient
    {
        public override void OnReceivedError(
            global::Android.Webkit.WebView? view,
            IWebResourceRequest? request,
            WebResourceError? error)
        {
            // Log để xem lỗi trong Output window Visual Studio
            System.Diagnostics.Debug.WriteLine(
                $"[WebView] Error loading {request?.Url}: {error?.Description}");

            // Không gọi base để tránh hiện trang lỗi mặc định của Android
            // base.OnReceivedError(view, request, error);
        }

        public override void OnReceivedSslError(
            global::Android.Webkit.WebView? view,
            SslErrorHandler? handler,
            global::Android.Net.Http.SslError? error)
        {
            // Cho phép tất cả SSL để Leaflet CDN (unpkg.com) không bị block
            // trên một số thiết bị Android cũ có root certificate lỗi thời
            handler?.Proceed();
        }
    }
}