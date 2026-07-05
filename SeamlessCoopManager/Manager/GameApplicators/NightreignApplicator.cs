using IniParser.Model;
using IniParser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static SeamlessCoopManager.Manager.ModApplicator;

namespace SeamlessCoopManager.Manager.GameApplicators
{
	internal class NightreignApplicator: GenericDefaultApplicator
	{
		private static readonly string INIT_REL_PATH = Path.Join("SeamlessCoop", "nrsc_settings.ini");
		public override string GetRelativeSettingsIniPath()
		{
			return INIT_REL_PATH;
		}

		//public ModApplicationResult ApplyMod(Game game, string modFolderPath)
		//{
		//	if (!game.IsInstalled())
		//	{
		//		return ModApplicationResult.UNKNOWN_INSTALL_PATH;
		//	}

		//	try
		//	{
		//		string userIniPath = Path.Join(game.Options.InstallPath, GetRelativeSettingsIniPath());
		//		string modIniPath = Path.Join(modFolderPath, GetRelativeSettingsIniPath());
		//		FileIniDataParser parser = new FileIniDataParser();
		//		IniData settingsIni = MergeIniData(parser, userIniPath, modIniPath);
		//		IOUtils.CopyDirectory(modFolderPath, game.Options.InstallPath!);
		//		if(game.Options.RetainOptions)
		//		{
		//			parser.WriteFile(userIniPath, settingsIni);
		//		}
		//		return ModApplicationResult.SUCCESS;				
		//	} catch(Exception e)
		//	{
		//		Console.Error.WriteLine($"Error applying {game.ShortName} mod: {e.Message}");
		//	}
		//	return ModApplicationResult.FAILED_UNKNOWN;
		//}
	}
}
