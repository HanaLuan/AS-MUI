using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using FluentAvalonia.Core;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;

namespace HttpCompressionFileExtractor {

	public partial class MainView : UserControl {

		public MainView () {
			InitializeComponent ();
		}

		protected override void OnAttachedToVisualTree (VisualTreeAttachmentEventArgs e) {
			base.OnAttachedToVisualTree (e);
			var viewModel = new MainViewModel ();
			DataContext = viewModel;
			FrameView.NavigationPageFactory = viewModel.NavigationPageFactory;
			NavigationService.Instance.SetFrame (FrameView);
			if (e.Root is AppWindow window) {
				InitializeNavigationPages ();
			} else {
				InitializeNavigationPages ();
			}
			NavView.ItemInvoked += OnNavigationViewItemInvoked;
		}

		void OnNavigationViewItemInvoked (object sender, NavigationViewItemInvokedEventArgs e) {
			if (e.InvokedItemContainer is NavigationViewItem item) {
				NavigationService.Instance.NavigateFromContext (item.Tag, e.RecommendedNavigationTransitionInfo);
			}
		}

		protected override void OnLoaded (RoutedEventArgs e) {
			base.OnLoaded (e);
			if (VisualRoot is AppWindow window) {
				TitleBarHost.Height = 31;
				TitleBarHost.ColumnDefinitions[3].Width = new GridLength (window.TitleBar.RightInset, GridUnitType.Pixel);
			}
		}

		protected override void OnPointerReleased (PointerReleasedEventArgs e) {
			var pointerPoint = e.GetCurrentPoint (this);
			// Frame handles X1 -> BackRequested automatically, we can handle X2
			// here to enable forward navigation
			if (pointerPoint.Properties.PointerUpdateKind == PointerUpdateKind.XButton2Released) {
				if (FrameView.CanGoForward) {
					FrameView.GoForward ();
					e.Handled = true;
				}
			}
			base.OnPointerReleased (e);
		}

		public void InitializeNavigationPages () {
			try {
				var mainPages = new MainViewModelBase[] {
					new SettingsControlViewModel { NavHeader = "Settings", IconKey = "SettingsIcon", ShowsInFooter = true },
					new HomeControlViewModel { NavHeader = "Home", IconKey = "HomeIcon" }
				};
				var menuItems = new List<NavigationViewItemBase> (4);
				var footerItems = new List<NavigationViewItemBase> (2);
				Dispatcher.UIThread.Post (() => {
					for (var i = 0; i < mainPages.Length; i++) {
						var page = mainPages[i];
						var item = new NavigationViewItem {
							Content = page.NavHeader,
							Tag = page,
							IconSource = this.FindResource (page.IconKey) as IconSource
						};
						if (page.ShowsInFooter) {
							footerItems.Add (item);
							continue;
						}
						menuItems.Add (item);
					}
					NavView.MenuItemsSource = menuItems;
					NavView.FooterMenuItemsSource = footerItems;
					FrameView.NavigateFromObject ((NavView.MenuItemsSource.ElementAt (0) as Control).Tag);
				});
			} catch {/*
	            var warningDialog = new WarningDialogProduct("Warning","Warning!",$"Exception: {ex.Message}");
	            warningDialog.ShowDialog();*/
			}
		}

	}

}