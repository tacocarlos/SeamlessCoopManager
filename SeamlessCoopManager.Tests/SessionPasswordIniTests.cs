using IniParser;
using IniParser.Model;
using SeamlessCoopManager.Manager;
using Xunit;

namespace SeamlessCoopManager.Tests
{
	public class SessionPasswordIniTests : GameStateTestBase
	{
		private const string SettingsIni =
			"[GAMEPLAY]\r\n" +
			"allow_invaders = 1\r\n" +
			"death_debuffs = 1\r\n" +
			"\r\n" +
			"[PASSWORD]\r\n" +
			"cooppassword = oldpass\r\n" +
			"\r\n" +
			"[LANGUAGE]\r\n" +
			"mod_language = english\r\n";

		private static IniData Parse(string contents)
		{
			string path = Path.GetTempFileName();
			try
			{
				File.WriteAllText(path, contents);
				return new FileIniDataParser().ReadFile(path);
			}
			finally
			{
				File.Delete(path);
			}
		}

		[Fact]
		public void ReplacesExistingPassword()
		{
			IniData data = Parse(SettingsIni);

			ModApplicator.SetSessionPassword(data, "newpass");

			Assert.Equal("newpass", data["PASSWORD"]["cooppassword"]);
		}

		[Fact]
		public void ReturnsTheSameInstanceItModified()
		{
			IniData data = Parse(SettingsIni);

			IniData result = ModApplicator.SetSessionPassword(data, "newpass");

			Assert.Same(data, result);
		}

		[Fact]
		public void LeavesOtherSectionsAndKeysUntouched()
		{
			IniData data = Parse(SettingsIni);

			ModApplicator.SetSessionPassword(data, "newpass");

			Assert.Equal("1", data["GAMEPLAY"]["allow_invaders"]);
			Assert.Equal("1", data["GAMEPLAY"]["death_debuffs"]);
			Assert.Equal("english", data["LANGUAGE"]["mod_language"]);
			Assert.Single(data["PASSWORD"]);
			Assert.Equal(3, data.Sections.Count);
		}

		[Fact]
		public void AddsPasswordKeyWhenSectionHasNone()
		{
			IniData data = Parse("[PASSWORD]\r\n");

			ModApplicator.SetSessionPassword(data, "newpass");

			Assert.Equal("newpass", data["PASSWORD"]["cooppassword"]);
		}

		[Fact]
		public void AddsPasswordSectionWhenMissing()
		{
			IniData data = Parse("[GAMEPLAY]\r\nallow_invaders = 1\r\n");

			ModApplicator.SetSessionPassword(data, "newpass");

			Assert.Equal("newpass", data["PASSWORD"]["cooppassword"]);
			Assert.Equal("1", data["GAMEPLAY"]["allow_invaders"]);
		}

		[Fact]
		public void EmptyPasswordClearsTheValue()
		{
			IniData data = Parse(SettingsIni);

			ModApplicator.SetSessionPassword(data, "");

			Assert.Equal("", data["PASSWORD"]["cooppassword"]);
		}

		[Fact]
		public void UsesCustomSectionAndFieldNames()
		{
			IniData data = Parse("[SESSION]\r\npass = oldpass\r\n");

			ModApplicator.SetSessionPassword(data, "newpass", "SESSION", "pass");

			Assert.Equal("newpass", data["SESSION"]["pass"]);
			Assert.False(data.Sections.ContainsSection("PASSWORD"));
		}

		[Fact]
		public void GetSettingsIniPath_NullWhenNotInstalled_ApplicatorPathWhenInstalled()
		{
			Assert.Null(Game.AC6.GetSettingsIniPath());

			string dir = MakeDir("ac6");
			Game.AC6.Options.SetInstallPath(dir);

			Assert.Equal(Path.Join(dir, "AC6Coop", "ac6_coop_settings.ini"), Game.AC6.GetSettingsIniPath());
		}

