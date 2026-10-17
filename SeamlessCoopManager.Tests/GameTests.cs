using SeamlessCoopManager.Manager;
using Xunit;

namespace SeamlessCoopManager.Tests
{
	public class GameTests : GameStateTestBase
	{
		[Fact]
		public void All_HasSixGamesWithUniqueShortNames()
		{
			Assert.Equal(6, Game.All.Count);
			Assert.Equal(Game.All.Count, Game.All.Select(g => g.ShortName).Distinct().Count());
		}

		[Fact]
		public void StaticAccessors_PointAtTheMatchingGame()
		{
			Assert.Equal("DSR", Game.DSR.ShortName);
			Assert.Equal("DS2", Game.DS2.ShortName);
			Assert.Equal("DS3", Game.DS3.ShortName);
			Assert.Equal("ELDEN_RING", Game.ELDEN_RING.ShortName);
			Assert.Equal("AC6", Game.AC6.ShortName);
			Assert.Equal("NIGHTREIGN", Game.NIGHTREIGN.ShortName);
		}

		[Theory]
		[InlineData("DSR", "ds1sc_settings.ini", 570940)]
		[InlineData("DS2", "ds2sc_settings.ini", 335300)]
		[InlineData("DS3", "ds3sc_settings.ini", 374320)]
		[InlineData("ELDEN_RING", "ersc_settings.ini", 1245620)]
		[InlineData("AC6", "ac6_coop_settings.ini", 1888160)]
		[InlineData("NIGHTREIGN", "nrsc_settings.ini", 2622380)]
		public void SettingsFileNameAndAppId_MatchGame(string shortName, string settingsFile, int appId)
		{
			Game game = Game.All.Single(g => g.ShortName == shortName);

			Assert.Equal(settingsFile, game.GetSettingsFileName());
			Assert.Equal(appId, game.GetSteamAppId());
		}

		[Fact]
		public void ToString_ReturnsLongName()
		{
			Assert.Equal("Elden Ring", Game.ELDEN_RING.ToString());
		}

		[Fact]
		public void IsInstalled_FalseByDefault_TrueOnceInstallPathSet()
		{
			Assert.False(Game.ELDEN_RING.IsInstalled());

			Game.ELDEN_RING.Options.SetInstallPath(MakeDir("er"));

			Assert.True(Game.ELDEN_RING.IsInstalled());
		}

		[Fact]
		public void ProbeForExecutable_FindsLauncherInInstallFolder()
		{
			string dir = MakeDir("er");
			string launcher = MakeFile("x", "er", "ersc_launcher.exe");
			Game.ELDEN_RING.Options.SetInstallPath(dir);

			Assert.True(Game.ELDEN_RING.ProbeForExecutable());
			Assert.Equal(launcher, Game.ELDEN_RING.Options.ExecPath);
			Assert.True(Game.ELDEN_RING.IsExecutable());
		}

		[Fact]
		public void ProbeForExecutable_FalseWhenLauncherMissing()
		{
			Game.ELDEN_RING.Options.SetInstallPath(MakeDir("er"));

			Assert.False(Game.ELDEN_RING.ProbeForExecutable());
			Assert.Null(Game.ELDEN_RING.Options.ExecPath);
		}

		[Fact]
		public void ProbeForExecutable_FalseWhenNotInstalled()
		{
			Assert.False(Game.ELDEN_RING.ProbeForExecutable());
		}

		[Fact]
		public void LaunchGame_FalseWhenNoExecutable()
		{
			Assert.False(Game.ELDEN_RING.LaunchGame());
		}
	}
}
