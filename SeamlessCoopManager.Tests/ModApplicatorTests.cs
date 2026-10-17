using IniParser;
using SeamlessCoopManager.Manager;
using Xunit;
using static SeamlessCoopManager.Manager.ModApplicator;

namespace SeamlessCoopManager.Tests
{
	public class ModApplicatorTests : GameStateTestBase
	{
		[Fact]
		public void GetApplicator_ExistsForEveryGame()
		{
			foreach (Game game in Game.All)
			{
				Assert.NotNull(Applicators.GetApplicator(game));
			}
		}

		[Fact]
		public void ApplicatorSettingsPath_EndsWithTheGamesSettingsFileName()
		{
			foreach (Game game in Game.All)
			{
				string relPath = Applicators.GetApplicator(game).GetRelativeSettingsIniPath();
				Assert.Equal(game.GetSettingsFileName(), Path.GetFileName(relPath));
			}
		}

		[Fact]
		public void ApplyMod_ReturnsUnknownInstallPathWhenNotInstalled()
		{
			string modDir = MakeDir("mod");

			var result = Applicators.GetApplicator(Game.ELDEN_RING).ApplyMod(Game.ELDEN_RING, modDir);

			Assert.Equal(ModApplicationResult.UNKNOWN_INSTALL_PATH, result);
		}

		[Fact]
		public void ApplyMod_CopiesFilesAndKeepsUserSettingsWhenRetaining()
		{
			string installDir = MakeDir("game");
			MakeFile("[PASSWORD]\r\ncooppassword = mine\r\n", "game", "SeamlessCoop", "ersc_settings.ini");
			string modDir = MakeDir("mod");
			MakeFile("dll", "mod", "SeamlessCoop", "ersc.dll");
			MakeFile("[PASSWORD]\r\ncooppassword = \r\n\r\n[GAMEPLAY]\r\nnew_option = 1\r\n", "mod", "SeamlessCoop", "ersc_settings.ini");
			MakeFile("exe", "mod", "ersc_launcher.exe");
			Game.ELDEN_RING.Options.SetInstallPath(installDir);
			Game.ELDEN_RING.Options.SetRetainOptions(true);

			var result = Applicators.GetApplicator(Game.ELDEN_RING).ApplyMod(Game.ELDEN_RING, modDir);

			Assert.Equal(ModApplicationResult.SUCCESS, result);
			Assert.True(File.Exists(Path.Combine(installDir, "ersc_launcher.exe")));
			Assert.True(File.Exists(Path.Combine(installDir, "SeamlessCoop", "ersc.dll")));
			var ini = new FileIniDataParser().ReadFile(Path.Combine(installDir, "SeamlessCoop", "ersc_settings.ini"));
			Assert.Equal("mine", ini["PASSWORD"]["cooppassword"]);
			Assert.Equal("1", ini["GAMEPLAY"]["new_option"]);
		}

		[Fact]
		public void ApplyMod_OverwritesUserSettingsWhenNotRetaining()
		{
			string installDir = MakeDir("game");
			MakeFile("[PASSWORD]\r\ncooppassword = mine\r\n", "game", "SeamlessCoop", "ersc_settings.ini");
			string modDir = MakeDir("mod");
			MakeFile("[PASSWORD]\r\ncooppassword = \r\n", "mod", "SeamlessCoop", "ersc_settings.ini");
			Game.ELDEN_RING.Options.SetInstallPath(installDir);
			Game.ELDEN_RING.Options.SetRetainOptions(false);

			var result = Applicators.GetApplicator(Game.ELDEN_RING).ApplyMod(Game.ELDEN_RING, modDir);

			Assert.Equal(ModApplicationResult.SUCCESS, result);
			var ini = new FileIniDataParser().ReadFile(Path.Combine(installDir, "SeamlessCoop", "ersc_settings.ini"));
			Assert.Equal("", ini["PASSWORD"]["cooppassword"]);
		}

		[Fact]
		public void CopyDirectory_CopiesNestedFilesAndOverwrites()
		{
			MakeFile("new", "src", "a.txt");
			MakeFile("nested", "src", "sub", "b.txt");
			MakeFile("old", "dst", "a.txt");

			IOUtils.CopyDirectory(Path.Combine(TempRoot, "src"), Path.Combine(TempRoot, "dst"));

			Assert.Equal("new", File.ReadAllText(Path.Combine(TempRoot, "dst", "a.txt")));
			Assert.Equal("nested", File.ReadAllText(Path.Combine(TempRoot, "dst", "sub", "b.txt")));
		}
	}
}
