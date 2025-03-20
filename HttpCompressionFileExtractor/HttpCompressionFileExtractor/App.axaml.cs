using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Eruru.CSharp.Api.FluentAvalonia;
using Microsoft.Extensions.DependencyInjection;

namespace HttpCompressionFileExtractor {

	public partial class App : Application {

		public new static App Current => Application.Current as App;

		public IServiceProvider Services { get; set; }
		public TopLevel TopLevel { get; set; }
		public Func<Uri, Stream> OnCreateFileStream { get; set; } = uri => {
			if (File.Exists (uri.LocalPath)) {
				return File.OpenWrite (uri.LocalPath);
			}
			return File.Create (uri.LocalPath);
		};

		public override void Initialize () {
			AvaloniaXamlLoader.Load (this);
		}

		public override void OnFrameworkInitializationCompleted () {
			var apiService = new ApiService (() => TopLevel);
			var fileService = new FileService (apiService);
			var services = new ServiceCollection ();
			services.AddSingleton (fileService);
			services.AddSingleton (apiService);
			Services = services.BuildServiceProvider ();
			Visual visual = null;
			if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
				var window = new MainWindow { DataContext = new MainViewModel () };
				visual = window;
				desktop.MainWindow = window;
			} else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform) {
				var window = new MainView { DataContext = new MainViewModel () };
				visual = window;
				singleViewPlatform.MainView = window;
			}
			Dispatcher.UIThread.Post (() => {
				TopLevel = TopLevel.GetTopLevel (visual);
			});
			base.OnFrameworkInitializationCompleted ();
		}

	}

}