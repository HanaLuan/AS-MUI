using Avalonia.Controls;
using FluentAvalonia.UI.Controls;

namespace HttpCompressionFileExtractor {

	public partial class HomeControl : UserControl {

		public HomeControl () {
			InitializeComponent ();
		}

		void TabView_TabCloseRequested (TabView _, TabViewTabCloseRequestedEventArgs args) {
			(DataContext as HomeControlViewModel).RemoveFile (args.Item as HttpCompressionFileViewModel);
		}

	}

}