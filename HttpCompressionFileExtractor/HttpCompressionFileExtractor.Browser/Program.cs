using System.Threading.Tasks;

using Avalonia;
using Avalonia.Browser;
using Avalonia.ReactiveUI;
using HttpCompressionFileExtractor;

internal sealed partial class Program {
	private static Task Main () {
		return BuildAvaloniaApp ()
			.WithInterFont ()
			.UseReactiveUI ()
			.StartBrowserAppAsync ("out");
	}

	public static AppBuilder BuildAvaloniaApp () {
		return AppBuilder.Configure<App> ();
	}
}
