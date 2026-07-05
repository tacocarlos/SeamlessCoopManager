using IniParser;
using IniParser.Model;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SeamlessCoopManager.Manager.ModApplicator;

namespace SeamlessCoopManager.Manager.GameApplicators
{
	internal abstract class GenericDefaultApplicator : ModApplicator
	{
		public abstract string GetRelativeSettingsIniPath();

		public ModApplicationResult ApplyMod(Game game, string modFolderPath)
		{
			if (!game.IsInstalled())
			{
				return ModApplicationResult.UNKNOWN_INSTALL_PATH;
			}

			try
			{
				string userIniPath = Path.Join(game.Options.InstallPath, GetRelativeSettingsIniPath());
				string modIniPath = Path.Join(modFolderPath, GetRelativeSettingsIniPath());
				FileIniDataParser parser = new FileIniDataParser();
				IniData settingsIni = MergeIniData(parser, userIniPath, modIniPath);
				IOUtils.CopyDirectory(modFolderPath, game.Options.InstallPath!);
				if (game.Options.RetainOptions)
				{
					parser.WriteFile(userIniPath, settingsIni);
				}
				return ModApplicationResult.SUCCESS;
			}
			catch (Exception e)
			{
				Console.Error.WriteLine($"Error applying {game.ShortName} mod: {e.Message}");
			}
			return ModApplicationResult.FAILED_UNKNOWN;
		}
	}
}
