using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using SeamlessCoopManager.Manager;
using System;
using static SeamlessCoopManager.Manager.ModApplicator;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace SeamlessCoopManager
{
	public sealed partial class MainWindow : Window
	{
		private string? _selectedFilePath;

		// Prevents the retain-settings confirmation dialog from firing when we
		// update the checkbox programmatically during game selection changes.
		private bool _suppressRetainDialog;

		public MainWindow()
		{
			InitializeComponent();
			SetupWindow();
			GameComboBox.ItemsSource = Game.All;

			Game.LoadManagerConfig();
		}

		private void SetupWindow()
		{
			const int width = 500;
			const int height = 760;
			AppWindow.Resize(new Windows.Graphics.SizeInt32(width, height));
			var display = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
			AppWindow.Move(new Windows.Graphics.PointInt32(
				(display.WorkArea.Width - width) / 2,
				(display.WorkArea.Height - height) / 2));
		}

		// ── Drop Zone ─────────────────────────────────────────────────────────────

		private void DropZone_DragOver(object sender, DragEventArgs e)
		{
			e.AcceptedOperation = DataPackageOperation.Copy;
			e.DragUIOverride.Caption = "Drop to select";
			e.DragUIOverride.IsGlyphVisible = true;
			DropZoneBorder.Stroke = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"];
			DropZoneBorder.Fill = (Brush)Application.Current.Resources["AccentFillColorTertiaryBrush"];
		}

		private void DropZone_DragLeave(object sender, DragEventArgs e)
		{
			ResetDropZoneColors();
		}

		private async void DropZone_Drop(object sender, DragEventArgs e)
		{
			ResetDropZoneColors();

			if (!e.DataView.Contains(StandardDataFormats.StorageItems))
				return;

			var items = await e.DataView.GetStorageItemsAsync();
			var file = items.OfType<StorageFile>()
				.FirstOrDefault(f => f.FileType.Equals(".zip", StringComparison.OrdinalIgnoreCase));

			if (file is not null)
				SetSelectedFile(file.Path, file.Name);
		}

		private async void DropZone_Tapped(object sender, TappedRoutedEventArgs e)
		{
			var picker = new FileOpenPicker();
			InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
			picker.SuggestedStartLocation = PickerLocationId.Downloads;
			picker.FileTypeFilter.Add(".zip");

			var file = await picker.PickSingleFileAsync();
			if (file is not null)
				SetSelectedFile(file.Path, file.Name);
		}

		private void SetSelectedFile(string path, string name)
		{
			_selectedFilePath = path;
			DropZoneTitle.Text = name;
			DropZoneSubtitle.Text = "Drop a different file or click to replace";
			RefreshButtons();
		}

		private void ResetDropZoneColors()
		{
			DropZoneBorder.Stroke = (Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"];
			DropZoneBorder.Fill = (Brush)Application.Current.Resources["LayerFillColorDefaultBrush"];
		}

		// ── Game selection ────────────────────────────────────────────────────────

		private void GameComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (GameComboBox.SelectedItem is Game game)
				LoadGameControls(game);
			else
				ClearGameControls();

			_selectedFilePath = null;
			DropZoneTitle.Text = "Drop your mod .zip here";
			DropZoneSubtitle.Text = "or click to browse";
			RefreshButtons();
		}

		private void LoadGameControls(Game game)
		{
			_suppressRetainDialog = true;
			InstallPathBox.Text = game.Options.InstallPath ?? string.Empty;
			ExecPathBox.Text = game.Options.ExecPath ?? string.Empty;
			RetainSettingsCheckBox.IsChecked = game.Options.RetainOptions;
			_suppressRetainDialog = false;

			BrowseDirectoryButton.IsEnabled = true;
			BrowseExecutableButton.IsEnabled = true;
			RetainSettingsCheckBox.IsEnabled = true;
			LoadSessionPassword(game);
		}

		private void ClearGameControls()
		{
			_suppressRetainDialog = true;
			InstallPathBox.Text = string.Empty;
			ExecPathBox.Text = string.Empty;
			RetainSettingsCheckBox.IsChecked = true;
			_suppressRetainDialog = false;

			BrowseDirectoryButton.IsEnabled = false;
			BrowseExecutableButton.IsEnabled = false;
			RetainSettingsCheckBox.IsEnabled = false;
			SessionPassword.Text = string.Empty;
			SessionPassword.IsEnabled = false;
			SavePasswordButton.IsEnabled = false;
		}

		// ── Session password ──────────────────────────────────────────────────────

		// Re-reads the password from the mod's settings file. The box is disabled when the
		// mod isn't installed yet, since there's no settings file to write to.
		private void LoadSessionPassword(Game game)
		{
			bool hasSettingsFile = game.ProbeSessionPassword();
			SessionPassword.Text = game.Options.SessionPassword;
			SessionPassword.IsEnabled = hasSettingsFile;
			SessionPassword.PlaceholderText = hasSettingsFile
				? "Enter Session Password"
				: "Apply the mod first to set a password";
			RefreshSavePasswordButton();
		}

		private void RefreshSavePasswordButton()
		{
			SavePasswordButton.IsEnabled = GameComboBox.SelectedItem is Game game
				&& SessionPassword.IsEnabled
				&& !string.IsNullOrEmpty(SessionPassword.Text)
				&& SessionPassword.Text != game.Options.SessionPassword;
		}

		private void SessionPassword_TextChanged(object sender, TextChangedEventArgs e)
		{
			RefreshSavePasswordButton();
		}

		private async void SavePassword_Click(object sender, RoutedEventArgs e)
		{
			if (GameComboBox.SelectedItem is not Game game)
				return;

			bool saved;
			try
			{
				saved = game.SaveSessionPassword(SessionPassword.Text);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error saving {game.ShortName} session password: {ex.Message}");
				saved = false;
			}

			RefreshSavePasswordButton();
			if (saved)
				return;

			var dialog = new ContentDialog
			{
				Title = "Couldn't save password",
				Content = "The session password could not be written to the mod's settings file. " +
				          "Make sure the game is not running and try again.",
				CloseButtonText = "OK",
				DefaultButton = ContentDialogButton.Close,
				XamlRoot = Content.XamlRoot,
			};
			await dialog.ShowAsync();
		}

		// ── Install directory ─────────────────────────────────────────────────────

		private async void BrowseDirectory_Click(object sender, RoutedEventArgs e)
		{
			if (GameComboBox.SelectedItem is not Game game)
				return;

			var picker = new FolderPicker();
			InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
			picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
			picker.FileTypeFilter.Add("*");

			var folder = await picker.PickSingleFolderAsync();
			if (folder is null)
				return;

			if (!game.Options.SetInstallPath(folder.Path))
				return;

			InstallPathBox.Text = folder.Path;
			Game.WriteManagerConfig();
			LoadSessionPassword(game);
			RefreshButtons();

			await PromptProbeForExecutable(game);
		}

		private async Task PromptProbeForExecutable(Game game)
		{
			var probeDialog = new ContentDialog
			{
				Title = "Detect game executable?",
				Content = "Would you like to automatically search for the game executable in the selected directory?",
				PrimaryButtonText = "Detect",
				CloseButtonText = "Skip",
				DefaultButton = ContentDialogButton.Primary,
				XamlRoot = Content.XamlRoot,
			};

			var result = await probeDialog.ShowAsync();
			if (result != ContentDialogResult.Primary)
				return;

			if (game.ProbeForExecutable())
			{
				ExecPathBox.Text = game.Options.ExecPath!;
				Game.WriteManagerConfig();
				RefreshButtons();
			}
		}

		// ── Game executable ───────────────────────────────────────────────────────

		private async void BrowseExecutable_Click(object sender, RoutedEventArgs e)
		{
			if (GameComboBox.SelectedItem is not Game game)
				return;

			var picker = new FileOpenPicker();
			InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
			picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
			picker.FileTypeFilter.Add(".exe");

			var file = await picker.PickSingleFileAsync();
			if (file is null)
				return;

			if (game.Options.SetExecPath(file.Path))
			{
				ExecPathBox.Text = file.Path;
				Game.WriteManagerConfig();
				RefreshButtons();
			}
		}

		// ── Retain settings ───────────────────────────────────────────────────────

		private void RetainSettings_Checked(object sender, RoutedEventArgs e)
		{
			if (_suppressRetainDialog)
				return;

			if (GameComboBox.SelectedItem is Game game)
			{
				game.Options.SetRetainOptions(true);
				Game.WriteManagerConfig();
			}
		}

		private async void RetainSettings_Unchecked(object sender, RoutedEventArgs e)
		{
			if (_suppressRetainDialog)
				return;

			if (GameComboBox.SelectedItem is not Game game)
				return;

			var dialog = new ContentDialog
			{
				Title = "Disable settings retention?",
				Content = "Your personal settings won't be preserved when applying future mod updates. " +
				          "You can re-enable this at any time.",
				PrimaryButtonText = "Disable",
				CloseButtonText = "Cancel",
				DefaultButton = ContentDialogButton.Close,
				XamlRoot = Content.XamlRoot,
			};

			var result = await dialog.ShowAsync();

			if (result == ContentDialogResult.Primary)
			{
				game.Options.SetRetainOptions(false);
				Game.WriteManagerConfig();
			}
			else
			{
				// Revert the checkbox without re-triggering the dialog.
				_suppressRetainDialog = true;
				RetainSettingsCheckBox.IsChecked = true;
				_suppressRetainDialog = false;
			}
		}

		// ── Buttons ───────────────────────────────────────────────────────────────

		private void RefreshButtons()
		{
			var game = GameComboBox.SelectedItem as Game;
			ApplyButton.IsEnabled = _selectedFilePath is not null
				&& game is not null
				&& game.IsInstalled();
			LaunchButton.IsEnabled = game is not null && game.IsExecutable();
		}

		private void LaunchGame_Click(object sender, RoutedEventArgs e)
		{
			if (GameComboBox.SelectedItem is Game game)
				game.LaunchGame();
		}

		private async void ApplyButton_Click(object sender, RoutedEventArgs e)
		{
			if (_selectedFilePath is null || GameComboBox.SelectedItem is not Game game)
				return;

			// Show in-button progress while the update runs on a background thread.
			ApplyButton.IsEnabled = false;
			LaunchButton.IsEnabled = false;
			ApplyButton.Content = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 8,
				Children =
				{
					new ProgressRing { IsActive = true, Width = 16, Height = 16 },
					new TextBlock { Text = "Applying..." }
				}
			};

			ModApplicationResult result;
			try
			{
				ModApplicator applicator = Applicators.GetApplicator(game);
				result = await Task.Run(() => game.UpdateMod(applicator, _selectedFilePath));
			}
			catch
			{
				result = ModApplicationResult.FAILED_UNKNOWN;
			}

			ApplyButton.Content = "Apply Update";

			// Applying can create the settings file or reset it when settings aren't retained.
			LoadSessionPassword(game);

			await ShowUpdateResultDialog(game, result);

			RefreshButtons();
		}

		

		private async Task ShowUpdateResultDialog(Game game, ModApplicationResult result)
		{

			(string title, string glyph, string body) = result switch
			{
				ModApplicationResult.SUCCESS => (
					"Update applied",
					"\uE73E", // Checkmark
					$"{game.Name} was updated successfully."),

				ModApplicationResult.INVALID_MOD => (
					"Invalid mod package",
					"\uE783", // Error badge
					"The selected file doesn't appear to be a valid mod package. " +
					"Make sure you're using the correct update zip."),

				ModApplicationResult.UNKNOWN_INSTALL_PATH => (
					"Install directory not found",
					"\uE783",
					"The game install directory could not be found. " +
					"Please verify the install path and try again."),

				_ => (
					"Update failed",
					"\uE783",
					"An unexpected error occurred while applying the update. " +
					"Make sure the game is not running and try again.")
			};

			var content = new StackPanel { Spacing = 12 };
			content.Children.Add(new FontIcon
			{
				Glyph = glyph,
				FontSize = 32,
				Foreground = result == ModApplicationResult.SUCCESS
					? (Brush)Application.Current.Resources["SystemFillColorSuccessBrush"]
					: (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"]
			});
			content.Children.Add(new TextBlock
			{
				Text = body,
				TextWrapping = TextWrapping.WrapWholeWords
			});

			var dialog = new ContentDialog
			{
				Title = title,
				Content = content,
				CloseButtonText = "OK",
				DefaultButton = ContentDialogButton.Close,
				XamlRoot = Content.XamlRoot,
			};

			await dialog.ShowAsync();
		}
	}
}
