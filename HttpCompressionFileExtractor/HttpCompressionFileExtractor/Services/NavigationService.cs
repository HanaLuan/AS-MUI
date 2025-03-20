using System;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Media.Animation;

namespace HttpCompressionFileExtractor {
	public class NavigationService {
		private static readonly Lazy<NavigationService> _instance = new (() => new NavigationService ());
		private Frame _frame;
		private Panel _overlayHost;

		// Protected constructor to defeat instantiation
		protected NavigationService () { }

		public void ClearOverlay () {
			_overlayHost?.Children.Clear ();
		}

		public void Navigate (Type t) {
			_frame?.Navigate (t);
		}

		public void NavigateFromContext (object dataContext, NavigationTransitionInfo transitionizer = null) {
			_frame?.NavigateFromObject (dataContext, new FluentAvalonia.UI.Navigation.FrameNavigationOptions {
				IsNavigationStackEnabled = true,
				TransitionInfoOverride = transitionizer ?? new SuppressNavigationTransitionInfo ()
			});
		}

		public void SetFrame (Frame frame) {
			_frame = frame;
		}

		public void SetupOverlay (Panel overlayHost) {
			_overlayHost = overlayHost;
		}

		public static NavigationService Instance => _instance.Value;

		public Control PreviousPage { get; private set; }
	}
}