using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using Eruru.CSharp.Api.FluentAvalonia;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace HttpCompressionFileExtractor {

	public partial class HomeControlViewModel : MainViewModelBase {

		public ObservableCollection<HttpCompressionFileViewModel> Files { get; set; } = [];
		public HttpCompressionFileViewModel File { get => _File; set => this.RaiseAndSetIfChanged (ref _File, value); }
		public ReactiveCommand<Unit, Unit> AddFileCommand { get; set; }

		HttpCompressionFileViewModel _File;
		readonly ApiService ApiService = App.Current.Services.GetService<ApiService> ();

		public HomeControlViewModel () {
			AddFileCommand = ReactiveCommand.CreateFromTask (AddFileAsync);
		}

		async Task AddFileAsync () {
			try {
				var viewModel = new InputDialogViewModel () { Title = "HTTP压缩包URL" };
				if (await ApiService.ShowInputDialogAsync (viewModel) != ContentDialogResult.Primary) {
					return;
				}
#if DEBUG
				if (string.IsNullOrEmpty (viewModel.Text)) {
					viewModel.Text = "http://127.0.0.1:8000/common_ext.pkg";
					viewModel.Text = "http://127.0.0.1:8000/gui-part2.pkg";
				}
#endif
				var file = new HttpCompressionFileViewModel ();
				if (!await file.ConnectAsync (viewModel.Text)) {
					return;
				}
				if (!await file.OpenAsync ()) {
					return;
				}
				Files.Add (file);
				File = file;
				file.Load ();
			} catch (Exception exception) {
				await ApiService.ShowExceptionDialogAsync (exception);
			}
		}

		public void RemoveFile (HttpCompressionFileViewModel file) {
			file.Dispose ();
			Files.Remove (file);
		}

	}

}