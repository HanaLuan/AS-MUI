using ReactiveUI;

namespace HttpCompressionFileExtractor {

	public class ViewModelBase : ReactiveObject {

		//public Interaction<IDialogUnit,bool> ShowNotificationDialog { get; set; }

		private string _dialogCalled;
		public string DialogCalled {
			get => _dialogCalled;
			set => this.RaiseAndSetIfChanged (ref _dialogCalled, value);
		}

		private string _notificationMessage;
		public string NotificationMessage {
			get => _notificationMessage;
			set => this.RaiseAndSetIfChanged (ref _notificationMessage, value);
		}

	}

	public class MainViewModelBase : ViewModelBase {
		public string NavHeader { get; set; }

		public string IconKey { get; set; }

		public bool ShowsInFooter { get; set; }
	}

}