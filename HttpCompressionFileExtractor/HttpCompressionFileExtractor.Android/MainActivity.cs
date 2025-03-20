using Android.App;
using Android.Content.PM;
using Android.Net;
using Avalonia;
using Avalonia.Android;
using Avalonia.ReactiveUI;

namespace HttpCompressionFileExtractor.Android {
	[Activity (
		Label = "HttpCompressionFileExtractor.Android",
		Theme = "@style/MyTheme.NoActionBar",
		Icon = "@drawable/icon",
		MainLauncher = true,
		ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
	public class MainActivity : AvaloniaMainActivity<App> {
		protected override AppBuilder CustomizeAppBuilder (AppBuilder builder) {
			return base.CustomizeAppBuilder (builder)
				.WithInterFont ()
				.UseReactiveUI ();
		}
		protected override void OnStart () {
			base.OnStart ();
			App.Current.OnCreateFileStream = uri => ContentResolver.OpenOutputStream (Uri.Parse (uri.ToString ()));
		}
	}
}
