using HttpCompressionFileExtractor.Factory;

namespace HttpCompressionFileExtractor {

	public class MainViewModel : ViewModelBase {

		public NavigationPageFactory NavigationPageFactory { get; }

		public MainViewModel () {
			NavigationPageFactory = new NavigationPageFactory (this);
		}

	}

}