		[Fact]
		public void ProbeSessionPassword_ReadsPasswordFromSettingsFile()
		{
			Game.ELDEN_RING.Options.SetInstallPath(MakeDir("er"));
			MakeFile(SettingsIni, "er", "SeamlessCoop", "ersc_settings.ini");

			Assert.True(Game.ELDEN_RING.ProbeSessionPassword());
			Assert.Equal("oldpass", Game.ELDEN_RING.Options.SessionPassword);
		}

		[Fact]
		public void ProbeSessionPassword_ClearsStalePasswordWhenSettingsFileMissing()
		{
			Game.ELDEN_RING.Options.SetInstallPath(MakeDir("er"));
			Game.ELDEN_RING.Options.SetSessionPassword("stale");

			Assert.False(Game.ELDEN_RING.ProbeSessionPassword());
			Assert.Equal("", Game.ELDEN_RING.Options.SessionPassword);
		}

		[Fact]
		public void SaveSessionPassword_WritesFileAndUpdatesOptions()
		{
			Game.ELDEN_RING.Options.SetInstallPath(MakeDir("er"));
			string iniPath = MakeFile(SettingsIni, "er", "SeamlessCoop", "ersc_settings.ini");

			Assert.True(Game.ELDEN_RING.SaveSessionPassword("newpass"));

			Assert.Equal("newpass", Game.ELDEN_RING.Options.SessionPassword);
			IniData reread = new FileIniDataParser().ReadFile(iniPath);
			Assert.Equal("newpass", reread["PASSWORD"]["cooppassword"]);
			Assert.Equal("1", reread["GAMEPLAY"]["allow_invaders"]);
			Assert.Equal("english", reread["LANGUAGE"]["mod_language"]);
		}

		[Fact]
		public void SaveSessionPassword_WritesToAc6CoopFolder()
		{
			Game.AC6.Options.SetInstallPath(MakeDir("ac6"));
			string iniPath = MakeFile(SettingsIni, "ac6", "AC6Coop", "ac6_coop_settings.ini");

			Assert.True(Game.AC6.SaveSessionPassword("newpass"));

			Assert.Equal("newpass", new FileIniDataParser().ReadFile(iniPath)["PASSWORD"]["cooppassword"]);
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		public void SaveSessionPassword_RejectsEmptyAndLeavesFileUnchanged(string? password)
		{
			Game.ELDEN_RING.Options.SetInstallPath(MakeDir("er"));
			string iniPath = MakeFile(SettingsIni, "er", "SeamlessCoop", "ersc_settings.ini");

			Assert.False(Game.ELDEN_RING.SaveSessionPassword(password));

			Assert.Equal(SettingsIni, File.ReadAllText(iniPath));
		}

		[Fact]
		public void SaveSessionPassword_FalseWhenSettingsFileMissingAndDoesNotCreateIt()
		{
			string dir = MakeDir("er");
			Game.ELDEN_RING.Options.SetInstallPath(dir);

			Assert.False(Game.ELDEN_RING.SaveSessionPassword("newpass"));

			Assert.False(File.Exists(Path.Join(dir, "SeamlessCoop", "ersc_settings.ini")));
			Assert.Equal("", Game.ELDEN_RING.Options.SessionPassword);
		}

		[Fact]
		public void SaveSessionPassword_FalseWhenNotInstalled()
		{
			Assert.False(Game.ELDEN_RING.SaveSessionPassword("newpass"));
		}

		[Fact]
		public void WrittenFileIsReadBackByLoadManagerConfig()
		{
			var (configPath, installDirs) = WriteConfigForAllGames();
			string iniPath = MakeFile(SettingsIni, "installs", "ELDEN_RING", "SeamlessCoop", "ersc_settings.ini");
			var parser = new FileIniDataParser();

			IniData data = ModApplicator.SetSessionPassword(parser.ReadFile(iniPath), "newpass");
			parser.WriteFile(iniPath, data);
			Game.LoadManagerConfig(configPath);

			Assert.Equal("newpass", Game.ELDEN_RING.Options.SessionPassword);
			IniData reread = parser.ReadFile(iniPath);
			Assert.Equal("english", reread["LANGUAGE"]["mod_language"]);
		}
	}
}
