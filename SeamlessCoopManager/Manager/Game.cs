using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using static SeamlessCoopManager.Manager.ModApplicator;

using IniParser;
using IniParser.Model;
using System.Diagnostics;

namespace SeamlessCoopManager.Manager
{
	public class Game
	{

		public class GameName
		{
			private string _short, _long, _execName;

			private int _steamAppId;
			public string ShortName { get => _short; }
			public string LongName { get => _long; }

			public string ExecName { get => _execName; }

			public int SteamAppId { get => _steamAppId; }

			public GameName(string shortName, string longName, string execName, int appId)
			{
				_short = shortName;
				_long = longName;
				_execName = execName;
				_steamAppId = appId;
			}


			public string GetPrimary() => LongName;
		}

		public class GameOptions
		{
			public static readonly string MANAGER_SETTINGS_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SeamlessCoopManager", "sc_manager.ini");
			private string? _installPath, _execPath;
			private bool _retainOptions;

			public string? InstallPath { get => _installPath; }
			public string? ExecPath { get => _execPath; }
			public bool RetainOptions { get => _retainOptions; }

			private GameOptions()
			{
				_installPath = null;
				_execPath = null;
				_retainOptions = true;
			}

			public static GameOptions GetDefault()
			{
				return new GameOptions();
			}

			public bool SetInstallPath(string? path)
			{
				if(!string.IsNullOrEmpty(path) && Directory.Exists(path))
				{
					_installPath = path;
					return true;
				}

				return false;
			}

			public bool SetExecPath(string? path)
			{
				if (!string.IsNullOrEmpty(path) && File.Exists(path))
				{
					_execPath = path;
					return true;
				}
				return false;
			}

			public bool SetRetainOptions(bool retain)
			{
				_retainOptions = retain;
				return true;
			}
		}

		private static readonly GameName DSR_NAME = new GameName("DSR", "Dark Souls: Remastered", "ds1sc_launcher.exe", 570940);
		private static readonly GameName DS2_NAME = new GameName("DS2", "Dark Souls 2: Scholar of the First Sin", "ds2sc_launcher.exe", 335300); // TODO: verify this is the name when the public build releases
		private static readonly GameName DS3_NAME = new GameName("DS3", "Dark Souls 3", "ds3sc_launcher.exe", 374320);
		private static readonly GameName SEKIRO_NAME = new GameName("SEKIRO", "Sekiro: Shadows Die Twice", "sekiro_coop_launcher.exe", 814380); // TODO: update this when Yui releases the Sekiro mod
		private static readonly GameName ELDEN_RING_NAME = new GameName("ELDEN_RING", "Elden Ring", "ersc_launcher.exe", 1245620);
		private static readonly GameName AC6_NAME = new GameName("AC6", "ARMORED CORE 6: Fires of Rubicon", "ac6_for_coop_launcher.exe", 1888160);
		private static readonly GameName NIGHTREIGN_NAME = new GameName("NIGHTREIGN", "Elden Ring Nightreign", "nrsc_launcher.exe", 2622380);


		public static readonly IReadOnlyList<Game> All = new List<Game>
		{
			new Game(DSR_NAME, ""),
			new Game(DS2_NAME, "Game\\"),
			new Game(DS3_NAME, "Game\\"),
			//new Game(SEKIRO_NAME, ""),
			new Game(ELDEN_RING_NAME, "Game\\"),
			new Game(AC6_NAME, "Game\\"),
			new Game(NIGHTREIGN_NAME, "Game\\"),
		}.AsReadOnly();

		public static readonly Game DSR = All[0];
		public static readonly Game DS2 = All[1];
		public static readonly Game DS3 = All[2];
		//public static readonly Game SEKIRO = All[3];
		//public static readonly Game ELDEN_RING = All[4];
		//public static readonly Game AC6 = All[5];
		//public static readonly Game NIGHTREIGN = All[6];

		// TODO: remove this when Sekiro mod is released
		public static readonly Game ELDEN_RING = All[3];
		public static readonly Game AC6 = All[4];
		public static readonly Game NIGHTREIGN = All[5];


		private readonly GameName _name;
		private readonly string _relGamePath; // relative path to the game's real folder
		private GameOptions _options;

