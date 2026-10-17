using SeamlessCoopManager.Manager;
using Xunit;

namespace SeamlessCoopManager.Tests
{
	public class GameOptionsTests : GameStateTestBase
	{
		[Fact]
		public void Default_HasNoPathsRetainsOptionsAndEmptyPassword()
		{
			var options = Game.GameOptions.GetDefault();

			Assert.Null(options.InstallPath);
			Assert.Null(options.ExecPath);
			Assert.True(options.RetainOptions);
			Assert.Equal("", options.SessionPassword);
		}

		[Fact]
		public void SetInstallPath_AcceptsExistingDirectory()
		{
			var options = Game.GameOptions.GetDefault();
			string dir = MakeDir("game");

			Assert.True(options.SetInstallPath(dir));
			Assert.Equal(dir, options.InstallPath);
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		public void SetInstallPath_RejectsNullOrEmpty(string? path)
		{
			var options = Game.GameOptions.GetDefault();

			Assert.False(options.SetInstallPath(path));
			Assert.Null(options.InstallPath);
		}

		[Fact]
		public void SetInstallPath_RejectsMissingDirectoryAndKeepsPrevious()
		{
			var options = Game.GameOptions.GetDefault();
			string dir = MakeDir("game");
			options.SetInstallPath(dir);

			Assert.False(options.SetInstallPath(Path.Combine(TempRoot, "does_not_exist")));
			Assert.Equal(dir, options.InstallPath);
		}

		[Fact]
		public void SetInstallPath_RejectsAFileInsteadOfADirectory()
		{
			var options = Game.GameOptions.GetDefault();
			string file = MakeFile("x", "game.exe");

			Assert.False(options.SetInstallPath(file));
		}

		[Fact]
		public void SetExecPath_AcceptsExistingFileAndRejectsMissingFile()
		{
			var options = Game.GameOptions.GetDefault();
			string file = MakeFile("x", "launcher.exe");

			Assert.True(options.SetExecPath(file));
			Assert.Equal(file, options.ExecPath);
			Assert.False(options.SetExecPath(Path.Combine(TempRoot, "missing.exe")));
			Assert.Equal(file, options.ExecPath);
		}

		[Fact]
		public void SetSessionPassword_AcceptsValueAndRejectsEmpty()
		{
			var options = Game.GameOptions.GetDefault();

			Assert.True(options.SetSessionPassword("hunter2"));
			Assert.Equal("hunter2", options.SessionPassword);
			Assert.False(options.SetSessionPassword(""));
			Assert.False(options.SetSessionPassword(null));
			Assert.Equal("hunter2", options.SessionPassword);
		}
	}
}
