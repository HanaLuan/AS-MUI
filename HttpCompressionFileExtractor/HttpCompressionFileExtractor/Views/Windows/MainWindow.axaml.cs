using Avalonia;
using FluentAvalonia.UI.Windowing;

namespace HttpCompressionFileExtractor {

	public partial class MainWindow : AppWindow {

		public MainWindow () {
			InitializeComponent ();
#if DEBUG
			this.AttachDevTools ();
#endif
			TitleBar.ExtendsContentIntoTitleBar = true;
			TitleBar.TitleBarHitTestType = TitleBarHitTestType.Complex;
		}

	}

}