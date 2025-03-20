
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Styling;
using FluentAvalonia.Styling;
using ReactiveUI;

namespace HttpCompressionFileExtractor {

	public class SettingsControlViewModel : MainViewModelBase {
		private const string _dark = "Dark";
		private const string _light = "Light";

		private const string _system = "System";

		private string _currentAppTheme;
		private FlowDirection _currentFlowDirection;
		private Color _customAccentColor = Colors.SlateBlue;
		private readonly FluentAvaloniaTheme _faTheme;
		private Color? _listBoxColor;
		private bool _useCustomAccentColor;

		public SettingsControlViewModel () {
			_faTheme = Application.Current.Styles[0] as FluentAvaloniaTheme;
			_currentAppTheme = _system; // Assuming default theme is system
			GetPredefColors ();

			this.WhenAnyValue (x => x.CurrentAppTheme)
				.Subscribe (
					value => {
						var newTheme = GetThemeVariant (value);
						if (newTheme != null) {
							Application.Current.RequestedThemeVariant = newTheme;
						}
						_faTheme.PreferSystemTheme = value == _system;
					});

			this.WhenAnyValue (x => x.CurrentFlowDirection)
				.Subscribe (
					value => {
						var lifetime = Application.Current.ApplicationLifetime;
						if (lifetime is IClassicDesktopStyleApplicationLifetime desktop) {
							desktop.MainWindow.FlowDirection = value;
						} else if (lifetime is ISingleViewApplicationLifetime single) {
							if (TopLevel.GetTopLevel (single.MainView) is Window window) {
								window.FlowDirection = value;
							}
						}
					});

			this.WhenAnyValue (x => x.UseCustomAccent)
				.Subscribe (
					value => {
						if (value) {
							UpdateAppAccentColor (CustomAccentColor);
						} else {
							UpdateAppAccentColor (null);
						}
					});

			this.WhenAnyValue (x => x.ListBoxColor)
				.Where (value => value != null)
				.Subscribe (
					value => {
						UpdateAppAccentColor (value.Value);
					});

			this.WhenAnyValue (x => x.CustomAccentColor)
				.Subscribe (
					value => {
						UpdateAppAccentColor (value);
					});
		}

		private void GetPredefColors () {
			PredefinedColors =
			[
				Color.FromRgb(255, 185, 0),
				Color.FromRgb(255, 140, 0),
				Color.FromRgb(247, 99, 12),
				Color.FromRgb(202, 80, 16),
				Color.FromRgb(218, 59, 1),
				Color.FromRgb(239, 105, 80),
				Color.FromRgb(209, 52, 56),
				Color.FromRgb(255, 67, 67),
				Color.FromRgb(231, 72, 86),
				Color.FromRgb(232, 17, 35),
				Color.FromRgb(234, 0, 94),
				Color.FromRgb(195, 0, 82),
				Color.FromRgb(227, 0, 140),
				Color.FromRgb(191, 0, 119),
				Color.FromRgb(194, 57, 179),
				Color.FromRgb(154, 0, 137),
				Color.FromRgb(0, 120, 212),
				Color.FromRgb(0, 99, 177),
				Color.FromRgb(142, 140, 216),
				Color.FromRgb(107, 105, 214),
				Color.FromRgb(135, 100, 184),
				Color.FromRgb(116, 77, 169),
				Color.FromRgb(177, 70, 194),
				Color.FromRgb(136, 23, 152),
				Color.FromRgb(0, 153, 188),
				Color.FromRgb(45, 125, 154),
				Color.FromRgb(0, 183, 195),
				Color.FromRgb(3, 131, 135),
				Color.FromRgb(0, 178, 148),
				Color.FromRgb(1, 133, 116),
				Color.FromRgb(0, 204, 106),
				Color.FromRgb(16, 137, 62),
				Color.FromRgb(122, 117, 116),
				Color.FromRgb(93, 90, 88),
				Color.FromRgb(104, 118, 138),
				Color.FromRgb(81, 92, 107),
				Color.FromRgb(86, 124, 115),
				Color.FromRgb(72, 104, 96),
				Color.FromRgb(73, 130, 5),
				Color.FromRgb(16, 124, 16),
				Color.FromRgb(118, 118, 118),
				Color.FromRgb(76, 74, 72),
				Color.FromRgb(105, 121, 126),
				Color.FromRgb(74, 84, 89),
				Color.FromRgb(100, 124, 100),
				Color.FromRgb(82, 94, 84),
				Color.FromRgb(132, 117, 69),
				Color.FromRgb(126, 115, 95)
			];
		}

		private static ThemeVariant GetThemeVariant (string value) {
			return value switch {
				_light => ThemeVariant.Light,
				_dark => ThemeVariant.Dark,
				_system => null,
				_ => null,
			};
		}

		private void UpdateAppAccentColor (Color? color) { _faTheme.CustomAccentColor = color; }

		public FlowDirection[] AppFlowDirections { get; } = [FlowDirection.LeftToRight, FlowDirection.RightToLeft];

		public string[] AppThemes { get; } = [_system, _light, _dark];

		public string CurrentAppTheme {
			get => _currentAppTheme;
			set => this.RaiseAndSetIfChanged (ref _currentAppTheme, value);
		}

		public static string CurrentAvaloniaVersion => typeof (Application).Assembly.GetName ().Version?.ToString ();

		public FlowDirection CurrentFlowDirection {
			get => _currentFlowDirection;
			set => this.RaiseAndSetIfChanged (ref _currentFlowDirection, value);
		}

		public static string CurrentVersion => typeof (FluentAvalonia.UI.Controls.NavigationView).Assembly.GetName ().Version?.ToString ();

		public Color CustomAccentColor {
			get => _customAccentColor;
			set => this.RaiseAndSetIfChanged (ref _customAccentColor, value);
		}

		public Color? ListBoxColor { get => _listBoxColor; set => this.RaiseAndSetIfChanged (ref _listBoxColor, value); }

		public List<Color> PredefinedColors { get; private set; }

		public bool UseCustomAccent {
			get => _useCustomAccentColor;
			set => this.RaiseAndSetIfChanged (ref _useCustomAccentColor, value);
		}
	}
}
