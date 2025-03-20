using System;
using System.Reflection;

using Avalonia.Controls;
using FluentAvalonia.UI.Controls;

namespace HttpCompressionFileExtractor.Factory {
	public class NavigationPageFactory (MainViewModel mainViewViewModel) : INavigationPageFactory {

		public MainViewModel MainViewViewModel { get; } = mainViewViewModel;

		/// <summary>
		/// Get a page from a type.
		/// </summary>
		/// <param name="srcType"></param>
		/// <returns></returns>
		public Control GetPage (Type srcType) {
			return null;
		}

		/// <summary>
		/// Get a page from an object.
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		public Control GetPageFromObject (object target) {
			var viewModelName = target.GetType ().Name;
			var controlName = viewModelName.Replace ("ControlViewModel", "Control");
			var namespacePrefix = "HttpCompressionFileExtractor";
			try {
				var controlType = Assembly.GetExecutingAssembly ().GetType ($"{namespacePrefix}.{controlName}");
				if (controlType != null && Activator.CreateInstance (controlType) is Control control) {
					control.DataContext = target;
					return control;
				}
			} catch (Exception ex) {
				Console.WriteLine ($"Error creating control instance: {ex.Message}");
			}
			return null;
		}
	}
}