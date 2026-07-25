using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;
using Windows.UI.Popups;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SeamlessCoopManager
{
	/// <summary>
	/// Provides application-specific behavior to supplement the default Application class.
	/// </summary>
	public partial class App : Application
	{
		private const string GithubRepoUrl = "https://github.com/tacocarlos/SeamlessCoopManager";

		private Window? _window;

		/// <summary>
		/// Initializes the singleton application object.  This is the first line of authored code
		/// executed, and as such is the logical equivalent of main() or WinMain().
		/// </summary>
		///
		public App()
		{
			this.InitializeComponent();
		}

		[STAThread]
		private static void Main(string[] args)
        {

            if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new Exception("Invalid Platform");
            } else
            {
                VelopackApp.Build().OnFirstRun(async (hook) =>
				{
					MessageDialog dialog = new MessageDialog("Thanks for installing!\nInstalled games will be attempted to be detected automatically, but you might have to manually add them.");
					await dialog.ShowAsync();
				}).Run();
				WinRT.ComWrappersSupport.InitializeComWrappers();
    			Application.Start((p) =>
    			{
    				var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
    				SynchronizationContext.SetSynchronizationContext(context);
    				new App();
    			});
            }
		}

		/// <summary>
		/// Invoked when the application is launched.
		/// </summary>
		/// <param name="args">Details about the launch request and process.</param>
		protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
		{
			_window = new MainWindow();
			_window.Activate();

			_ = CheckForUpdatesAsync();
		}

		private static async Task CheckForUpdatesAsync()
		{
			try
			{
				var mgr = new UpdateManager(new GithubSource(GithubRepoUrl, null, prerelease: false));

				// Not installed via Velopack (e.g. running from the IDE) — nothing to update.
				if (!mgr.IsInstalled)
					return;

				var newVersion = await mgr.CheckForUpdatesAsync();
				if (newVersion is null)
					return; // already up to date

				mgr.DownloadUpdates(newVersion);

				mgr.ApplyUpdatesAndRestart(newVersion);
			}
			catch
			{
				// Update checks must never crash or block the app.
			}
		}
	}
}
