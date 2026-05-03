package crc64df9c57dff0956030;


public class CustomWebViewClient
	extends android.webkit.WebViewClient
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onReceivedError:(Landroid/webkit/WebView;Landroid/webkit/WebResourceRequest;Landroid/webkit/WebResourceError;)V:GetOnReceivedError_Landroid_webkit_WebView_Landroid_webkit_WebResourceRequest_Landroid_webkit_WebResourceError_Handler\n" +
			"n_onReceivedSslError:(Landroid/webkit/WebView;Landroid/webkit/SslErrorHandler;Landroid/net/http/SslError;)V:GetOnReceivedSslError_Landroid_webkit_WebView_Landroid_webkit_SslErrorHandler_Landroid_net_http_SslError_Handler\n" +
			"";
		mono.android.Runtime.register ("ProjectApp.Platforms.Android.CustomWebViewClient, ProjectApp", CustomWebViewClient.class, __md_methods);
	}

	public CustomWebViewClient ()
	{
		super ();
		if (getClass () == CustomWebViewClient.class) {
			mono.android.TypeManager.Activate ("ProjectApp.Platforms.Android.CustomWebViewClient, ProjectApp", "", this, new java.lang.Object[] {  });
		}
	}

	public void onReceivedError (android.webkit.WebView p0, android.webkit.WebResourceRequest p1, android.webkit.WebResourceError p2)
	{
		n_onReceivedError (p0, p1, p2);
	}

	private native void n_onReceivedError (android.webkit.WebView p0, android.webkit.WebResourceRequest p1, android.webkit.WebResourceError p2);

	public void onReceivedSslError (android.webkit.WebView p0, android.webkit.SslErrorHandler p1, android.net.http.SslError p2)
	{
		n_onReceivedSslError (p0, p1, p2);
	}

	private native void n_onReceivedSslError (android.webkit.WebView p0, android.webkit.SslErrorHandler p1, android.net.http.SslError p2);

	private java.util.ArrayList refList;
	public void monodroidAddReference (java.lang.Object obj)
	{
		if (refList == null)
			refList = new java.util.ArrayList ();
		refList.add (obj);
	}

	public void monodroidClearReferences ()
	{
		if (refList != null)
			refList.clear ();
	}
}
