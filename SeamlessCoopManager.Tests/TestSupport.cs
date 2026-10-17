using System.Reflection;
using SeamlessCoopManager.Manager;
using Xunit;

// Game.All is static shared state, so tests must not run concurrently.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace SeamlessCoopManager.Tests
{
	/// <summary>
	/// Gives each test a scratch directory and resets every Game's options before and after the test.
	/// </summary>
	public abstract class GameStateTestBase : IDisposable
	{
		protected readonly string TempRoot;

		protected GameStateTestBase()
		{
			TempRoot = Path.Combine(Path.GetTempPath(), "scm_tests", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(TempRoot);
			ResetAllGameOptions();
		}

		public void Dispose()
		{
			ResetAllGameOptions();
			try { Directory.Delete(TempRoot, true); } catch (IOException) { }
		}

		protected static void ResetAllGameOptions()
		{
			FieldInfo field = typeof(Game).GetField("_options", BindingFlags.Instance | BindingFlags.NonPublic)!;
			foreach (Game game in Game.All)
			{
				field.SetValue(game, Game.GameOptions.GetDefault());
			}
		}

		protected string MakeDir(params string[] parts)
		{
			string path = Path.Combine(new[] { TempRoot }.Concat(parts).ToArray());
			Directory.CreateDirectory(path);
			return path;
		}

		protected string MakeFile(string contents, params string[] parts)
		{
			string path = Path.Combine(new[] { TempRoot }.Concat(parts).ToArray());
			Directory.CreateDirectory(Path.GetDirectoryName(path)!);
			File.WriteAllText(path, contents);
			return path;
		}

		/// <summary>
		/// Writes a manager config where every game points at its own temp install folder,
		/// so LoadManagerConfig never falls back to the real Steam registry lookup.
		/// Returns the config path and the install folder per game.
		/// </summary>
		protected (string ConfigPath, Dictionary<Game, string> InstallDirs) WriteConfigForAllGames()
		{
			var installDirs = new Dictionary<Game, string>();
			var lines = new List<string>();
			foreach (Game game in Game.All)
			{
				string dir = MakeDir("installs", game.ShortName);
				installDirs[game] = dir;
				lines.Add($"[{game.ShortName}]");
				lines.Add($"InstallPath = {dir}");
				lines.Add("ExecPath = ");
				lines.Add("RetainOptions = True");
				lines.Add("");
			}
			string configPath = MakeFile(string.Join(Environment.NewLine, lines), "config", "sc_manager.ini");
			return (configPath, installDirs);
		}
	}
}
