using Android.Webkit;
using Microsoft.Maui.Handlers;

namespace ProjectApp.Platforms.Android
{
    public class CustomWebViewHandler : WebViewHandler
    {
        protected override global::Android.Webkit.WebView CreatePlatformView()
        {
            var webView = base.CreatePlatformView();

            var settings = webView.Settings;

            settings.JavaScriptEnabled = true;
            settings.DomStorageEnabled = true;
            settings.MixedContentMode = MixedContentHandling.AlwaysAllow;

            // Bật navigator.geolocation trong WebView
            settings.SetGeolocationEnabled(true);

            settings.SetSupportZoom(true);
            settings.BuiltInZoomControls = true;
            settings.DisplayZoomControls = false;

            var ua = settings.UserAgentString ?? "";
            ua = System.Text.RegularExpressions.Regex.Replace(ua, @"Version/[\d.]+ ", "");
            settings.UserAgentString = ua;

            webView.SetWebViewClient(new CustomWebViewClient());
            // Grant quyền geolocation cho JS tự động (app đã xin permission ở C#)
            webView.SetWebChromeClient(new CustomWebChromeClient());

            return webView;
        }
    }

    /// <summary>
    /// WebViewClient tùy chỉnh: bỏ qua lỗi SSL không quan trọng
    /// và log lỗi để dễ debug trên thiết bị thật.
    /// </summary>
    public class CustomWebChromeClient : WebChromeClient
    {
        public override void OnGeolocationPermissionsShowPrompt(
            string? origin,
            GeolocationPermissions.ICallback? callback)
        {
            // App đã xin ACCESS_FINE_LOCATION ở tầng C# (RequestLocationPermissionAsync)
            // Ở đây chỉ cần grant cho JS origin để navigator.geolocation hoạt động
            callback?.Invoke(origin, true, false);
        }
    }

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