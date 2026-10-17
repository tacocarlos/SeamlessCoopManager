using SeamlessCoopManager.Manager;
using Xunit;

namespace SeamlessCoopManager.Tests
{
	public class ManagerConfigTests : GameStateTestBase
	{
		private const string PasswordIni =
			"[GAMEPLAY]\r\n" +
			"allow_invaders = 1\r\n" +
			"\r\n" +
			"[PASSWORD]\r\n" +
			"cooppassword = hunter2\r\n";

		[Fact]
		public void WriteThenLoad_RoundTripsPathsAndRetainOptions()
		{
			string installDir = MakeDir("er");
			string launcher = MakeFile("x", "er", "ersc_launcher.exe");
			var (configPath, _) = WriteConfigForAllGames();
			Game.LoadManagerConfig(configPath);
			Game.ELDEN_RING.Options.SetInstallPath(installDir);
			Game.ELDEN_RING.Options.SetExecPath(launcher);
			Game.ELDEN_RING.Options.SetRetainOptions(false);

			Game.WriteManagerConfig(configPath);
			ResetAllGameOptions();
			Game.LoadManagerConfig(configPath);

			Assert.Equal(installDir, Game.ELDEN_RING.Options.InstallPath);
			Assert.Equal(launcher, Game.ELDEN_RING.Options.ExecPath);
			Assert.False(Game.ELDEN_RING.Options.RetainOptions);
		}

		[Fact]
		public void Load_CreatesMissingDirectoryAndConfigFile()
		{
			string configPath = Path.Combine(TempRoot, "new_dir", "sc_manager.ini");

			Game.LoadManagerConfig(configPath);

			Assert.True(File.Exists(configPath));
		}

		[Fact]
		public void Load_GameMissingFromConfigHasPreviousPathsCleared()
		{
			var (configPath, _) = WriteConfigForAllGames();
			string staleDir = MakeDir("stale");
			string staleExe = MakeFile("x", "stale", "ersc_launcher.exe");
			Game.ELDEN_RING.Options.SetInstallPath(staleDir);
			Game.ELDEN_RING.Options.SetExecPath(staleExe);
			Game.ELDEN_RING.Options.SetRetainOptions(false);
			File.WriteAllText(configPath, "[DSR]\r\nInstallPath = \r\nExecPath = \r\nRetainOptions = True\r\n");

			Game.LoadManagerConfig(configPath);

			// Steam lookup may legitimately find a real install, but never the stale temp folder.
			Assert.NotEqual(staleDir, Game.ELDEN_RING.Options.InstallPath);
			Assert.Null(Game.ELDEN_RING.Options.ExecPath);
			Assert.True(Game.ELDEN_RING.Options.RetainOptions);
		}

		[Fact]
		public void Load_ReadsPasswordFromSeamlessCoopSettings()
		{
			var (configPath, installDirs) = WriteConfigForAllGames();
			MakeFile(PasswordIni, "installs", "ELDEN_RING", "SeamlessCoop", "ersc_settings.ini");

			Game.LoadManagerConfig(configPath);

			Assert.Equal("hunter2", Game.ELDEN_RING.Options.SessionPassword);
		}

		[Fact]
		public void Load_MissingSettingsForEarlierGameStillProbesLaterGames()
		{
			// DSR is first in Game.All and has no settings file; Nightreign is last.
			var (configPath, _) = WriteConfigForAllGames();
			MakeFile(PasswordIni, "installs", "NIGHTREIGN", "SeamlessCoop", "nrsc_settings.ini");

			Game.LoadManagerConfig(configPath);

			Assert.Equal("hunter2", Game.NIGHTREIGN.Options.SessionPassword);
		}

		[Fact]
		public void Load_SettingsWithoutPasswordSectionLeavesPasswordEmpty()
		{
			var (configPath, _) = WriteConfigForAllGames();
			MakeFile("[GAMEPLAY]\r\nallow_invaders = 1\r\n", "installs", "ELDEN_RING", "SeamlessCoop", "ersc_settings.ini");

			Game.LoadManagerConfig(configPath);

			Assert.Equal("", Game.ELDEN_RING.Options.SessionPassword);
		}

		[Fact]
		public void Load_PasswordProbeUsesSameIniPathAsTheModApplicator()
		{
			// Every game's password should be read from the file its applicator writes to.
			var (configPath, installDirs) = WriteConfigForAllGames();
			foreach (Game game in Game.All)
			{
				string relPath = Applicators.GetApplicator(game).GetRelativeSettingsIniPath();
				string iniPath = Path.Combine(installDirs[game], relPath);
				Directory.CreateDirectory(Path.GetDirectoryName(iniPath)!);
				File.WriteAllText(iniPath, PasswordIni.Replace("hunter2", $"pw_{game.ShortName}"));
			}

			Game.LoadManagerConfig(configPath);

			foreach (Game game in Game.All)
			{
				Assert.Equal($"pw_{game.ShortName}", game.Options.SessionPassword);
			}
		}
	}
}