		public string Name
		{
			get { return _name.GetPrimary(); }
		}

		public string ShortName
		{
			get { return _name.ShortName; }
		}

		public override string ToString() => Name;

		public GameOptions Options
		{
			get { return _options; }
		}

		private Game(GameName name, string relativeGamePath)
		{
			_name = name;
			_options = GameOptions.GetDefault();
			_relGamePath = relativeGamePath;
		}

		public bool IsInstalled()
		{
			return !string.IsNullOrEmpty(this.Options.InstallPath) && Directory.Exists(this.Options.InstallPath);
		}

		public bool IsExecutable()
		{
			return !string.IsNullOrEmpty(this.Options.ExecPath) && Path.Exists(this.Options.ExecPath);
		}

		public int GetSteamAppId()
		{
			return _name.SteamAppId;
		}

		public string RelativeGamePath { get { return _relGamePath; } }

		public bool ProbeForExecutable()
		{
			if (!IsInstalled())
			{
				return false;
			}

			string execPath = Path.Combine(this.Options.InstallPath!, this._name.ExecName);
			if (File.Exists(execPath))
			{
				this.Options.SetExecPath(execPath);
				return true;
			}
			return false;
		}

		public static void WriteManagerConfig(string? ConfigFile = null)
		{
			if(ConfigFile == null)
			{
				ConfigFile = GameOptions.MANAGER_SETTINGS_PATH;
			}
			FileIniDataParser parser = new FileIniDataParser();
			IniData data = new IniData();
			foreach(Game game in All)
			{
				string sectionName = game.ShortName;
				data[sectionName]["InstallPath"] = game.Options.InstallPath ?? "";
				data[sectionName]["ExecPath"] = game.Options.ExecPath ?? "";
				data[sectionName]["RetainOptions"] = game.Options.RetainOptions.ToString();
			}
			parser.WriteFile(ConfigFile, data);
		}

		// note for future: this must be run first, or the directory won't exist
		public static void LoadManagerConfig(string? ConfigFile = null) {
			string? directoryPath = Path.GetDirectoryName(ConfigFile);
			if(ConfigFile == null)
			{
				ConfigFile = GameOptions.MANAGER_SETTINGS_PATH;
				directoryPath = Path.GetDirectoryName(ConfigFile);
			}


			if (Directory.Exists(directoryPath) == false)
			{
				Directory.CreateDirectory(directoryPath!);
			}

			if (!File.Exists(ConfigFile)) WriteManagerConfig(ConfigFile); // surely there's a better way, but idc

			FileIniDataParser parser = new FileIniDataParser();
			IniData options = parser.ReadFile(ConfigFile);
			foreach(Game game in All)
			{
				string sectionName = game.ShortName;
				if (options.Sections.ContainsSection(sectionName))
				{
					string installPath = options[sectionName]["InstallPath"];
					string execPath = options[sectionName]["ExecPath"];
					bool retainOptions = bool.Parse(options[sectionName]["RetainOptions"]);
					game.Options.SetInstallPath(installPath);
					game.Options.SetExecPath(execPath);
					game.Options.SetRetainOptions(retainOptions);
				}
				else
				{
					game.Options.SetInstallPath(null);
					game.Options.SetExecPath(null);
					game.Options.SetRetainOptions(true);
				}

				if(string.IsNullOrEmpty(game.Options.InstallPath))
				{
					string? installPath = SteamGameLocator.GetInstallPath(game.GetSteamAppId());
					Console.WriteLine(installPath);
					game.Options.SetInstallPath(Path.Join(installPath, game.RelativeGamePath));
				}
			}
		}

		public ModApplicationResult UpdateMod(ModApplicator applicator, string modZipPath)
		{
			return applicator.UpdateMod(this, modZipPath);
		}

		public bool LaunchGame()
		{
			if(!IsExecutable())
			{
				Console.Error.WriteLine($"Cannot launch {Name}: Executable path is not set or does not exist.");
				return false;
			}


			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				FileName = this.Options.ExecPath,
				WorkingDirectory = Path.GetDirectoryName(this.Options.ExecPath),
				UseShellExecute = true,
			};

			Process? p = Process.Start(startInfo);
			return p != null;
		}
	}
}
